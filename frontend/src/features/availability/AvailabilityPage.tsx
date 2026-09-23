import { useState, type FormEvent } from 'react'
import { AsyncState } from '@/shared/components/AsyncState'
import { formatDateTime } from '@/shared/format/date'
import { formatMoney } from '@/shared/format/money'
import { useSession } from '@/features/session/SessionProvider'
import { searchAvailability } from './availability.api'
import type { AvailabilityCriteria, AvailabilityResult } from './availability.types'

type SelectedAvailabilityAsset = {
  inventoryItemId: number
  assetCode: string
  productName: string
  size: string
  packageCode: string
  prices: AvailabilityResult['groups'][number]['prices']
}

const toLocalInput = (date: Date) => {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 16)
}

const initialStart = new Date(Date.now() + 24 * 60 * 60 * 1000)
initialStart.setHours(10, 0, 0, 0)
const initialEnd = new Date(initialStart.getTime() + 3 * 24 * 60 * 60 * 1000)

export function AvailabilityPage() {
  const { activeBranchId } = useSession()
  const [criteria, setCriteria] = useState<AvailabilityCriteria>({
    query: '',
    size: '',
    startAt: toLocalInput(initialStart),
    endAt: toLocalInput(initialEnd),
  })
  const [result, setResult] = useState<AvailabilityResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  const [selected, setSelected] = useState<SelectedAvailabilityAsset[]>([])

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!activeBranchId) return
    setLoading(true)
    setError(null)
    setSelected([])
    try {
      setResult(await searchAvailability(activeBranchId, criteria))
    } catch (nextError) {
      setError(nextError as Error)
    } finally {
      setLoading(false)
    }
  }

  const toggleAsset = (group: AvailabilityResult['groups'][number], item: AvailabilityResult['groups'][number]['items'][number]) => {
    if (!item.availableForWholePeriod) return
    setSelected((current) => current.some((asset) => asset.inventoryItemId === item.inventoryItemId)
      ? current.filter((asset) => asset.inventoryItemId !== item.inventoryItemId)
      : [...current, {
          inventoryItemId: item.inventoryItemId,
          assetCode: item.assetCode,
          productName: group.productName,
          size: group.size,
          packageCode: group.prices[0]?.packageCode ?? '1D',
          prices: group.prices,
        }])
  }

  const continueToReservation = () => {
    if (!activeBranchId || selected.length === 0) return
    let existing: Record<string, unknown> = {}
    try {
      const stored = sessionStorage.getItem(`aura.reservationDraft.${activeBranchId}`)
      if (stored) existing = JSON.parse(stored) as Record<string, unknown>
    } catch { /* Start a clean draft if an older draft cannot be read. */ }
    const previous = Array.isArray(existing.selected) ? existing.selected as SelectedAvailabilityAsset[] : []
    const merged = [...previous, ...selected.filter((asset) => !previous.some((current) => current.inventoryItemId === asset.inventoryItemId))]
    sessionStorage.setItem(`aura.reservationDraft.${activeBranchId}`, JSON.stringify({
      ...existing,
      startAt: criteria.startAt,
      endAt: criteria.endAt,
      selected: merged,
    }))
    window.location.assign('/reservations')
  }

  return (
    <section className="page-stack">
      <header className="page-heading">
        <span className="eyebrow">Tư vấn qua Instagram</span>
        <h1>Tìm đồ theo lịch</h1>
        <p>Xem cả đồ trống và đồ đang bận. Kết quả tra cứu chưa giữ đồ.</p>
      </header>

      <form className="panel search-form" onSubmit={submit}>
        <label className="field field--wide">
          <span>Tên hoặc mã đồ</span>
          <input
            value={criteria.query}
            onChange={(event) => setCriteria({ ...criteria, query: event.target.value })}
            placeholder="Afrodille, SG-AF-GAM…"
          />
        </label>
        <label className="field">
          <span>Size</span>
          <input
            value={criteria.size}
            onChange={(event) => setCriteria({ ...criteria, size: event.target.value })}
            placeholder="S, M, L"
          />
        </label>
        <label className="field">
          <span>Nhận đồ</span>
          <input
            type="datetime-local"
            value={criteria.startAt}
            onChange={(event) => setCriteria({ ...criteria, startAt: event.target.value })}
            required
          />
        </label>
        <label className="field">
          <span>Trả đồ</span>
          <input
            type="datetime-local"
            value={criteria.endAt}
            onChange={(event) => setCriteria({ ...criteria, endAt: event.target.value })}
            required
          />
        </label>
        <button className="button button--primary" type="submit" disabled={loading || !activeBranchId}>
          {loading ? 'Đang kiểm tra…' : 'Kiểm tra lịch đồ'}
        </button>
      </form>

      {selected.length > 0 && <section className="panel availability-selection">
        <div><span className="eyebrow">Đã chọn {selected.length} mã trống</span><strong>{selected.map((item) => item.assetCode).join(' · ')}</strong><p>Đã nhận cọc? Chuyển sang Giữ chỗ để chọn gói thuê và xác nhận giao dịch.</p></div>
        <button className="button button--primary" type="button" onClick={continueToReservation}>Tạo giữ chỗ với {selected.length} mã</button>
      </section>}

      {(loading || error || result) && (
        <AsyncState loading={loading} error={error} empty={result?.groups.length === 0}>
          <div className="result-list">
            {result?.groups.map((group) => (
              <article className="panel product-card" key={group.variantId}>
                <div className="product-card__heading">
                  <div>
                    <h2>{group.productName} · {group.size}</h2>
                    <p>{group.measurements ?? 'Chưa có số đo'}</p>
                  </div>
                  <span className="availability-pill">{group.availableCount}/{group.items.length} mã trống</span>
                </div>
                <div className="price-row">
                  {group.prices.map((price) => (
                    <span key={price.packageCode}>{price.label} · {formatMoney(price.price)}</span>
                  ))}
                </div>
                <div className="availability-assets">
                  {group.items.map((item) => (
                    <div className={item.availableForWholePeriod ? 'availability-asset' : 'availability-asset availability-asset--busy'} key={item.inventoryItemId}>
                      <div><strong>{item.assetCode}</strong><span>{item.availableForWholePeriod ? 'Trống toàn bộ lịch' : item.availabilityNote}</span></div>
                      <div className="availability-asset__actions">
                        <span className={item.availableForWholePeriod ? 'availability-state availability-state--free' : 'availability-state availability-state--busy'}>
                          {item.availableForWholePeriod ? 'Có thể chọn' : 'Đang bận'}
                        </span>
                        {item.availableForWholePeriod && <button className={selected.some((asset) => asset.inventoryItemId === item.inventoryItemId) ? 'button button--small button--primary' : 'button button--small'} type="button" onClick={() => toggleAsset(group, item)}>{selected.some((asset) => asset.inventoryItemId === item.inventoryItemId) ? 'Đã chọn' : 'Chọn'}</button>}
                      </div>
                      {!item.availableForWholePeriod && (item.busyUntil || item.referenceNo) && (
                        <small>
                          {item.referenceNo ? `Mã ${item.referenceNo}` : ''}
                          {item.referenceNo && item.busyUntil ? ' · ' : ''}
                          {item.busyUntil ? `Bận đến ${formatDateTime(item.busyUntil)}` : ''}
                        </small>
                      )}
                    </div>
                  ))}
                </div>
              </article>
            ))}
          </div>
        </AsyncState>
      )}
    </section>
  )
}
