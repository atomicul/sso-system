API_URL ?= http://localhost:5080
SPA_URL ?= http://localhost:5173
OUT ?= out
VERSION ?=
DB_IMAGE ?= mcr.microsoft.com/mssql/server:2025-latest
DB_CONTAINER ?= sso-db
DB_VOLUME ?= $(DB_CONTAINER)-data
DB_PORT ?= 1433
DB_PASSWORD ?= Dev-Passw0rd
DB_CONNECTION ?= Server=localhost,$(DB_PORT);Database=Sso;User Id=sa;Password=$(DB_PASSWORD);TrustServerCertificate=True
DEV_KEYS ?= $(CURDIR)/.dev/keys

SOLUTIONS := backend/Sso.Server.slnx client_library/Sso.Client.slnx
API_PROJECT := backend/Sso.Server
FRONTEND := frontend
DEPLOY := deploy

.DEFAULT_GOAL := help

.PHONY: help install dev dev-backend dev-frontend dev-database dev-keys reset-dev-database wait-database wait-backend migration api build run check clean

help: ## Show this help
	@awk 'BEGIN {FS = ":.*## "} /^[a-z-]+:.*## / {printf "  %-20s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

install: ## Restore the .NET solutions and tools and install the frontend and deploy packages
	for solution in $(SOLUTIONS); do dotnet restore $$solution || exit 1; done
	dotnet tool restore
	pnpm -C $(FRONTEND) install
	uv --directory $(DEPLOY) sync --locked

dev: ## Run the database, the API and the Vite dev server together
	$(MAKE) -j3 dev-database dev-backend dev-frontend

dev-backend: dev-keys wait-database ## Run only the API with dotnet watch, once the database is up
	SpaUrl=$(SPA_URL) ConnectionStrings__Default="$(DB_CONNECTION)" SigningKey__Path=$(DEV_KEYS)/sso.pem \
		dotnet watch --project $(API_PROJECT)

dev-frontend: wait-backend ## Run only the Vite dev server, once the API is up
	API_URL=$(API_URL) pnpm -C $(FRONTEND) dev

dev-database: ## Run SQL Server in Docker for development
	docker run --rm --init --name $(DB_CONTAINER) --platform linux/amd64 \
		-e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=$(DB_PASSWORD) \
		--health-cmd '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$$MSSQL_SA_PASSWORD" -Q "SELECT 1" -b -o /dev/null' \
		--health-start-period 2m --health-start-interval 1s \
		-p $(DB_PORT):1433 -v $(DB_VOLUME):/var/opt/mssql $(DB_IMAGE)

dev-keys: ## Generate the development key pairs in DEV_KEYS, unless they exist
	@mkdir -p $(DEV_KEYS)
	@test -f $(DEV_KEYS)/sso.pem || uv --directory $(DEPLOY) run generate-key-pair $(DEV_KEYS)/sso.pem

reset-dev-database: ## Delete the development database volume; stop the database first
	docker volume rm -f $(DB_VOLUME)

wait-database: ## Wait until the development database container is healthy
	@echo "Waiting for $(DB_CONTAINER) to accept connections (make dev-database starts it)"
	@until [ "$$(docker inspect -f '{{.State.Health.Status}}' $(DB_CONTAINER) 2>/dev/null)" = healthy ]; do sleep 1; done

wait-backend: ## Wait until the API answers its health check
	@echo "Waiting for the API at $(API_URL) to be healthy (make dev-backend starts it)"
	@until curl -sf $(API_URL)/health > /dev/null; do sleep 1; done

migration: ## Add an EF Core migration named NAME, e.g. make migration NAME=AddTags
	dotnet ef migrations add $(NAME) --project $(API_PROJECT) --output-dir Data/Migrations

api: ## Regenerate the frontend client from the API OpenAPI document
	dotnet build $(API_PROJECT)
	cp $(API_PROJECT)/obj/openapi.json $(FRONTEND)/openapi.json
	pnpm -C $(FRONTEND) generate:api

build: ## Publish the API to out/ with the built SPA in out/wwwroot
	rm -rf $(OUT)
	dotnet publish $(API_PROJECT) -c Release -o $(OUT) $(if $(VERSION),-p:Version=$(VERSION))
	pnpm -C $(FRONTEND) build
	mkdir -p $(OUT)/wwwroot
	cp -R $(FRONTEND)/dist/. $(OUT)/wwwroot/

run: build dev-keys ## Build, then serve the API and the SPA together
	cd $(OUT) && ConnectionStrings__Default="$(DB_CONNECTION)" SigningKey__Path=$(DEV_KEYS)/sso.pem \
		dotnet Sso.Server.dll --urls $(API_URL)

check: ## Run formatting, build, pending migration, lint, type checks and tests of every project
	for solution in $(SOLUTIONS); do dotnet format $$solution --verify-no-changes || exit 1; done
	for solution in $(SOLUTIONS); do dotnet build $$solution || exit 1; done
	dotnet ef migrations has-pending-model-changes --project $(API_PROJECT)
	for solution in $(SOLUTIONS); do dotnet test --solution $$solution || exit 1; done
	pnpm -C $(FRONTEND) lint
	pnpm -C $(FRONTEND) format:check
	pnpm -C $(FRONTEND) typecheck
	pnpm -C $(FRONTEND) test
	uv --directory $(DEPLOY) run ruff format --check
	uv --directory $(DEPLOY) run ruff check
	uv --directory $(DEPLOY) run pytest
	uv --directory $(DEPLOY) run ansible-lint
	uv --directory $(DEPLOY) run ansible-playbook --syntax-check --inventory localhost, playbooks/database.yml playbooks/webapp.yml playbooks/tls.yml

clean: ## Remove build output, including the bin and obj directories of the .NET projects
	rm -rf $(OUT) $(FRONTEND)/dist $(foreach solution,$(SOLUTIONS),$(dir $(solution))*/bin $(dir $(solution))*/obj)
