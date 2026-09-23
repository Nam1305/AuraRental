import { useState, type FormEvent } from 'react'
import { AsyncState } from '@/shared/components/AsyncState'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import { formatMoney } from '@/shared/format/money'
import { formatDateTime } from '@/shared/format/date'
import { statusLabel } from '@/shared/format/status'
import { useSession } from '@/features/session/SessionProvider'
import { ImageUploadInput } from '@/shared/components/ImageUploadInput'
import { MoneyField } from '@/shared/components/MoneyInput'
import { getOrder } from '@/features/orders/order.api'
import type { OrderItem } from '@/features/orders/order.types'
import { recordReservationPayment } from '@/features/reservations/reservation.api'
import {
  approveRefund,
  createRefund,
  getRefundReceipt,
  getReturnQueue,
  inspectOrderItem,
  returnRefundForReview,
  settleRefund,
  submitRefund,
} from './return.api'
import type { ReturnQueueItem } from './return.types'
import { RefundReceiptImage } from './RefundReceiptImage'

export function ReturnsPage() {
  const { activeBranchId } = useSession()
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const queue = useApiQuery(
    () => activeBranchId ? getReturnQueue(activeBranchId) : Promise.resolve([]),
    [activeBranchId],
  )
  const selected = queue.data?.find((item) => item.orderId === selectedId) ?? null

  return (
    <section className="page-stack">
      <header className="page-heading">
        <span className="eyebrow">Nhận trả → kiểm từng mã → duyệt → đối soát</span>
        <h1>Trả đồ</h1>
        <p>Điều kiện và phí được ghi trên từng item; kho chỉ cập nhật sau khi kiểm tra và đối soát.</p>
      </header>
      <div className="module-layout">
        <AsyncState loading={queue.loading} error={queue.error} empty={queue.data?.length === 0}>
          <div className="result-list">
            {queue.data?.map((item) => <button type="button" className={`panel operation-card ${selectedId === item.orderId ? 'operation-card--selected' : ''}`} key={item.orderId} onClick={() => setSelectedId(item.orderId)}>
              <div className="operation-card__top"><strong>{item.orderNo}</strong><span className="status-pill">{statusLabel(item.refundStatus ?? 'INSPECTING')}</span></div>
              <h2>{item.customerName}</h2><p>Đã kiểm {item.inspectionCompleted}/{item.inspectionTotal} món</p>
              <div className="operation-card__meta"><span>Nhận lúc {formatDateTime(item.returnedAt)}</span><strong>{item.refundAmount != null ? `Hoàn ${formatMoney(item.refundAmount)}` : 'Chưa chốt phiếu'}</strong></div>
            </button>)}
          </div>
        </AsyncState>
        {selected && activeBranchId ? <ReturnDetailPanel branchId={activeBranchId} queueItem={selected} onChanged={() => void queue.reload()} /> : <div className="state-card detail-empty">Chọn một đơn vừa trả để bắt đầu kiểm hàng.</div>}
      </div>
    </section>
  )
}

