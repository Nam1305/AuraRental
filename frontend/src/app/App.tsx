import { LoginPage } from '@/features/auth/LoginPage'
import { SessionProvider, useSession } from '@/features/session/SessionProvider'
import { AppRouter } from './AppRouter'

function SessionBoundary() {
  const { user, loading } = useSession()
  if (loading) return <main className="center-page"><div className="state-card">Đang xác thực phiên làm việc…</div></main>
  if (!user) return <LoginPage />
  if (user.branches.length === 0) {
    return <main className="center-page"><div className="state-card state-card--error">Tài khoản chưa được cấp chi nhánh.</div></main>
  }
  return <AppRouter />
}

export function App() {
  return <SessionProvider><SessionBoundary /></SessionProvider>
}
