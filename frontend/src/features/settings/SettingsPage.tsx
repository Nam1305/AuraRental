import { ModulePlaceholder } from '@/shared/components/ModulePlaceholder'

export function SettingsPage() {
  return <ModulePlaceholder
    eyebrow="Manager only"
    title="Cấu hình"
    description="Quản lý chi nhánh, nhân viên và chính sách cọc."
    endpoints={['GET /api/v1/settings', 'PUT /api/v1/settings', 'PUT /api/v1/users/{id}/branches']}
  />
}