function ReturnDetailPanel({ branchId, queueItem, onChanged }: { branchId: number; queueItem: ReturnQueueItem; onChanged: () => void }) {
  const { user } = useSession()
  const [version, setVersion] = useState(0)
  const [message, setMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<Error | null>(null)
  const detail = useApiQuery(() => getOrder(branchId, queueItem.orderId), [branchId, queueItem.orderId, version])
  const receipt = useApiQuery(
    () => queueItem.refundId && queueItem.refundStatus === 'APPROVED'
      ? getRefundReceipt(branchId, queueItem.refundId)
      : Promise.resolve(null),
    [branchId, queueItem.refundId, queueItem.refundStatus, version],
  )
  const order = detail.data
  const isManager = user?.role === 'MANAGER'
  const settlementCompleted = receipt.data?.status === 'SETTLED'

  const refresh = (success: string) => { setMessage(success); setVersion((value) => value + 1); onChanged() }
  const run = async (action: () => Promise<unknown>, success: string) => {
    setActionError(null)
    try { await action(); refresh(success) } catch (error) { setActionError(error as Error) }
  }
  const completeSettlement = async (method: string, reference: string | null) => {
    setActionError(null)
    try {
      await settleRefund(branchId, queueItem.refundId!, method, reference)
      setMessage('Đã đối soát. Ảnh tổng kết bên dưới đã cập nhật trạng thái để gửi khách.')
      setVersion((value) => value + 1)
    } catch (error) { setActionError(error as Error) }
  }

  return <AsyncState loading={detail.loading} error={detail.error}>{order && <aside className="panel detail-panel return-detail">
    <div className="operation-card__top"><strong>{order.orderNo}</strong><span className="status-pill">{statusLabel(queueItem.refundStatus ?? 'INSPECTING')}</span></div>
    <h2>{order.customer.nameSnapshot}</h2><p>Đã nhận lại {formatDateTime(order.returnDelivery.completedAt)}</p>
    <div className="inspection-list">{order.items.map((item) => <InspectionForm key={`${item.orderItemId}-${item.condition ?? 'new'}`} branchId={branchId} orderId={order.id} item={item} onSaved={() => refresh(`Đã lưu kiểm tra ${item.assetCode}.`)} onError={setActionError} />)}</div>

    <section className="settlement-card">
      <div className="section-heading"><div><span className="eyebrow">Đối soát cọc</span><h3>{queueItem.refundStatus ? statusLabel(queueItem.refundStatus) : 'Chưa tạo phiếu'}</h3></div></div>
      {(queueItem.refundAmount != null || queueItem.additionalCollection != null) && <dl className="detail-grid"><div><dt>Hoàn khách</dt><dd>{formatMoney(queueItem.refundAmount ?? 0)}</dd></div><div><dt>Thu thêm</dt><dd>{formatMoney(queueItem.additionalCollection ?? 0)}</dd></div></dl>}
      {message && <div className="success-note">{message}</div>}{actionError && <div className="inline-error">{actionError.message}</div>}
      <div className="action-buttons">
        {!queueItem.refundId && order.items.every((item) => item.condition) && <button className="button button--primary" onClick={() => { const note = window.prompt('Ghi chú điều chỉnh (có thể bỏ trống)'); if (note !== null) void run(() => createRefund(branchId, order.id, note.trim() || null), 'Đã tạo phiếu đối soát nháp.') }}>Tạo phiếu đối soát</button>}
        {queueItem.refundId && queueItem.refundStatus === 'DRAFT' && <button className="button button--primary" onClick={() => window.confirm('Gửi phiếu cho manager duyệt?') && void run(() => submitRefund(branchId, queueItem.refundId!), 'Đã gửi phiếu duyệt.')}>Gửi duyệt</button>}
        {isManager && queueItem.refundId && queueItem.refundStatus === 'SUBMITTED' && <button className="button button--primary" onClick={() => window.confirm('Duyệt số tiền đối soát hiện tại?') && void run(() => approveRefund(branchId, queueItem.refundId!, queueItem.refundVersion ?? 1), 'Đã duyệt phiếu đối soát.')}>Duyệt phiếu</button>}
        {isManager && queueItem.refundId && queueItem.refundStatus === 'SUBMITTED' && <button className="button" onClick={() => { const reason = window.prompt('Lý do trả staff kiểm tra lại'); if (reason) void run(() => returnRefundForReview(branchId, queueItem.refundId!, reason), 'Đã trả phiếu về bản nháp.') }}>Trả về kiểm tra</button>}
        {isManager && queueItem.refundStatus === 'APPROVED' && (queueItem.additionalCollection ?? 0) > 0 && <button className="button" onClick={() => void run(() => recordReservationPayment(branchId, order.reservationId, 'ADDITIONAL_COLLECTION', queueItem.additionalCollection ?? 0), 'Đã ghi nhận khoản thu thêm.')}>Ghi nhận thu thêm</button>}
        {isManager && queueItem.refundId && queueItem.refundStatus === 'APPROVED' && !settlementCompleted && <button className="button button--primary" onClick={() => { const method = window.prompt('Phương thức hoàn/đối soát', 'BANK_TRANSFER'); const reference = method && window.prompt('Mã giao dịch (có thể bỏ trống)'); if (method && reference !== null && window.confirm('Hoàn tất đối soát và đóng đơn?')) void completeSettlement(method, reference.trim() || null) }}>Hoàn tất đối soát</button>}
      </div>
      {queueItem.refundId && queueItem.refundStatus === 'APPROVED' && <AsyncState loading={receipt.loading} error={receipt.error}>
        {receipt.data && <RefundReceiptImage receipt={receipt.data} onMessage={setMessage} />}
      </AsyncState>}
    </section>
  </aside>}</AsyncState>
}

function InspectionForm({ branchId, orderId, item, onSaved, onError }: { branchId: number; orderId: number; item: OrderItem; onSaved: () => void; onError: (error: Error) => void }) {
  const [condition, setCondition] = useState(item.condition ?? 'GOOD')
  const [actualRentalFee, setActualRentalFee] = useState(String(item.rentalPrice))
  const [processingFee, setProcessingFee] = useState(String(item.processingFee ?? 0))
  const [damageNote, setDamageNote] = useState(item.damageNote ?? '')
  const [damagePhotoPaths, setDamagePhotoPaths] = useState(item.damagePhotoPaths.join('\n'))
  const [saving, setSaving] = useState(false)
  const inventoryOutcome = condition === 'GOOD' ? 'USABLE' : condition === 'DAMAGED' ? 'MAINTENANCE' : 'LOST'

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setSaving(true)
    try {
      await inspectOrderItem(branchId, orderId, item.orderItemId, {
        condition,
        actualRentalFee: Number(actualRentalFee),
        processingFee: condition === 'GOOD' ? 0 : Number(processingFee),
        damageNote: damageNote.trim() || null,
        damagePhotoPaths: damagePhotoPaths.split('\n').map((path) => path.trim()).filter(Boolean),
        inventoryOutcome,
      })
      onSaved()
    } catch (error) { onError(error as Error) } finally { setSaving(false) }
  }

  return <form className="inspection-card" onSubmit={submit}>
    <div><strong>{item.productName} · {item.size}</strong><small>{item.assetCode}</small></div>
    <div className="form-grid">
      <label className="field"><span>Tình trạng</span><select value={condition} onChange={(event) => { const value = event.target.value; setCondition(value); if (value === 'GOOD') setProcessingFee('0') }}><option value="GOOD">Tốt</option><option value="DAMAGED">Hư hỏng</option><option value="MISSING">Thất lạc</option></select></label>
      <MoneyField label="Phí thuê thực tế" value={actualRentalFee} onChange={setActualRentalFee} required />
      {condition !== 'GOOD' && <MoneyField label="Phí xử lý/bồi thường" value={processingFee} onChange={setProcessingFee} required />}
      {condition === 'DAMAGED' && <><label className="field"><span>Mô tả hư hỏng</span><textarea value={damageNote} onChange={(event) => setDamageNote(event.target.value)} required /></label><label className="field"><span>Ảnh bằng chứng</span><ImageUploadInput branchId={branchId} purpose="DAMAGE_EVIDENCE" value={damagePhotoPaths} onChange={setDamagePhotoPaths} /></label></>}
    </div>
    <div className="inspection-card__footer"><span>Kho sau kiểm: <strong>{statusLabel(inventoryOutcome)}</strong></span><button className="button" disabled={saving}>{saving ? 'Đang lưu…' : item.condition ? 'Cập nhật' : 'Lưu kiểm tra'}</button></div>
  </form>
}
