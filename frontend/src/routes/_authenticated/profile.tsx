import { Link, createFileRoute } from '@tanstack/react-router'
import { ArrowLeftIcon } from 'lucide-react'
import { getGetApiProfileQueryOptions } from '@/api/generated'
import { Button } from '@/components/ui/button'
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
      <div className="flex items-center gap-2">
        <Button
          variant="ghost"
          size="icon"
          aria-label="Back to your apps"
          nativeButton={false}
          render={<Link to="/" />}
        >
          <ArrowLeftIcon />
        </Button>
        <h1 className="font-heading text-lg font-semibold">Your profile</h1>
      </div>
      <ProfileForm profile={profile} />
    </div>
  )
}
