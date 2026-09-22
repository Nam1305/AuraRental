const actions = [
  { href: '/availability', label: 'Tìm đồ trống', detail: 'Bắt đầu tư vấn', tone: 'rose' },
  { href: '/reservations', label: 'Giữ chỗ', detail: '2 reservation cần theo dõi', tone: 'sand' },
  { href: '/orders', label: 'Đơn đang thuê', detail: '4 đơn đang vận hành', tone: 'sage' },
  { href: '/returns', label: 'Trả đồ', detail: '1 đơn chờ kiểm tra', tone: 'lavender' },
]

export function DashboardPage() {
  return (
    <section className="page-stack">
      <header className="page-heading page-heading--hero">
        <span className="eyebrow">Việc cần làm hôm nay</span>
        <h1>Chào buổi sáng</h1>
        <p>Ưu tiên các đơn chờ cọc, lịch giao nhận và hàng vừa trả.</p>
      </header>

      <div className="action-grid">
        {actions.map((action) => (
          <a className={`action-card action-card--${action.tone}`} href={action.href} key={action.href}>
            <span>{action.label}</span>
            <strong>{action.detail}</strong>
            <b>→</b>
          </a>
        ))}
      </div>

      <section className="panel next-task">
        <div>
          <span className="eyebrow">Việc tiếp theo</span>
          <h2>Xác nhận cọc còn lại</h2>
          <p>Đơn và dashboard sẽ lấy dữ liệu thật khi module Orders/Payments được nối API.</p>
        </div>
        <a className="button" href="/orders">Mở danh sách đơn</a>
      </section>
    </section>
  )
}
