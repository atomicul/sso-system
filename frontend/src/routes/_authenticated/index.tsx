import { createFileRoute } from '@tanstack/react-router'
import { getGetApiAppsQueryOptions } from '@/api/generated'
import { AppList } from '@/apps/app-list'

export const Route = createFileRoute('/_authenticated/')({
  loader: ({ context }) =>
    context.queryClient.ensureQueryData(getGetApiAppsQueryOptions()),
  component: AppsPage,
})

function AppsPage() {
  return (
    <div className="flex flex-col gap-4">
      <h1 className="font-heading text-lg font-semibold">Your apps</h1>
      <AppList />
    </div>
  )
}
