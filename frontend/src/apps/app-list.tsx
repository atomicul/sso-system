import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/fetcher'
import { useGetApiApps } from '@/api/generated'
import { AppCard } from './app-card'

export function AppList() {
  const { t } = useTranslation()
  const { data: apps, isPending, error } = useGetApiApps()

  if (isPending) {
    return <p className="text-muted-foreground text-sm">{t('apps.loading')}</p>
  }
  if (error) {
    return <p className="text-destructive text-sm">{errorMessage(error)}</p>
  }
  if (apps.length === 0) {
    return <p className="text-muted-foreground text-sm">{t('apps.empty')}</p>
  }

  return (
    <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3">
      {apps.map((app) => (
        <li key={app.key}>
          <AppCard app={app} />
        </li>
      ))}
    </ul>
  )
}
