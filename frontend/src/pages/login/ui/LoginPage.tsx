import { LanguageSwitcher } from '@/shared/i18n/ui/LanguageSwitcher'
import { LoginForm } from '@/features/auth/login/ui/LoginForm'

export function LoginPage() {
  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-4 p-4">
      <LanguageSwitcher />
      <LoginForm />
    </div>
  )
}
