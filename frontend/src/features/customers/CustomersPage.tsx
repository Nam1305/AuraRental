import { useState, type FormEvent } from 'react'
import { AsyncState } from '@/shared/components/AsyncState'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import { searchCustomers } from './customer.api'

export function CustomersPage() {
  const [input, setInput] = useState('')
  const [query, setQuery] = useState('')
  const customers = useApiQuery(() => searchCustomers(query), [query])

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setQuery(input.trim())
  }

  return (
    <section className="page-stack">
      <header className="page-heading">
        <span className="eyebrow">CRM tối giản</span>
        <h1>Khách hàng</h1>
        <p>Tìm theo tên, số điện thoại hoặc Instagram; mở hồ sơ để xem lịch sử thuê.</p>
      </header>
      <form className="inline-search" onSubmit={submit}>
        <input value={input} onChange={(event) => setInput(event.target.value)} placeholder="0908…, @ngocanh, tên khách" />
        <button className="button" type="submit">Tìm</button>
      </form>
      <AsyncState loading={customers.loading} error={customers.error} empty={customers.data?.length === 0}>
        <div className="result-list">
          {customers.data?.map((customer) => (
            <article className="panel customer-card" key={customer.id}>
              <div className="avatar">{customer.name.slice(0, 1).toUpperCase()}</div>
              <div>
                <h2>{customer.name}</h2>
                <p>{customer.phone}{customer.instagramHandle ? ` · @${customer.instagramHandle}` : ''}</p>
                <small>{customer.completedOrderCount} đơn hoàn tất</small>
              </div>
              <a href={`/customers/${customer.id}`} aria-label={`Mở hồ sơ ${customer.name}`}>›</a>
            </article>
          ))}
        </div>
      </AsyncState>
    </section>
  )
}
