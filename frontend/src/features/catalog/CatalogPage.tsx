import { useState, type FormEvent } from 'react'
import { useSession } from '@/features/session/SessionProvider'
import { AsyncState } from '@/shared/components/AsyncState'
import { formatMoney } from '@/shared/format/money'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import { getProducts } from './catalog.api'

export function CatalogPage() {
  const { activeBranchId } = useSession()
  const [input, setInput] = useState('')
  const [query, setQuery] = useState('')
  const products = useApiQuery(
    () => activeBranchId ? getProducts(activeBranchId, query) : Promise.resolve([]),
    [activeBranchId, query],
  )

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setQuery(input.trim())
  }

  return (
    <section className="page-stack">
      <header className="page-heading">
        <span className="eyebrow">Catalog dùng chung · giá theo chi nhánh</span>
        <h1>Kho sản phẩm</h1>
        <p>Xem mẫu, size, số mã vật lý và giá thấp nhất ở chi nhánh đang chọn.</p>
      </header>
      <form className="inline-search" onSubmit={submit}>
        <input value={input} onChange={(event) => setInput(event.target.value)} placeholder="Tìm tên hoặc mã mẫu" />
        <button className="button" type="submit">Tìm</button>
      </form>
      <AsyncState loading={products.loading} error={products.error} empty={products.data?.length === 0}>
        <div className="card-grid">
          {products.data?.map((product) => (
            <article className="panel catalog-card" key={product.id}>
              <div className="catalog-card__code">{product.code}</div>
              <h2>{product.name}</h2>
              <p>Size {product.sizes.join(', ') || '—'} · {product.activeInventoryCount} mã đang quản lý</p>
              <div className="catalog-card__footer">
                <strong>{product.priceFrom === null ? 'Chưa có giá' : `Từ ${formatMoney(product.priceFrom)}`}</strong>
                <span>{product.packageCodes.join(' · ')}</span>
              </div>
            </article>
          ))}
        </div>
      </AsyncState>
    </section>
  )
}
