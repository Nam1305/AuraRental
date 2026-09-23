import { useEffect, useState } from 'react'
import { AsyncState } from '@/shared/components/AsyncState'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import { formatMoney } from '@/shared/format/money'
import { formatDateTime } from '@/shared/format/date'
import { statusLabel } from '@/shared/format/status'
import { useSession } from '@/features/session/SessionProvider'
import { MoneyDialog } from '@/shared/components/MoneyInput'
import { recordReservationPayment } from '@/features/reservations/reservation.api'
import {
  cancelOrder,
  completeDelivery,
  completeReturn,
  getOrder,
  prepareOrder,
  searchOrders,
  startDelivery,
  startReturn,
  verifyIdentity,
} from './order.api'

export function OrdersPage() {
  const { activeBranchId } = useSession()
  const [status, setStatus] = useState('PENDING_DEPOSIT,PENDING_VERIFICATION,CONFIRMED,PREPARING,RENTING,INSPECTING')
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const orders = useApiQuery(
    () => activeBranchId ? searchOrders(activeBranchId, status, query) : Promise.resolve([]),
    [activeBranchId, status, query],
  )

  useEffect(() => { setSelectedId(null) }, [activeBranchId])
  useEffect(() => {
    const closeDetail = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setSelectedId(null)
    }
    window.addEventListener('keydown', closeDetail)
    return () => window.removeEventListener('keydown', closeDetail)
  }, [])

  return (
    <section className="page-stack">
      <header className="page-heading">
        <span className="eyebrow">Cọc → chuẩn bị → giao → thuê → nhận trả</span>
        <h1>Đơn hàng</h1>
        <p>Theo dõi đúng trạng thái vận hành; mỗi nút chỉ xuất hiện khi backend cho phép chuyển bước.</p>
      </header>
      <div className="panel filter-bar">
        <label className="field"><span>Trạng thái</span><select value={status} onChange={(event) => setStatus(event.target.value)}><option value="PENDING_DEPOSIT,PENDING_VERIFICATION,CONFIRMED,PREPARING,RENTING,INSPECTING">Đang vận hành</option><option value="PENDING_DEPOSIT">Chờ cọc</option><option value="CONFIRMED">Đã xác nhận</option><option value="PREPARING">Chuẩn bị/giao</option><option value="RENTING">Đang thuê</option><option value="INSPECTING">Đang kiểm hàng</option><option value="COMPLETED">Hoàn tất</option><option value="CANCELLED">Đã hủy</option><option value="">Tất cả</option></select></label>
        <label className="field filter-bar__search"><span>Tìm đơn hoặc khách</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="ORD…, tên, số điện thoại" /></label>
      </div>
      <div className="module-layout orders-layout">
        <AsyncState loading={orders.loading} error={orders.error} empty={orders.data?.length === 0}>
          <div className="result-list">
            {orders.data?.map((order) => <button type="button" className={`panel operation-card ${selectedId === order.id ? 'operation-card--selected' : ''}`} key={order.id} onClick={() => setSelectedId(order.id)}>
              <div className="operation-card__top"><strong>{order.orderNo}</strong><span className="status-pill">{statusLabel(order.status)}</span></div>
              <h2>{order.customerName}</h2><p>{order.phoneMasked} · {order.itemSummary}</p>
              <div className="operation-card__meta"><span>{formatDateTime(order.rentalStartAt)} → {formatDateTime(order.rentalEndAt)}</span><strong>{order.depositRemaining > 0 ? `Thiếu ${formatMoney(order.depositRemaining)}` : 'Đủ cọc'}</strong></div>
              <span className="order-card__open">Xem và xử lý đơn <b>→</b></span>
            </button>)}
          </div>
        </AsyncState>
        {selectedId && activeBranchId ? <div className="order-detail-layer">
          <button className="order-detail-backdrop" type="button" aria-label="Đóng chi tiết đơn" onClick={() => setSelectedId(null)} />
          <div className="order-detail-surface" role="dialog" aria-modal={!window.matchMedia('(min-width: 1024px)').matches} aria-label="Chi tiết đơn hàng">
            <header className="order-detail-mobile-header"><button type="button" onClick={() => setSelectedId(null)}>←</button><div><small>Đơn hàng</small><strong>Chi tiết và thao tác</strong></div></header>
            <OrderDetailPanel branchId={activeBranchId} orderId={selectedId} onChanged={() => void orders.reload()} />
          </div>
        </div> : <div className="state-card detail-empty">Chọn một đơn để xem checklist và thao tác.</div>}
      </div>
    </section>
  )
}

