"""setup-webapp: installs a release of the app on a host as a systemd service."""

import argparse
import os
import sys
from pathlib import Path

from sso_deploy.passwords import generate_password
from sso_deploy.runner import add_release_argument, argument_parser, run_playbook


def parse_client_apps_directory(value: str) -> Path:
    path = Path(value).expanduser()
    if not (path / "apps.json").is_file():
        raise argparse.ArgumentTypeError(f"expected a directory with the client apps' apps.json, got {value!r}")
    return path.resolve()


def main() -> None:
    parser = argument_parser(__doc__.partition(": ")[2])
    add_release_argument(parser)
    parser.add_argument(
        "--change-connection-string",
        action="store_true",
        help="ask for the database connection string even though the host already has one",
    )
    parser.add_argument(
        "--change-certificate",
        action="store_true",
        help="replace the host's Data Protection certificate, which logs every user out",
    )
    parser.add_argument(
        "--change-signing-key",
        action="store_true",
        help="replace the key the SSO signs its tokens with, which client apps reject until they have its public key",
    )
    parser.add_argument(
        "--client-apps",
        type=parse_client_apps_directory,
        help="replace the host's client apps with the apps.json, public keys and icons of this directory",
    )
    args = parser.parse_args()
    variables = {
        "sso_change_connection_string": args.change_connection_string,
        "sso_change_certificate": args.change_certificate,
        "sso_change_signing_key": args.change_signing_key,
        "sso_client_apps_source": str(args.client_apps or ""),
        "sso_controller_directory": os.getcwd(),
    }
    secrets = {"SSO_GENERATED_CERTIFICATE_PASSWORD": generate_password()}
    sys.exit(run_playbook("webapp.yml", args, variables=variables, secrets=secrets))
