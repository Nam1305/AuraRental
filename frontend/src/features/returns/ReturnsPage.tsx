import { ModulePlaceholder } from '@/shared/components/ModulePlaceholder'

export function ReturnsPage() {
  return <ModulePlaceholder
    eyebrow="Inspection → refund"
    title="Trả đồ"
    description="Ghi nhận GOOD, DAMAGED hoặc MISSING theo từng mã; backend tự tính tiền đối soát."
    endpoints={['GET /api/v1/returns', 'PUT /api/v1/order-items/{id}/inspection', 'POST /api/v1/refunds/{id}/approve']}
  />
}
