import { Link, Outlet, createFileRoute, redirect } from '@tanstack/react-router'
import { AccountMenu } from '@/auth/account-menu'
import { fetchSession } from '@/auth/session'

export const Route = createFileRoute('/_authenticated')({
  beforeLoad: async ({ context, location }) => {
    const user = await fetchSession(context.queryClient)
    if (!user) {
      throw redirect({ to: '/login', search: { redirect: location.href } })
    }
    return { user }
  },
  component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
  const { user } = Route.useRouteContext()

  return (
    <div className="flex h-svh flex-col">
      <header className="shrink-0 border-b">
        <div className="mx-auto flex max-w-2xl items-center justify-between gap-4 p-4">
          <Link to="/" className="font-heading font-semibold">
            SSO
          </Link>
          <AccountMenu email={user.email} />
        </div>
      </header>
      <main className="flex-1 overflow-y-auto">
        <div className="mx-auto max-w-2xl p-4">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
