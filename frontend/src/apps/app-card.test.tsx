import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { AppCard } from './app-card'

describe('AppCard', () => {
  it('links to the launch URL of the app', () => {
    const html = renderToStaticMarkup(
      <AppCard app={{ key: 'billing', name: 'Billing', iconUrl: null }} />,
    )

    expect(html).toContain('href="/api/apps/billing/launch"')
    expect(html).toContain('Billing')
  })

  it('shows the icon of an app that has one', () => {
    const html = renderToStaticMarkup(
      <AppCard
        app={{
          key: 'billing',
          name: 'Billing',
          iconUrl: '/api/apps/billing/icon',
        }}
      />,
    )

    expect(html).toContain('<img src="/api/apps/billing/icon"')
  })

  it('shows a placeholder for an app without an icon', () => {
    const html = renderToStaticMarkup(
      <AppCard app={{ key: 'billing', name: 'Billing', iconUrl: null }} />,
    )

    expect(html).not.toContain('<img')
    expect(html).toContain('<svg')
  })
})
