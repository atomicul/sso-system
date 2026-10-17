import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import { getGetApiProfileQueryOptions } from '@/api/generated'
import { Page } from '@/components/page'
import { ProfileForm } from '@/profile/profile-form'

export const Route = createFileRoute('/_authenticated/profile')({
  loader: ({ context }) =>
    context.queryClient.query({
      ...getGetApiProfileQueryOptions(),
      staleTime: 'static',
    }),
  component: ProfilePage,
})

function ProfilePage() {
  const { t } = useTranslation()

  return (
    <Page title={t('profile.title')}>
      <p className="text-muted-foreground text-sm">
        {t('profile.description')}
      </p>
      <ProfileForm />
    </Page>
  )
}
