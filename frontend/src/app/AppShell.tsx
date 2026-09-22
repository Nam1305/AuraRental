import type { ReactNode } from 'react'
import { useSession } from '@/features/session/SessionProvider'

const navigation = [
  { href: '/', label: 'Hôm nay', icon: '◉' },
  { href: '/availability', label: 'Tìm đồ', icon: '⌕' },
  { href: '/reservations', label: 'Giữ chỗ', icon: '◇' },
  { href: '/orders', label: 'Đơn', icon: '▤' },
  { href: '/returns', label: 'Trả đồ', icon: '↩' },
  { href: '/catalog', label: 'Kho', icon: '▦' },
  { href: '/customers', label: 'Khách', icon: '○' },
  { href: '/reports', label: 'Báo cáo', icon: '⌁' },
  { href: '/settings', label: 'Cấu hình', icon: '⚙' },
]

export function AppShell({ children }: { children: ReactNode }) {
  const { user, activeBranchId, selectBranch, logout } = useSession()
  const activeBranch = user?.branches.find((branch) => branch.id === activeBranchId)
  const pathname = window.location.pathname

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="/"><span>A</span><strong>Aura Rental</strong></a>
        <nav>
          {navigation.map((item) => (
            <a className={pathname === item.href ? 'active' : ''} href={item.href} key={item.href}>
              <i>{item.icon}</i>{item.label}
            </a>
          ))}
        </nav>
        <button className="sidebar-user" type="button" onClick={logout}>
          <span>{user?.name.slice(0, 1).toUpperCase()}</span>
          <div><strong>{user?.name}</strong><small>{user?.role} · Đăng xuất</small></div>
        </button>
      </aside>

      <div className="workspace">
        <header className="topbar">
          <a className="mobile-brand" href="/"><span>A</span>Aura</a>
          {user && user.branches.length > 1 ? (
            <label className="branch-select">
              <span>Chi nhánh</span>
              <select value={activeBranchId ?? ''} onChange={(event) => selectBranch(event.target.value)}>
                {user.branches.map((branch) => <option value={branch.id} key={branch.id}>{branch.name}</option>)}
              </select>
            </label>
          ) : (
            <div className="branch-badge"><span>Chi nhánh</span><strong>{activeBranch?.name ?? 'Chưa được cấp quyền'}</strong></div>
          )}
          <span className="user-chip">{user?.name}</span>
        </header>
        <main className="content">{children}</main>
      </div>

      <nav className="bottom-nav">
        {navigation.slice(0, 5).map((item) => (
          <a className={pathname === item.href ? 'active' : ''} href={item.href} key={item.href}>
            <i>{item.icon}</i><span>{item.label}</span>
          </a>
        ))}
      </nav>
    </div>
  )
}
