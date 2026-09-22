import { useMemo, useState, type FormEvent } from 'react'
import { AsyncState } from '@/shared/components/AsyncState'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import { formatMoney } from '@/shared/format/money'
import { formatDateTime, toLocalDateTimeInput } from '@/shared/format/date'
import { statusLabel } from '@/shared/format/status'
import { useSession } from '@/features/session/SessionProvider'
import { searchCustomers } from '@/features/customers/customer.api'
import { searchAvailability } from '@/features/availability/availability.api'
import type { AvailabilityGroup } from '@/features/availability/availability.types'
import {
  cancelReservation,
  createQuote,
  createReservation,
  extendDeadline,
  getReservation,
  recordReservationPayment,
  reissueOtp,
  searchReservations,
} from './reservation.api'
import type { Quote, RentalSelection, ReservationDetail as ReservationDetailType } from './reservation.types'

const tomorrow = new Date(Date.now() + 24 * 60 * 60 * 1000)
tomorrow.setHours(10, 0, 0, 0)
const dayAfter = new Date(tomorrow.getTime() + 24 * 60 * 60 * 1000)

export function ReservationsPage() {
  const { activeBranchId } = useSession()
  const [status, setStatus] = useState('ACTIVE,OVERDUE')
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const reservations = useApiQuery(
    () => activeBranchId ? searchReservations(activeBranchId, status, query) : Promise.resolve([]),
    [activeBranchId, status, query],
  )

  return (
    <section className="page-stack">
      <header className="page-heading page-heading--actions">
        <div>
          <span className="eyebrow">Quote → nhận cọc → khóa lịch</span>
          <h1>Giữ chỗ</h1>
          <p>Chỉ tạo giữ chỗ sau khi đã nhận và xác nhận giao dịch đầu tiên.</p>
        </div>
        <button className="button button--primary" onClick={() => setShowCreate((value) => !value)}>
          {showCreate ? 'Đóng form' : '+ Tạo giữ chỗ'}
        </button>
      </header>

      {showCreate && activeBranchId && (
        <CreateReservationPanel
          branchId={activeBranchId}
          onCreated={(reservation) => {
            setShowCreate(false)
            setSelectedId(reservation.id)
            void reservations.reload()
          }}
        />
      )}

      <div className="panel filter-bar">
        <label className="field">
          <span>Trạng thái</span>
          <select value={status} onChange={(event) => setStatus(event.target.value)}>
            <option value="ACTIVE,OVERDUE">Đang giữ + quá hạn</option>
            <option value="ACTIVE">Đang giữ</option>
            <option value="OVERDUE">Quá hạn</option>
            <option value="CONVERTED_TO_ORDER">Đã lên đơn</option>
            <option value="CANCELLED">Đã hủy</option>
            <option value="">Tất cả</option>
          </select>
        </label>
        <label className="field filter-bar__search">
          <span>Tìm reservation hoặc khách</span>
          <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="RSV…, tên, số điện thoại" />
        </label>
      </div>

      <div className="module-layout">
        <AsyncState loading={reservations.loading} error={reservations.error} empty={reservations.data?.length === 0}>
          <div className="result-list">
            {reservations.data?.map((item) => (
              <button
                className={`panel operation-card ${selectedId === item.id ? 'operation-card--selected' : ''}`}
                type="button"
                key={item.id}
                onClick={() => setSelectedId(item.id)}
              >
                <div className="operation-card__top">
                  <strong>{item.reservationNo}</strong>
                  <span className="status-pill">{statusLabel(item.status)}</span>
                </div>
                <h2>{item.customerName}</h2>
                <p>{item.itemSummary}</p>
                <div className="operation-card__meta">
                  <span>{formatDateTime(item.rentalStartAt)} → {formatDateTime(item.rentalEndAt)}</span>
                  <strong>Còn cọc {formatMoney(item.depositRemaining)}</strong>
                </div>
              </button>
            ))}
          </div>
        </AsyncState>

        {selectedId && activeBranchId ? (
          <ReservationDetail branchId={activeBranchId} reservationId={selectedId} onChanged={() => void reservations.reload()} />
        ) : (
          <div className="state-card detail-empty">Chọn một reservation để xem chi tiết và thao tác.</div>
        )}
      </div>
    </section>
  )
}

