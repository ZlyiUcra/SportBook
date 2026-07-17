import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { useSessionStore } from '@/entities/session/model/store'

/** Public page explaining what SportBook is and how the customer/owner flows work - reachable without signing in. */
export function AboutPage() {
  const { t } = useTranslation()
  const user = useSessionStore((state) => state.user)

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6 p-4">
      <div>
        <h1 className="text-2xl font-semibold">{t('about.title')}</h1>
        <p className="mt-2 text-muted-foreground">{t('about.intro')}</p>
      </div>

      <section>
        <h2 className="font-medium">{t('about.oneAccountTitle')}</h2>
        <p className="mt-1 text-sm text-muted-foreground">{t('about.oneAccount')}</p>
      </section>

      <section>
        <h2 className="font-medium">{t('about.customerTitle')}</h2>
        <p className="mt-1 text-sm text-muted-foreground">{t('about.customer')}</p>
      </section>

      <section>
        <h2 className="font-medium">{t('about.ownerTitle')}</h2>
        <p className="mt-1 text-sm text-muted-foreground">{t('about.owner')}</p>
      </section>

      <section>
        <h2 className="font-medium">{t('about.reviewsTitle')}</h2>
        <p className="mt-1 text-sm text-muted-foreground">{t('about.reviews')}</p>
      </section>

      {!user && (
        <div className="flex items-center gap-3 border-t pt-4">
          <p className="text-sm text-muted-foreground">{t('about.loginPrompt')}</p>
          <Button asChild size="sm" variant="outline">
            <Link to="/login">{t('nav.logIn')}</Link>
          </Button>
          <Button asChild size="sm">
            <Link to="/register">{t('about.registerCta')}</Link>
          </Button>
        </div>
      )}
    </div>
  )
}
