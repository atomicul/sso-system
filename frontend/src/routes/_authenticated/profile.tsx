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
      <div className="flex flex-col gap-1">
        <h1 className="font-heading text-lg font-semibold">Your profile</h1>
        <p className="text-muted-foreground text-sm">
          Apps receive these details the next time you open them from here.
        </p>
      </div>
      <ProfileForm profile={profile} />
    </div>
  )
}
