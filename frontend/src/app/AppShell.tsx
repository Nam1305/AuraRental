import { useEffect, useState, type ReactNode } from 'react'
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
  const [mobileNavOpen, setMobileNavOpen] = useState(false)
  const [desktopNavCollapsed, setDesktopNavCollapsed] = useState(
    () => localStorage.getItem('aura.desktopNavCollapsed') === 'true',
  )
  const activeBranch = user?.branches.find((branch) => branch.id === activeBranchId)
  const pathname = window.location.pathname

  useEffect(() => {
    localStorage.setItem('aura.desktopNavCollapsed', String(desktopNavCollapsed))
  }, [desktopNavCollapsed])

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMobileNavOpen(false)
    }
    window.addEventListener('keydown', closeOnEscape)
    return () => window.removeEventListener('keydown', closeOnEscape)
  }, [])

  const toggleNavigation = () => {
    if (window.matchMedia('(min-width: 1024px)').matches) {
      setDesktopNavCollapsed((value) => !value)
    } else {
      setMobileNavOpen((value) => !value)
    }
  }

  return (
    <div className={`app-shell${mobileNavOpen ? ' sidebar-open' : ''}${desktopNavCollapsed ? ' sidebar-collapsed' : ''}`}>
      <button className="sidebar-backdrop" type="button" aria-label="Đóng menu" onClick={() => setMobileNavOpen(false)} />
      <aside className="sidebar" id="main-navigation" aria-label="Điều hướng chính">
        <div className="sidebar-header">
          <a className="brand" href="/" onClick={() => setMobileNavOpen(false)}><span>A</span><strong>Aura Rental</strong></a>
          <button className="sidebar-close" type="button" aria-label="Đóng menu" onClick={() => setMobileNavOpen(false)}>×</button>
        </div>
        <nav>
          {navigation.map((item) => (
            <a className={pathname === item.href ? 'active' : ''} href={item.href} key={item.href} title={item.label} onClick={() => setMobileNavOpen(false)}>
              <i>{item.icon}</i><span>{item.label}</span>
            </a>
          ))}
        </nav>
        <button className="sidebar-user" type="button" title="Đăng xuất" onClick={() => { setMobileNavOpen(false); logout() }}>
          <span>{user?.name.slice(0, 1).toUpperCase()}</span>
          <div><strong>{user?.name}</strong><small>{user?.role} · Đăng xuất</small></div>
        </button>
      </aside>

      <div className="workspace">
        <header className="topbar">
          <button className="nav-toggle" type="button" aria-label="Đóng hoặc mở menu" aria-controls="main-navigation" aria-expanded={window.matchMedia('(min-width: 1024px)').matches ? !desktopNavCollapsed : mobileNavOpen} onClick={toggleNavigation}>
            <span /><span /><span />
          </button>
          <a className="mobile-brand" href="/"><span>A</span>Aura</a>
          {user && user.branches.length > 1 ? (
            <label className="branch-select">
              <span>Chi nhánh</span>
              <select value={activeBranchId ?? ''} onChange={(event) => selectBranch(Number(event.target.value))}>
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
