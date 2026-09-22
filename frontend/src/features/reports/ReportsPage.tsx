import { ModulePlaceholder } from '@/shared/components/ModulePlaceholder'

export function ReportsPage() {
  return <ModulePlaceholder
    eyebrow="Theo chi nhánh"
    title="Báo cáo"
    description="Doanh thu thuê, tiền cọc, hoàn tiền và hiệu suất kho theo kỳ."
    endpoints={['GET /api/v1/reports/summary', 'GET /api/v1/reports/branches']}
  />
}
