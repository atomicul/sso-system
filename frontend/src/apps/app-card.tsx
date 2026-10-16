import { AppWindow } from 'lucide-react'
import { getGetApiAppsKeyLaunchUrl } from '@/api/generated'
import type { AppResponse } from '@/api/generated/model'

/**
 * Links to the app's launch URL with a plain anchor rather than a router link: the API answers it by redirecting the
 * browser to the app itself.
 */
export function AppCard({ app }: { app: AppResponse }) {
  return (
    <a
      href={getGetApiAppsKeyLaunchUrl(app.key)}
      className="hover:bg-muted flex flex-col items-center gap-3 rounded-lg border p-4 text-center transition-colors"
    >
      {app.iconUrl ? (
        <img
          src={app.iconUrl}
          alt=""
          className="size-12 rounded-md object-cover"
        />
      ) : (
        <AppWindow aria-hidden className="text-muted-foreground size-12" />
      )}
      <span className="text-sm font-medium">{app.name}</span>
    </a>
  )
}
