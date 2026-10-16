import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { getGetApiAppsQueryOptions } from '@/api/generated'
import { AppList } from '@/apps/app-list'

export const Route = createFileRoute('/_authenticated/')({
  loader: ({ context }) =>
    context.queryClient.query({
      ...getGetApiAppsQueryOptions(),
      staleTime: 'static',
    }),
  component: AppsPage,
})

function AppsPage() {
  const { t } = useTranslation()

  return (
    <div className="flex flex-col gap-4">
      <h1 className="font-heading text-lg font-semibold">{t('apps.title')}</h1>
      <AppList />
    </div>
  )
}