function OrderDetailPanel({ branchId, orderId, onChanged }: { branchId: number; orderId: number; onChanged: () => void }) {
  const [version, setVersion] = useState(0)
  const [message, setMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<Error | null>(null)
  const [depositDialogOpen, setDepositDialogOpen] = useState(false)
  const detail = useApiQuery(() => getOrder(branchId, orderId), [branchId, orderId, version])
  const order = detail.data

  const run = async (action: () => Promise<unknown>, success: string) => {
    setActionError(null)
    try { await action(); setMessage(success); setVersion((value) => value + 1); onChanged() } catch (error) { setActionError(error as Error) }
  }

  return <AsyncState loading={detail.loading} error={detail.error}>{order && <aside className="panel detail-panel order-detail-panel">
    <div className="operation-card__top"><strong>{order.orderNo}</strong><span className="status-pill">{statusLabel(order.status)}</span></div>
    <div className="order-customer"><div className="avatar">{order.customer.nameSnapshot.slice(0, 1).toUpperCase()}</div><div><h2>{order.customer.nameSnapshot}</h2><a href={`tel:${order.customer.phoneSnapshot}`}>{order.customer.phoneSnapshot}</a><p>{order.customer.deliveryAddressSnapshot}</p></div></div>
    <dl className="detail-grid"><div><dt>Lịch thuê</dt><dd>{formatDateTime(order.rentalStartAt)}<br />→ {formatDateTime(order.rentalEndAt)}</dd></div><div><dt>Cọc yêu cầu</dt><dd>{formatMoney(order.depositRequired)}</dd></div><div><dt>Đã nhận</dt><dd>{formatMoney(order.depositConfirmed)}</dd></div><div><dt>Còn thiếu</dt><dd>{formatMoney(order.depositRemaining)}</dd></div></dl>
    <div className="progress-strip"><span className={order.delivery.status !== 'NOT_STARTED' ? 'done' : ''}>Giao đi</span><span className={order.status === 'RENTING' || order.status === 'INSPECTING' || order.status === 'COMPLETED' ? 'done' : ''}>Đang thuê</span><span className={order.returnDelivery.status !== 'NOT_STARTED' ? 'done' : ''}>Nhận trả</span><span className={order.status === 'COMPLETED' ? 'done' : ''}>Đối soát</span></div>
    {message && <div className="success-note">{message}</div>}{actionError && <div className="inline-error">{actionError.message}</div>}
    <section className="order-action-box"><span className="eyebrow">Việc cần làm</span><div className="action-buttons order-actions">
      {order.allowedActions.includes('RECORD_TARGET_DEPOSIT') && <button className="button button--primary" onClick={() => setDepositDialogOpen(true)}>Nhận thêm cọc</button>}
      {order.allowedActions.includes('VERIFY_IDENTITY') && <button className="button" onClick={() => window.confirm('Đã đối chiếu CCCD với khách?') && void run(() => verifyIdentity(branchId, order.id), 'Đã xác minh CCCD.')}>Xác minh CCCD</button>}
      {order.allowedActions.includes('PREPARE') && <button className="button button--primary" onClick={() => void run(() => prepareOrder(branchId, order.id), 'Đã chuyển sang chuẩn bị đồ.')}>Bắt đầu chuẩn bị</button>}
      {order.allowedActions.includes('START_DELIVERY') && order.delivery.status === 'NOT_STARTED' && <button className="button button--primary" onClick={() => { const tracking = window.prompt('Mã vận đơn giao đi (có thể bỏ trống)'); if (tracking !== null) void run(() => startDelivery(branchId, order.id, tracking.trim() || null), 'Đã bắt đầu giao đồ.') }}>Bắt đầu giao</button>}
      {order.allowedActions.includes('COMPLETE_DELIVERY') && order.delivery.status === 'IN_TRANSIT' && <button className="button button--primary" onClick={() => window.confirm('Xác nhận khách đã nhận đồ?') && void run(() => completeDelivery(branchId, order.id), 'Đã giao đồ; đơn chuyển sang đang thuê.')}>Xác nhận đã giao</button>}
      {order.allowedActions.includes('START_RETURN') && order.returnDelivery.status === 'NOT_STARTED' && <button className="button button--primary" onClick={() => { const tracking = window.prompt('Mã vận đơn nhận trả (có thể bỏ trống)'); if (tracking !== null) void run(() => startReturn(branchId, order.id, tracking.trim() || null), 'Đã bắt đầu nhận đồ trả.') }}>Bắt đầu nhận trả</button>}
      {order.allowedActions.includes('COMPLETE_RETURN') && order.returnDelivery.status === 'IN_TRANSIT' && <button className="button button--primary" onClick={() => window.confirm('Xác nhận shop đã nhận lại đồ?') && void run(() => completeReturn(branchId, order.id), 'Đã nhận đồ; chuyển sang kiểm hàng.')}>Đã nhận lại đồ</button>}
      {order.allowedActions.includes('CANCEL') && <button className="button button--danger" onClick={() => { const reason = window.prompt('Lý do hủy đơn'); if (reason && window.confirm('Xác nhận hủy đơn?')) void run(() => cancelOrder(branchId, order.id, reason), 'Đã hủy đơn.') }}>Hủy đơn</button>}
      {order.status === 'INSPECTING' && <a className="button" href="/returns">Mở kiểm hàng trả</a>}
      {order.allowedActions.length === 0 && order.status !== 'INSPECTING' && <span className="order-no-action">Hiện không có thao tác cần xử lý.</span>}
    </div></section>
    <h3 className="detail-section-title">Sản phẩm trong đơn</h3>
    <div className="line-items">{order.items.map((item) => <div key={item.orderItemId}><span>{item.productName} · {item.size}<small>{item.assetCode} · {item.packageCode}{item.condition ? ` · ${statusLabel(item.condition)}` : ''}</small></span><strong>{formatMoney(item.rentalPrice)}</strong></div>)}</div>
    {order.payments.length > 0 && <details className="details-block"><summary>Giao dịch ({order.payments.length})</summary>{order.payments.map((payment) => <div key={payment.id}><span>{statusLabel(payment.type)} · {payment.method}</span><strong>{formatMoney(payment.amount)}</strong></div>)}</details>}
    {depositDialogOpen && <MoneyDialog title="Ghi nhận cọc vừa nhận" initialAmount={order.depositRemaining} confirmLabel="Xác nhận ghi nhận" onClose={() => setDepositDialogOpen(false)} onConfirm={(amount) => { setDepositDialogOpen(false); void run(() => recordReservationPayment(branchId, order.reservationId, 'TARGET_DEPOSIT', amount), 'Đã ghi nhận cọc.') }} />}
  </aside>}</AsyncState>
}
