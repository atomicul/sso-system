import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/_authenticated/')({
  component: HomePage,
})

function HomePage() {
  return <p className="text-muted-foreground text-sm">You are signed in.</p>
}
