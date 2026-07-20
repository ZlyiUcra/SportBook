import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { PasswordInput } from '@/shared/ui/password-input'
import { Label } from '@/shared/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/ui/card'
import { ApiRequestError } from '@/shared/api/axiosInstance'
import { useSessionStore } from '@/entities/session/model/store'
import { register as registerRequest } from '../api/register'
import { registerSchema, type RegisterFormValues } from '../model/schema'

export function RegisterForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const signIn = useSessionStore((state) => state.signIn)
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({ resolver: zodResolver(registerSchema) })

  const mutation = useMutation({
    mutationFn: registerRequest,
    onSuccess: (data) => {
      signIn(data.accessToken, data.refreshToken, data.user)
      navigate('/')
    },
  })

  return (
    <Card className="w-full max-w-sm">
      <CardHeader>
        <CardTitle>{t('auth.register.title')}</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={handleSubmit((values) => mutation.mutate(values))}
          className="flex flex-col gap-4"
        >
          <div className="flex flex-col gap-2">
            <Label htmlFor="name">{t('auth.name')}</Label>
            <Input id="name" {...register('name')} />
            {errors.name && <p className="text-sm text-destructive">{t('auth.validation.nameRequired')}</p>}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="email">{t('auth.email')}</Label>
            <Input id="email" type="email" {...register('email')} />
            {errors.email && <p className="text-sm text-destructive">{t('auth.validation.emailInvalid')}</p>}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="password">{t('auth.password')}</Label>
            <PasswordInput
              id="password"
              showPasswordLabel={t('auth.showPassword')}
              hidePasswordLabel={t('auth.hidePassword')}
              {...register('password')}
            />
            {errors.password && <p className="text-sm text-destructive">{t('auth.validation.passwordMin')}</p>}
          </div>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? t('auth.register.submitting') : t('auth.register.submit')}
          </Button>
          {mutation.isError && (
            <p role="alert" className="text-sm text-destructive">
              {mutation.error instanceof ApiRequestError ? mutation.error.message : t('auth.genericError')}
            </p>
          )}
          <p className="text-sm text-muted-foreground">
            {t('auth.register.haveAccount')}{' '}
            <Link to="/login" className="underline">
              {t('auth.register.loginLink')}
            </Link>
          </p>
        </form>
      </CardContent>
    </Card>
  )
}