type SelectedAsset = RentalSelection & {
  assetCode: string
  productName: string
  size: string
  prices: AvailabilityGroup['prices']
}

function CreateReservationPanel({ branchId, onCreated }: { branchId: string; onCreated: (reservation: ReservationDetailType) => void }) {
  const [customerQuery, setCustomerQuery] = useState('')
  const [customers, setCustomers] = useState<Awaited<ReturnType<typeof searchCustomers>>>([])
  const [customerId, setCustomerId] = useState('')
  const [startAt, setStartAt] = useState(toLocalDateTimeInput(tomorrow))
  const [endAt, setEndAt] = useState(toLocalDateTimeInput(dayAfter))
  const [availability, setAvailability] = useState<AvailabilityGroup[]>([])
  const [selected, setSelected] = useState<SelectedAsset[]>([])
  const [depositPlan, setDepositPlan] = useState('FULL')
  const [paymentType, setPaymentType] = useState<'SLOT_DEPOSIT' | 'TARGET_DEPOSIT'>('SLOT_DEPOSIT')
  const [deadline, setDeadline] = useState(toLocalDateTimeInput(new Date(Date.now() + 4 * 60 * 60 * 1000)))
  const [method, setMethod] = useState('BANK_TRANSFER')
  const [transactionRef, setTransactionRef] = useState('')
  const [quote, setQuote] = useState<Quote | null>(null)
  const [created, setCreated] = useState<ReservationDetailType | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)

  const findCustomers = async () => {
    setError(null)
    try { setCustomers(await searchCustomers(customerQuery.trim())) } catch (nextError) { setError(nextError as Error) }
  }

  const findInventory = async () => {
    setLoading(true)
    setError(null)
    setQuote(null)
    try {
      const result = await searchAvailability(branchId, { query: '', size: '', startAt, endAt })
      setAvailability(result.groups)
    } catch (nextError) { setError(nextError as Error) } finally { setLoading(false) }
  }

  const toggleAsset = (group: AvailabilityGroup, inventoryItemId: string, assetCode: string) => {
    setQuote(null)
    setSelected((current) => current.some((item) => item.inventoryItemId === inventoryItemId)
      ? current.filter((item) => item.inventoryItemId !== inventoryItemId)
      : [...current, { inventoryItemId, assetCode, productName: group.productName, size: group.size, packageCode: group.prices[0]?.packageCode ?? '1D', prices: group.prices }])
  }

  const quoteInput = useMemo(() => ({
    customerId,
    rentalStartAt: startAt,
    rentalEndAt: endAt,
    depositPlan,
    items: selected.map(({ inventoryItemId, packageCode }) => ({ inventoryItemId, packageCode })),
  }), [customerId, depositPlan, endAt, selected, startAt])

  const preview = async (event: FormEvent) => {
    event.preventDefault()
    setLoading(true)
    setError(null)
    try { setQuote(await createQuote(branchId, quoteInput)) } catch (nextError) { setError(nextError as Error) } finally { setLoading(false) }
  }

  const confirm = async () => {
    if (!quote) return
    setLoading(true)
    setError(null)
    try {
      setCreated(await createReservation(branchId, {
        ...quoteInput,
        depositDeadlineAt: paymentType === 'SLOT_DEPOSIT' ? deadline : null,
        receivedPayment: {
          type: paymentType,
          amount: paymentType === 'SLOT_DEPOSIT' ? quote.slotDepositAmount : quote.depositRequired,
          method,
          transactionRef: transactionRef.trim() || null,
          proofPath: null,
          paidAt: new Date().toISOString(),
        },
      }))
    } catch (nextError) { setError(nextError as Error) } finally { setLoading(false) }
  }

  if (created) {
    return (
      <section className="panel success-panel">
        <span className="eyebrow">Đã khóa lịch thành công</span>
        <h2>{created.reservationNo}</h2>
        <p>Gửi link và OTP cho khách qua Instagram.</p>
        <div className="credential-box"><code>{created.customerForm?.url}</code><strong>OTP: {created.customerForm?.otp}</strong><small>Hết hạn {formatDateTime(created.customerForm?.expiresAt)}</small></div>
        <button className="button button--primary" onClick={() => onCreated(created)}>Xong</button>
      </section>
    )
  }

  return (
    <form className="panel create-flow" onSubmit={preview}>
      <div className="section-heading"><div><span className="eyebrow">Bước 1</span><h2>Khách và lịch thuê</h2></div></div>
      <div className="inline-search"><input value={customerQuery} onChange={(event) => setCustomerQuery(event.target.value)} placeholder="Tên, SĐT hoặc Instagram" /><button className="button" type="button" onClick={() => void findCustomers()}>Tìm khách</button></div>
      {customers.length > 0 && <div className="choice-list">{customers.map((customer) => <label key={customer.id} className={customerId === customer.id ? 'choice-card choice-card--selected' : 'choice-card'}><input type="radio" name="customer" checked={customerId === customer.id} onChange={() => { setCustomerId(customer.id); setQuote(null) }} /><span><strong>{customer.name}</strong><small>{customer.phone} · {customer.instagramHandle ?? 'Không có Instagram'}</small></span></label>)}</div>}

      <div className="form-grid">
        <label className="field"><span>Nhận đồ</span><input type="datetime-local" value={startAt} onChange={(event) => { setStartAt(event.target.value); setQuote(null) }} required /></label>
        <label className="field"><span>Trả đồ</span><input type="datetime-local" value={endAt} onChange={(event) => { setEndAt(event.target.value); setQuote(null) }} required /></label>
      </div>
      <button className="button" type="button" onClick={() => void findInventory()} disabled={loading}>Kiểm tra đồ trống</button>

      {availability.length > 0 && <div className="compact-products">{availability.map((group) => <article key={group.variantId}><strong>{group.productName} · {group.size}</strong><div className="asset-row">{group.items.map((item) => <button className={selected.some((selectedItem) => selectedItem.inventoryItemId === item.inventoryItemId) ? 'asset-selected' : ''} type="button" key={item.inventoryItemId} onClick={() => toggleAsset(group, item.inventoryItemId, item.assetCode)}>{item.assetCode}</button>)}</div></article>)}</div>}

      {selected.length > 0 && <div className="selected-items">{selected.map((item) => <label className="field selected-item" key={item.inventoryItemId}><span>{item.productName} · {item.size} · {item.assetCode}</span><select value={item.packageCode} onChange={(event) => { setQuote(null); setSelected((current) => current.map((candidate) => candidate.inventoryItemId === item.inventoryItemId ? { ...candidate, packageCode: event.target.value } : candidate)) }}>{item.prices.map((price) => <option value={price.packageCode} key={price.packageCode}>{price.label} · {formatMoney(price.price)}</option>)}</select></label>)}</div>}

      <div className="form-grid">
        <label className="field"><span>Gói cọc</span><select value={depositPlan} onChange={(event) => { setDepositPlan(event.target.value); setQuote(null) }}><option value="FULL">Cọc 100%</option><option value="FIFTY_WITH_ID">Cọc 50% + CCCD</option></select></label>
        <label className="field"><span>Đã nhận</span><select value={paymentType} onChange={(event) => setPaymentType(event.target.value as typeof paymentType)}><option value="SLOT_DEPOSIT">Cọc giữ chỗ 100.000đ</option><option value="TARGET_DEPOSIT">Nhận đủ cọc đích</option></select></label>
        {paymentType === 'SLOT_DEPOSIT' && <label className="field"><span>Hạn chốt đủ cọc</span><input type="datetime-local" value={deadline} onChange={(event) => setDeadline(event.target.value)} required /></label>}
        <label className="field"><span>Phương thức</span><select value={method} onChange={(event) => setMethod(event.target.value)}><option value="BANK_TRANSFER">Chuyển khoản</option><option value="CASH">Tiền mặt</option></select></label>
        <label className="field"><span>Mã giao dịch</span><input value={transactionRef} onChange={(event) => setTransactionRef(event.target.value)} placeholder="Tùy chọn" /></label>
      </div>

      {error && <div className="inline-error">{error.message}</div>}
      {quote ? <div className="quote-summary"><div><span>Phí thuê</span><strong>{formatMoney(quote.rentalFee)}</strong></div><div><span>Cọc yêu cầu</span><strong>{formatMoney(quote.depositRequired)}</strong></div><div><span>Thu ngay</span><strong>{formatMoney(paymentType === 'SLOT_DEPOSIT' ? quote.slotDepositAmount : quote.depositRequired)}</strong></div><button className="button button--primary" type="button" onClick={() => void confirm()} disabled={loading}>Xác nhận đã nhận tiền và giữ chỗ</button></div> : <button className="button button--primary" type="submit" disabled={loading || !customerId || selected.length === 0}>{loading ? 'Đang tính…' : 'Xem báo giá'}</button>}
    </form>
  )
}

