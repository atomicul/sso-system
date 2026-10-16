import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'

export const Route = createFileRoute('/_authenticated/')({
  component: HomePage,
})

function HomePage() {
  const { t } = useTranslation()

  return <p className="text-muted-foreground text-sm">{t('home.signedIn')}</p>
}
