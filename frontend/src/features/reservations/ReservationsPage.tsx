import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { AsyncState } from '@/shared/components/AsyncState'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import { formatMoney } from '@/shared/format/money'
import { formatDateTime, toLocalDateTimeInput } from '@/shared/format/date'
import { statusLabel } from '@/shared/format/status'
import { StoredImage } from '@/shared/components/StoredImage'
import { MoneyDialog, MoneyField } from '@/shared/components/MoneyInput'
import { useSession } from '@/features/session/SessionProvider'
import { searchAvailability } from '@/features/availability/availability.api'
import type { AvailabilityGroup } from '@/features/availability/availability.types'
import {
  cancelReservation,
  createQuote,
  createReservation,
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
  const [status, setStatus] = useState('ACTIVE')
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const reservations = useApiQuery(
    () => activeBranchId ? searchReservations(activeBranchId, status, query) : Promise.resolve([]),
    [activeBranchId, status, query],
  )

  useEffect(() => {
    if (activeBranchId && sessionStorage.getItem(`aura.reservationDraft.${activeBranchId}`)) setShowCreate(true)
  }, [activeBranchId])

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
          key={activeBranchId}
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
            <option value="ACTIVE">Đang giữ</option>
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

type ReservationDraft = {
  startAt?: string
  endAt?: string
  selected?: SelectedAsset[]
  depositPlan?: string
  paymentType?: 'SLOT_DEPOSIT' | 'TARGET_DEPOSIT' | 'CUSTOM_DEPOSIT'
  customDeposit?: string
  method?: string
}

function readReservationDraft(branchId: number): ReservationDraft {
  try {
    const value = sessionStorage.getItem(`aura.reservationDraft.${branchId}`)
    if (!value) return {}
    const draft = JSON.parse(value) as ReservationDraft
    return {
      ...draft,
      selected: Array.isArray(draft.selected) ? draft.selected : [],
      paymentType: draft.paymentType === 'TARGET_DEPOSIT' || draft.paymentType === 'CUSTOM_DEPOSIT' ? draft.paymentType : 'SLOT_DEPOSIT',
      customDeposit: typeof draft.customDeposit === 'string' ? draft.customDeposit.replace(/\D/g, '') : '',
    }
  } catch {
    return {}
  }
}

function CreateReservationPanel({ branchId, onCreated }: { branchId: number; onCreated: (reservation: ReservationDetailType) => void }) {
  const draft = useMemo(() => readReservationDraft(branchId), [branchId])
  const [startAt, setStartAt] = useState(draft.startAt ?? toLocalDateTimeInput(tomorrow))
  const [endAt, setEndAt] = useState(draft.endAt ?? toLocalDateTimeInput(dayAfter))
  const [assetCode, setAssetCode] = useState('')
  const [lookupResults, setLookupResults] = useState<AvailabilityGroup[]>([])
  const [selected, setSelected] = useState<SelectedAsset[]>(draft.selected ?? [])
  const [depositPlan, setDepositPlan] = useState(draft.depositPlan ?? 'FULL')
  const [paymentType, setPaymentType] = useState<'SLOT_DEPOSIT' | 'TARGET_DEPOSIT' | 'CUSTOM_DEPOSIT'>(draft.paymentType ?? 'SLOT_DEPOSIT')
  const [customDeposit, setCustomDeposit] = useState(draft.customDeposit ?? '')
  const [method, setMethod] = useState(draft.method ?? 'BANK_TRANSFER')
  const [quote, setQuote] = useState<Quote | null>(null)
  const [created, setCreated] = useState<ReservationDetailType | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)

  useEffect(() => {
    sessionStorage.setItem(`aura.reservationDraft.${branchId}`, JSON.stringify({ startAt, endAt, selected, depositPlan, paymentType, customDeposit, method }))
  }, [branchId, customDeposit, depositPlan, endAt, method, paymentType, selected, startAt])

  const lookupAssets = async () => {
    const codes = [...new Set(assetCode.split(/[\s,;]+/).map((code) => code.trim()).filter(Boolean))]
    if (codes.length === 0) {
      setError(new Error('Nhập hoặc dán ít nhất một mã đồ để xem thông tin.'))
      return
    }
    setLoading(true)
    setError(null)
    try {
      const results = await Promise.all(codes.map((code) => searchAvailability(branchId, { query: code, size: '', startAt, endAt })))
      const matchingGroups = results.flatMap((result, index) => result.groups
        .map((group) => ({ ...group, items: group.items.filter((item) => item.assetCode.toLowerCase() === codes[index].toLowerCase()) }))
        .filter((group) => group.items.length > 0))
      const foundCodes = new Set(matchingGroups.flatMap((group) => group.items.map((item) => item.assetCode.toLowerCase())))
      const missingCodes = codes.filter((code) => !foundCodes.has(code.toLowerCase()))
      setLookupResults((current) => {
        const addedIds = new Set(matchingGroups.flatMap((group) => group.items.map((item) => item.inventoryItemId)))
        return [...current.filter((group) => !group.items.some((item) => addedIds.has(item.inventoryItemId))), ...matchingGroups]
      })
      setAssetCode('')
      if (missingCodes.length > 0) setError(new Error(`Không tìm thấy: ${missingCodes.join(', ')}.`))
    } catch (nextError) { setError(nextError as Error) } finally { setLoading(false) }
  }

  const toggleAsset = (group: AvailabilityGroup, inventoryItemId: number, assetCode: string) => {
    setQuote(null)
    setSelected((current) => current.some((item) => item.inventoryItemId === inventoryItemId)
      ? current.filter((item) => item.inventoryItemId !== inventoryItemId)
      : [...current, { inventoryItemId, assetCode, productName: group.productName, size: group.size, packageCode: group.prices[0]?.packageCode ?? '1D', prices: group.prices }])
  }

  const quoteInput = useMemo(() => ({
    rentalStartAt: startAt,
    rentalEndAt: endAt,
    depositPlan,
    items: selected.map(({ inventoryItemId, packageCode }) => ({ inventoryItemId, packageCode })),
  }), [depositPlan, endAt, selected, startAt])

  const preview = async (event: FormEvent) => {
    event.preventDefault()
    setLoading(true)
    setError(null)
    try { setQuote(await createQuote(branchId, quoteInput)) } catch (nextError) { setError(nextError as Error) } finally { setLoading(false) }
  }

  const confirm = async () => {
    if (!quote) return
    const receivedAmount = paymentType === 'SLOT_DEPOSIT'
      ? quote.slotDepositAmount
      : paymentType === 'TARGET_DEPOSIT'
        ? quote.depositRequired
        : Number(customDeposit)
    if (paymentType === 'CUSTOM_DEPOSIT' && (!Number.isFinite(receivedAmount) || receivedAmount <= quote.slotDepositAmount)) {
      setError(new Error(`Mức cọc tự nhập phải lớn hơn ${formatMoney(quote.slotDepositAmount)}.`))
      return
    }
    if (paymentType === 'CUSTOM_DEPOSIT' && receivedAmount > quote.depositRequired) {
      setError(new Error(`Mức cọc tự nhập không được vượt ${formatMoney(quote.depositRequired)}.`))
      return
    }
    setLoading(true)
    setError(null)
    try {
      setCreated(await createReservation(branchId, {
        ...quoteInput,
        receivedPayment: {
          type: paymentType === 'SLOT_DEPOSIT' ? 'SLOT_DEPOSIT' : 'TARGET_DEPOSIT',
          amount: receivedAmount,
          method,
          transactionRef: null,
          proofPath: null,
          paidAt: new Date().toISOString(),
        },
      }))
      sessionStorage.removeItem(`aura.reservationDraft.${branchId}`)
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
      <div className="section-heading"><div><span className="eyebrow">Bước 1</span><h2>Lịch thuê và mã đồ</h2><p>Thêm nhiều váy hoặc phụ kiện liên tiếp; form này tự lưu nháp khi bạn chuyển sang Tìm đồ.</p></div></div>

      <div className="form-grid">
        <label className="field"><span>Nhận đồ</span><input type="datetime-local" value={startAt} onChange={(event) => { setStartAt(event.target.value); setLookupResults([]); setQuote(null) }} required /></label>
        <label className="field"><span>Trả đồ</span><input type="datetime-local" value={endAt} onChange={(event) => { setEndAt(event.target.value); setLookupResults([]); setQuote(null) }} required /></label>
      </div>
      <div className="asset-lookup">
        <label className="field"><span>Thêm mã đồ</span><textarea rows={2} value={assetCode} onChange={(event) => setAssetCode(event.target.value)} placeholder={'Dán một hoặc nhiều mã, mỗi dòng một mã\nVí dụ: HN-D-001\nHN-AC-015'} /></label>
        <button className="button" type="button" onClick={() => void lookupAssets()} disabled={loading}>Thêm vào form</button>
      </div>
      <a className="draft-search-link" href="/availability">Cần tìm thêm? Mở Tìm đồ — form này đã được lưu nháp</a>

      {lookupResults.map((group) => group.items.map((item) => <article className="asset-preview" key={item.inventoryItemId}>
        {group.imagePaths[0] ? <StoredImage branchId={branchId} objectPath={group.imagePaths[0]} alt={group.productName} className="asset-preview__image" /> : <div className="asset-preview__placeholder">Chưa có ảnh</div>}
        <div><span className="eyebrow">{item.assetCode}</span><h3>{group.productName} · {group.size}</h3><p>{item.availableForWholePeriod ? 'Trống trong lịch đã chọn' : item.availabilityNote ?? 'Không trống trong lịch đã chọn'}</p></div>
        <button className={`${selected.some((selectedItem) => selectedItem.inventoryItemId === item.inventoryItemId) ? 'button button--primary' : 'button'}`} type="button" disabled={!item.availableForWholePeriod} onClick={() => toggleAsset(group, item.inventoryItemId, item.assetCode)}>{selected.some((selectedItem) => selectedItem.inventoryItemId === item.inventoryItemId) ? 'Đã chọn' : 'Chọn váy'}</button>
      </article>))}

      {selected.length > 0 && <div className="selected-items"><strong>Đã chọn {selected.length} mã</strong>{selected.map((item) => <div className="selected-item" key={item.inventoryItemId}><label className="field"><span>{item.productName} · {item.size} · {item.assetCode}</span><select value={item.packageCode} onChange={(event) => { setQuote(null); setSelected((current) => current.map((candidate) => candidate.inventoryItemId === item.inventoryItemId ? { ...candidate, packageCode: event.target.value } : candidate)) }}>{item.prices.map((price) => <option value={price.packageCode} key={price.packageCode}>{price.label} · {formatMoney(price.price)}</option>)}</select></label><button className="button button--small" type="button" onClick={() => { setQuote(null); setSelected((current) => current.filter((candidate) => candidate.inventoryItemId !== item.inventoryItemId)) }}>Bỏ</button></div>)}</div>}

      <div className="form-grid">
        <label className="field"><span>Gói cọc</span><select value={depositPlan} onChange={(event) => { setDepositPlan(event.target.value); setQuote(null) }}><option value="FULL">Cọc 100%</option><option value="FIFTY_WITH_ID">Cọc 50% + CCCD</option></select></label>
        <label className="field"><span>Mức cọc đã nhận</span><select value={paymentType} onChange={(event) => setPaymentType(event.target.value as typeof paymentType)}><option value="SLOT_DEPOSIT">Cọc giữ chỗ 100.000đ</option><option value="TARGET_DEPOSIT">Nhận đủ cọc đích</option><option value="CUSTOM_DEPOSIT">Nhập mức cọc khác</option></select></label>
        {paymentType === 'CUSTOM_DEPOSIT' && <MoneyField label="Mức cọc tự nhập" value={customDeposit} onChange={(value) => { setCustomDeposit(value); setError(null) }} required />}
        <label className="field"><span>Phương thức</span><select value={method} onChange={(event) => setMethod(event.target.value)}><option value="BANK_TRANSFER">Chuyển khoản</option><option value="CASH">Tiền mặt</option></select></label>
        <label className="field"><span>Mã giao dịch</span><input value="Tự sinh khi xác nhận" readOnly aria-label="Mã giao dịch được tự sinh" /></label>
      </div>

      {error && <div className="inline-error">{error.message}</div>}
      {quote ? <div className="quote-summary"><div><span>Phí thuê</span><strong>{formatMoney(quote.rentalFee)}</strong></div><div><span>Cọc yêu cầu</span><strong>{formatMoney(quote.depositRequired)}</strong></div><div><span>Thu ngay</span><strong>{formatMoney(paymentType === 'SLOT_DEPOSIT' ? quote.slotDepositAmount : paymentType === 'TARGET_DEPOSIT' ? quote.depositRequired : Number(customDeposit) || 0)}</strong></div>{paymentType === 'CUSTOM_DEPOSIT' && <small>Nhập mức lớn hơn {formatMoney(quote.slotDepositAmount)} và không quá {formatMoney(quote.depositRequired)}.</small>}<button className="button button--primary" type="button" onClick={() => void confirm()} disabled={loading}>Xác nhận đã nhận tiền và giữ chỗ</button></div> : <button className="button button--primary" type="submit" disabled={loading || selected.length === 0}>{loading ? 'Đang tính…' : 'Xem báo giá'}</button>}
    </form>
  )
}

function ReservationDetail({ branchId, reservationId, onChanged }: { branchId: number; reservationId: number; onChanged: () => void }) {
  const [version, setVersion] = useState(0)
  const [message, setMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<Error | null>(null)
  const [paymentDialogOpen, setPaymentDialogOpen] = useState(false)

  useEffect(() => {
    setMessage(null)
    setActionError(null)
  }, [reservationId])

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
        <h2>{reservation.customer?.name ?? 'Chờ khách điền form'}</h2><p>{reservation.customer?.phone ?? 'Chưa có SĐT'} · {reservation.branch.name}</p>
        <dl className="detail-grid"><div><dt>Lịch thuê</dt><dd>{formatDateTime(reservation.rentalStartAt)}<br />→ {formatDateTime(reservation.rentalEndAt)}</dd></div><div><dt>Đã nhận cọc</dt><dd>{formatMoney(reservation.deposit.confirmedReceived)}</dd></div><div><dt>Còn lại</dt><dd>{formatMoney(reservation.deposit.remaining)}</dd></div></dl>
        <div className="line-items">{reservation.items.map((item) => <div key={item.inventoryItemId}><span>{item.productName} · {item.size}<small>{item.assetCode} · {item.packageCode}</small></span><strong>{formatMoney(item.rentalPrice)}</strong></div>)}</div>
        {message && <div className="success-note">{message}</div>}{actionError && <div className="inline-error">{actionError.message}</div>}
        {reservation.status === 'ACTIVE' && reservation.formStatus === 'NOT_SUBMITTED' && <div className="action-buttons">
          {reservation.deposit.remaining > 0 && <button className="button button--primary" onClick={() => setPaymentDialogOpen(true)}>Ghi nhận cọc</button>}
          <button className="button" onClick={() => { const reason = window.prompt('Lý do cấp lại link/OTP'); if (reason) void reissueCredential(reason) }}>Cấp lại OTP</button>
          <button className="button button--danger" onClick={() => { const reason = window.prompt('Lý do hủy reservation'); if (reason && window.confirm('Xác nhận hủy giữ chỗ?')) void run(() => cancelReservation(branchId, reservation.id, reason), 'Đã hủy giữ chỗ.') }}>Hủy</button>
        </div>}
        {paymentDialogOpen && <MoneyDialog title="Ghi nhận cọc vừa nhận" initialAmount={reservation.deposit.remaining} confirmLabel="Xác nhận ghi nhận" onClose={() => setPaymentDialogOpen(false)} onConfirm={(amount) => { setPaymentDialogOpen(false); void run(() => recordReservationPayment(branchId, reservation.id, 'TARGET_DEPOSIT', amount), 'Đã ghi nhận cọc.') }} />}
      </aside>}
    </AsyncState>
  )
}