function ReservationDetail({ branchId, reservationId, onChanged }: { branchId: string; reservationId: string; onChanged: () => void }) {
  const [version, setVersion] = useState(0)
  const [message, setMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<Error | null>(null)
  const detail = useApiQuery(() => getReservation(branchId, reservationId), [branchId, reservationId, version])
  const run = async (action: () => Promise<unknown>, success: string) => {
    setActionError(null)
    try { await action(); setMessage(success); setVersion((value) => value + 1); onChanged() } catch (error) { setActionError(error as Error) }
  }
  const reissueCredential = async (reason: string) => {
    setActionError(null)
    try {
      const credential = await reissueOtp(branchId, reservationId, reason)
      setMessage(`OTP mới: ${credential.otp} · ${credential.formUrl}`)
      setVersion((value) => value + 1)
      onChanged()
    } catch (error) {
      setActionError(error as Error)
    }
  }
  const reservation = detail.data
  return (
    <AsyncState loading={detail.loading} error={detail.error}>
      {reservation && <aside className="panel detail-panel">
        <div className="operation-card__top"><strong>{reservation.reservationNo}</strong><span className="status-pill">{statusLabel(reservation.status)}</span></div>
        <h2>{reservation.customer.name}</h2><p>{reservation.customer.phone} · {reservation.branch.name}</p>
        <dl className="detail-grid"><div><dt>Lịch thuê</dt><dd>{formatDateTime(reservation.rentalStartAt)}<br />→ {formatDateTime(reservation.rentalEndAt)}</dd></div><div><dt>Đã nhận cọc</dt><dd>{formatMoney(reservation.deposit.confirmedReceived)}</dd></div><div><dt>Còn lại</dt><dd>{formatMoney(reservation.deposit.remaining)}</dd></div><div><dt>Hạn cọc</dt><dd>{formatDateTime(reservation.deposit.deadlineAt)}</dd></div></dl>
        <div className="line-items">{reservation.items.map((item) => <div key={item.inventoryItemId}><span>{item.productName} · {item.size}<small>{item.assetCode} · {item.packageCode}</small></span><strong>{formatMoney(item.rentalPrice)}</strong></div>)}</div>
        {message && <div className="success-note">{message}</div>}{actionError && <div className="inline-error">{actionError.message}</div>}
        {(reservation.status === 'ACTIVE' || reservation.status === 'OVERDUE') && reservation.formStatus === 'NOT_SUBMITTED' && <div className="action-buttons">
          {reservation.deposit.remaining > 0 && <button className="button button--primary" onClick={() => { const value = Number(window.prompt('Số tiền cọc vừa nhận', String(reservation.deposit.remaining))); if (value > 0) void run(() => recordReservationPayment(branchId, reservation.id, 'TARGET_DEPOSIT', value), 'Đã ghi nhận cọc.') }}>Ghi nhận cọc</button>}
          <button className="button" onClick={() => { const reason = window.prompt('Lý do cấp lại link/OTP'); if (reason) void reissueCredential(reason) }}>Cấp lại OTP</button>
          <button className="button" onClick={() => { const date = window.prompt('Hạn cọc mới (YYYY-MM-DDTHH:mm)', toLocalDateTimeInput(new Date(Date.now() + 4 * 60 * 60 * 1000))); const reason = date && window.prompt('Lý do gia hạn'); if (date && reason) void run(() => extendDeadline(branchId, reservation.id, date, reason), 'Đã gia hạn cọc.') }}>Gia hạn</button>
          <button className="button button--danger" onClick={() => { const reason = window.prompt('Lý do hủy reservation'); if (reason && window.confirm('Xác nhận hủy giữ chỗ?')) void run(() => cancelReservation(branchId, reservation.id, reason), 'Đã hủy giữ chỗ.') }}>Hủy</button>
        </div>}
      </aside>}
    </AsyncState>
  )
}
