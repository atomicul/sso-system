import { Link, Outlet, createFileRoute, redirect } from '@tanstack/react-router'
import { fetchSession } from '@/auth/session'
import { useAuth } from '@/auth/use-auth'
import { Button } from '@/components/ui/button'

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
  const { logout } = useAuth()

  return (
    <div className="min-h-svh">
      <header className="border-b">
        <div className="mx-auto flex max-w-2xl items-center justify-between gap-4 p-4">
          <Link to="/" className="font-heading font-semibold">
            SSO
          </Link>
          <div className="flex items-center gap-3">
            <Link
              to="/profile"
              className="text-muted-foreground hover:text-foreground truncate text-sm"
            >
              {user.email}
            </Link>
            <Button variant="outline" size="sm" onClick={() => logout()}>
              Log out
            </Button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-2xl p-4">
        <Outlet />
      </main>
    </div>
  )
}
