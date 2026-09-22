import { useState, type FormEvent } from 'react'
import { useSession } from '@/features/session/SessionProvider'

export function LoginPage() {
  const { login, loading, error } = useSession()
  const [identifier, setIdentifier] = useState('')
  const [password, setPassword] = useState('')

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (identifier.trim() && password) await login(identifier, password)
  }

  return (
    <main className="login-page">
      <section className="login-card">
        <div className="brand-mark">A</div>
        <span className="eyebrow">Aura Rental Workspace</span>
        <h1>Chào bạn quay lại</h1>
        <p>Tài khoản quyết định chi nhánh được truy cập. Frontend không tự gán role hoặc quyền chi nhánh.</p>

        <form onSubmit={submit} className="login-form">
          <label className="field">
            <span>Email hoặc username</span>
            <input
              type="text"
              autoComplete="username"
              value={identifier}
              onChange={(event) => setIdentifier(event.target.value)}
              placeholder="hanoi hoặc hanoi.staff@aurarental.local"
              autoFocus
            />
          </label>
          <label className="field">
            <span>Mật khẩu</span>
            <input
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="Nhập mật khẩu"
            />
          </label>
          <button
            className="button button--primary button--block"
            disabled={loading || !identifier.trim() || !password}
          >
            {loading ? 'Đang đăng nhập…' : 'Đăng nhập'}
          </button>
        </form>
        {error && <div className="inline-error">{error.message}</div>}
      </section>
    </main>
  )
}
