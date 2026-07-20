import React from 'react'
import { withTranslation, type WithTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/button'

type Props = WithTranslation & { children: React.ReactNode }
type State = { hasError: boolean }

/**
 * Catches render errors anywhere below it in the tree - without this, an unhandled error in any
 * page renders a blank white screen with no fallback (no boundary previously existed anywhere in
 * the app). React has no hook-based error-boundary API as of React 19 - `componentDidCatch` still
 * requires a class component. Wrapped with `withTranslation` (not `useTranslation`) for the same
 * reason - hooks aren't usable in a class component.
 */
class ErrorBoundaryImpl extends React.Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: unknown) {
    console.error(error)
  }

  render() {
    if (this.state.hasError) {
      const { t } = this.props
      return (
        <div className="flex min-h-svh flex-col items-center justify-center gap-4 p-4 text-center">
          <p className="text-lg font-medium">{t('common.somethingWentWrong')}</p>
          <Button onClick={() => window.location.reload()}>{t('common.reloadPage')}</Button>
        </div>
      )
    }
    return this.props.children
  }
}

export const ErrorBoundary = withTranslation()(ErrorBoundaryImpl)
