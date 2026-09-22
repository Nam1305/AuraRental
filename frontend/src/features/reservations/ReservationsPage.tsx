import { ModulePlaceholder } from '@/shared/components/ModulePlaceholder'

export function ReservationsPage() {
  return <ModulePlaceholder
    eyebrow="Quote → cọc → OTP"
    title="Giữ chỗ"
    description="Tạo reservation sau khi staff đã xác nhận tiền; thao tác này mới khóa lịch mã đồ."
    endpoints={['POST /api/v1/quotes', 'POST /api/v1/reservations', 'POST /api/v1/reservations/{id}/otp/reissue']}
  />
}
