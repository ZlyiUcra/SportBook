import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'
import { useSessionStore } from '@/entities/session/model/store'
import { getMe } from '@/entities/user/api/getMe'
import { useLogout } from '@/features/auth/logout/model/useLogout'

export function HomePage() {
  const { t } = useTranslation()
  const user = useSessionStore((state) => state.user)
  const logout = useLogout()
  const meQuery = useQuery({ queryKey: ['me'], queryFn: getMe })

  return (
    <div className="mx-auto flex min-h-svh max-w-lg flex-col gap-4 p-8">
      <h1 className="text-2xl font-semibold">{t('app.title')}</h1>
      <p>{t('home.registeredAs', { name: user?.name, email: user?.email })}</p>
      <p className="text-sm text-muted-foreground">
        {meQuery.isLoading && t('home.loading')}
        {meQuery.isError && t('home.error')}
        {meQuery.data && t('home.confirmed', { name: meQuery.data.name, role: meQuery.data.role })}
      </p>
      <Button onClick={logout} variant="outline" className="self-start">
        {t('home.signOut')}
      </Button>
    </div>
  )
}
