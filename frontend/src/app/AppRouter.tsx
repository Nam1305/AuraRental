import { AppShell } from './AppShell'
import { AvailabilityPage } from '@/features/availability/AvailabilityPage'
import { CatalogPage } from '@/features/catalog/CatalogPage'
import { CustomersPage } from '@/features/customers/CustomersPage'
import { CustomerDetailPage } from '@/features/customers/CustomerDetailPage'
import { DashboardPage } from '@/features/dashboard/DashboardPage'
import { OrdersPage } from '@/features/orders/OrdersPage'
import { PublicRentalFormPage } from '@/features/public-form/PublicRentalFormPage'
import { ReportsPage } from '@/features/reports/ReportsPage'
import { ReservationsPage } from '@/features/reservations/ReservationsPage'
import { ReturnsPage } from '@/features/returns/ReturnsPage'
import { SettingsPage } from '@/features/settings/SettingsPage'
import { ModulePlaceholder } from '@/shared/components/ModulePlaceholder'

export function AppRouter() {
  const pathname = window.location.pathname.replace(/\/$/, '') || '/'
  if (pathname.startsWith('/r/')) return <PublicRentalFormPage />

  let page
  if (pathname === '/') page = <DashboardPage />
  else if (pathname === '/availability') page = <AvailabilityPage />
  else if (pathname === '/catalog') page = <CatalogPage />
  else if (pathname === '/customers') page = <CustomersPage />
  else if (pathname.startsWith('/customers/')) page = <CustomerDetailPage customerId={Number(pathname.split('/')[2])} />
  else if (pathname === '/reservations') page = <ReservationsPage />
  else if (pathname === '/orders') page = <OrdersPage />
  else if (pathname === '/returns') page = <ReturnsPage />
  else if (pathname === '/reports') page = <ReportsPage />
  else if (pathname === '/settings') page = <SettingsPage />
  else page = <ModulePlaceholder eyebrow="404" title="Không tìm thấy màn hình" description="Đường dẫn không tồn tại." endpoints={[]} />

  return <AppShell>{page}</AppShell>
}
