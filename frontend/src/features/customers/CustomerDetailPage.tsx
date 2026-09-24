import { AsyncState } from '@/shared/components/AsyncState'
import { formatMoney } from '@/shared/format/money'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import { getCustomer, getCustomerOrders } from './customer.api'

export function CustomerDetailPage({ customerId }: { customerId: number }) {
  const customer = useApiQuery(() => getCustomer(customerId), [customerId])
  const orders = useApiQuery(() => getCustomerOrders(customerId), [customerId])

  return (
    <section className="page-stack">
      <a className="back-link" href="/customers">← Danh sách khách</a>
      <AsyncState loading={customer.loading} error={customer.error} empty={!customer.data}>
        <header className="page-heading">
          <span className="eyebrow">Hồ sơ và lịch sử thuê</span>
          <h1>{customer.data?.name}</h1>
          <p>{customer.data?.phone}{customer.data?.instagramHandle ? ` · IG @${customer.data.instagramHandle}` : ''}{customer.data?.tiktokHandle ? ` · TikTok @${customer.data.tiktokHandle}` : ''}</p>
        </header>
      </AsyncState>

      <section>
        <h2 className="section-title">Lịch sử đơn</h2>
        <AsyncState loading={orders.loading} error={orders.error} empty={orders.data?.length === 0}>
          <div className="result-list">
            {orders.data?.map((order) => (
              <article className="panel history-card" key={order.orderId}>
                <div><strong>{order.orderNo}</strong><span>{order.branchName}</span></div>
                <span className="status-pill">{order.status}</span>
                <p>{order.items.map((item) => `${item.productName} ${item.size} · ${item.assetCode}`).join(', ')}</p>
                <footer><strong>{formatMoney(order.rentalFee)}</strong><span>Phí xử lý {formatMoney(order.processingFee)}</span></footer>
              </article>
            ))}
          </div>
        </AsyncState>
      </section>
    </section>
  )
}
