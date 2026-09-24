import { useState, type FormEvent } from 'react'
import { AsyncState } from '@/shared/components/AsyncState'
import { formatDateTime } from '@/shared/format/date'
import { formatMoney } from '@/shared/format/money'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import { getPublicRentalForm, submitPublicRentalForm } from './public-rental-form.api'
import type { SubmittedPublicRentalForm } from './public-rental-form.types'

function tokenFromPath() {
  const part = window.location.pathname.replace(/\/+$/, '').split('/')[2]
  return part ? decodeURIComponent(part) : ''
}

export function PublicRentalFormPage() {
  const token = tokenFromPath()
  const form = useApiQuery(
    () => token ? getPublicRentalForm(token) : Promise.reject(new Error('Liên kết xác nhận không hợp lệ.')),
    [token],
  )
  const [customerName, setCustomerName] = useState('')
  const [customerPhone, setCustomerPhone] = useState('')
  const [instagramHandle, setInstagramHandle] = useState('')
  const [tiktokHandle, setTiktokHandle] = useState('')
  const [deliveryAddress, setDeliveryAddress] = useState('')
  const [otp, setOtp] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<Error | null>(null)
  const [submitted, setSubmitted] = useState<SubmittedPublicRentalForm | null>(null)

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!token) return

    setSubmitting(true)
    setSubmitError(null)
    try {
      const result = await submitPublicRentalForm(token, {
        customerName: customerName.trim(),
        customerPhone: customerPhone.trim(),
        instagramHandle: instagramHandle.trim(),
        tiktokHandle: tiktokHandle.trim(),
        deliveryAddress: deliveryAddress.trim(),
        otp: otp.trim(),
      })
      setSubmitted(result)
    } catch (error) {
      setSubmitError(error as Error)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="public-form-page">
      <section className="login-card public-rental-card">
        <div className="brand-mark">A</div>
        <span className="eyebrow">Aura Rental</span>
        <h1>{submitted ? 'Đã xác nhận thuê' : 'Xác nhận thông tin thuê'}</h1>

        <AsyncState loading={form.loading} error={form.error}>
          {form.data && (submitted ? (
            <PublicFormSuccess result={submitted} />
          ) : (
            <form className="login-form" onSubmit={onSubmit}>
              <p className="public-rental-intro">Vui lòng kiểm tra lịch thuê và điền thông tin nhận đồ trước khi xác nhận.</p>

              <dl className="detail-grid public-rental-summary">
                <div><dt>Mã giữ chỗ</dt><dd>{form.data.reservationNo}</dd></div>
                <div><dt>Chi nhánh</dt><dd>{form.data.branch.name}</dd></div>
                <div><dt>Nhận đồ</dt><dd>{formatDateTime(form.data.rentalStartAt)}</dd></div>
                <div><dt>Trả đồ</dt><dd>{formatDateTime(form.data.rentalEndAt)}</dd></div>
              </dl>

              <div className="public-rental-location">
                <strong>Địa chỉ chi nhánh</strong>
                <span>{form.data.branch.address}</span>
              </div>

              <div>
                <h2 className="section-title">Sản phẩm thuê</h2>
                <div className="line-items">
                  {form.data.items.map((item) => (
                    <div key={`${item.assetCode}-${item.packageCode}`}>
                      <span>
                        <strong>{item.productName} · {item.size}</strong>
                        <small>{item.assetCode} · {item.packageLabel}</small>
                      </span>
                      <strong>{formatMoney(item.rentalPrice)}</strong>
                    </div>
                  ))}
                </div>
              </div>

              <div className="quote-summary public-rental-deposit">
                <div><span>Đã xác nhận cọc</span><strong>{formatMoney(form.data.depositConfirmed)}</strong></div>
                <div><span>Còn cần thanh toán</span><strong>{formatMoney(form.data.depositRemaining)}</strong></div>
              </div>

              <label className="field">
                <span>Họ và tên</span>
                <input value={customerName} onChange={(event) => setCustomerName(event.target.value)} autoComplete="name" required />
              </label>
              <label className="field">
                <span>Số điện thoại</span>
                <input value={customerPhone} onChange={(event) => setCustomerPhone(event.target.value)} inputMode="tel" autoComplete="tel" required />
              </label>
              <label className="field">
                <span>Tài khoản Instagram <small>(không bắt buộc)</small></span>
                <input value={instagramHandle} onChange={(event) => setInstagramHandle(event.target.value)} placeholder="@ten_tai_khoan" autoComplete="off" />
              </label>
              <label className="field">
                <span>Tài khoản TikTok <small>(không bắt buộc)</small></span>
                <input value={tiktokHandle} onChange={(event) => setTiktokHandle(event.target.value)} placeholder="@ten_tai_khoan" autoComplete="off" />
              </label>
              <label className="field">
                <span>Địa chỉ giao / nhận đồ</span>
                <textarea value={deliveryAddress} onChange={(event) => setDeliveryAddress(event.target.value)} rows={3} autoComplete="street-address" required />
              </label>
              {form.data.otpRequired && (
                <label className="field">
                  <span>Mã OTP</span>
                  <input value={otp} onChange={(event) => setOtp(event.target.value.replace(/\s/g, ''))} inputMode="numeric" autoComplete="one-time-code" maxLength={6} required placeholder="Nhập mã đã nhận" />
                </label>
              )}

              {submitError && <div className="inline-error" role="alert">{submitError.message}</div>}
              <button className="button button--primary button--block" type="submit" disabled={submitting}>
                {submitting ? 'Đang xác nhận…' : 'Xác nhận thuê'}
              </button>
            </form>
          ))}
        </AsyncState>
      </section>
    </main>
  )
}

function PublicFormSuccess({ result }: { result: SubmittedPublicRentalForm }) {
  return (
    <div className="success-panel public-rental-success">
      <p>{result.message}</p>
      <div className="success-note">Thông tin đã được gửi đến chi nhánh {result.branchName}.</div>
      <dl className="detail-grid">
        <div><dt>Mã đơn thuê</dt><dd>{result.orderNo}</dd></div>
        <div><dt>Thanh toán còn lại</dt><dd>{formatMoney(result.depositRemaining)}</dd></div>
      </dl>
      <p className="public-rental-muted">Nhân viên sẽ liên hệ với bạn để hoàn tất bước bàn giao.</p>
    </div>
  )
}
