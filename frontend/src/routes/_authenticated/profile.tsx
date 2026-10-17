import { createFileRoute } from '@tanstack/react-router'
import { getGetApiProfileQueryOptions } from '@/api/generated'
import { ProfileForm } from '@/profile/profile-form'

export const Route = createFileRoute('/_authenticated/profile')({
  loader: ({ context }) =>
    context.queryClient.ensureQueryData(getGetApiProfileQueryOptions()),
  component: ProfilePage,
})

function ProfilePage() {
  const profile = Route.useLoaderData()

  return (
    <div className="flex flex-col gap-4">
      <h1 className="font-heading text-lg font-semibold">Your profile</h1>
      <ProfileForm profile={profile} />
    </div>
  )
}
