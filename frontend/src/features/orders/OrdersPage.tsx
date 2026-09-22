import { ModulePlaceholder } from '@/shared/components/ModulePlaceholder'

export function OrdersPage() {
  return <ModulePlaceholder
    eyebrow="Đơn do khách submit form"
    title="Đơn hàng"
    description="Theo dõi cọc còn lại, checklist CCCD Instagram và tiến độ giao nhận."
    endpoints={['GET /api/v1/orders', 'GET /api/v1/orders/{id}', 'POST /api/v1/orders/{id}/identity-verification']}
  />
}
