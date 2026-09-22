export function PublicRentalFormPage() {
  return (
    <main className="public-form-page">
      <section className="login-card">
        <div className="brand-mark">A</div>
        <span className="eyebrow">Form thuê đồ</span>
        <h1>Xác nhận thông tin thuê</h1>
        <p>Route public đã được tách khỏi dashboard. Module này sẽ gọi API token/OTP mà không gửi header chi nhánh.</p>
        <div className="state-card">API public form thuộc phase tiếp theo của backend.</div>
      </section>
    </main>
  )
}
