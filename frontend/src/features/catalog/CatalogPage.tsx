import { useEffect, useState, type FormEvent } from 'react'
import { useSession } from '@/features/session/SessionProvider'
import { AsyncState } from '@/shared/components/AsyncState'
import { ImageUploadInput } from '@/shared/components/ImageUploadInput'
import { StoredImage } from '@/shared/components/StoredImage'
import { MoneyField } from '@/shared/components/MoneyInput'
import { formatMoney } from '@/shared/format/money'
import { useApiQuery } from '@/shared/hooks/use-api-query'
import {
  addInventoryItems,
  addVariant,
  createProduct,
  getProduct,
  getProducts,
  replacePrices,
  updateInventoryItem,
  updateProduct,
} from './catalog.api'
import type { InventoryItem, ProductDetail, ProductVariant, VariantInput } from './catalog.types'

const EMPTY_VARIANT = {
  size: '', measurements: '', replacementValue: '', price1d: '', price2d: '', price3d: '', assetCodes: '',
}

const paths = (value: string) => value.split('\n').map((item) => item.trim()).filter(Boolean)
const assetItems = (value: string) => paths(value).map((assetCode) => ({ assetCode }))
const priceInputs = (one: string, two: string, three: string) => [
  { packageCode: '1D', price: Number(one) },
  ...(Number(two) > 0 ? [{ packageCode: '2D', price: Number(two) }] : []),
  ...(Number(three) > 0 ? [{ packageCode: '3D', price: Number(three) }] : []),
]

export function CatalogPage() {
  const { activeBranchId, user } = useSession()
  const [input, setInput] = useState('')
  const [query, setQuery] = useState('')
  const [activeFilter, setActiveFilter] = useState('true')
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [creating, setCreating] = useState(false)
  const [version, setVersion] = useState(0)
  const isManager = user?.role === 'MANAGER'
  const canManageBranch = Boolean(user && activeBranchId)
  const activeBranchName = user?.branches.find((branch) => branch.id === activeBranchId)?.name
  const products = useApiQuery(
    () => activeBranchId ? getProducts(activeBranchId, query, activeFilter) : Promise.resolve([]),
    [activeBranchId, query, activeFilter, version],
  )

  useEffect(() => { setSelectedId(null); setCreating(false) }, [activeBranchId])
  useEffect(() => {
    const closeDetail = (event: KeyboardEvent) => { if (event.key === 'Escape') setSelectedId(null) }
    window.addEventListener('keydown', closeDetail)
    return () => window.removeEventListener('keydown', closeDetail)
  }, [])
  const refresh = () => setVersion((value) => value + 1)
  const submit = (event: FormEvent) => { event.preventDefault(); setQuery(input.trim()) }

  return (
    <section className="page-stack">
      <header className="page-heading page-heading--actions">
        <div><span className="eyebrow">Catalog riêng theo chi nhánh đang chọn</span><h1>Kho sản phẩm</h1><p>Quản lý mẫu, size, bảng giá 1D/2D/3D và từng mã đồ vật lý tại chi nhánh này.</p></div>
        <div className="heading-action">
          <button
            className="button button--primary"
            disabled={!canManageBranch}
            title="Tạo sản phẩm mới tại chi nhánh đang chọn"
            onClick={() => { setCreating(true); setSelectedId(null) }}
          >+ Thêm sản phẩm vào kho</button>
          <small>Giá và mã đồ sẽ được thêm vào {activeBranchName ?? 'chi nhánh đang chọn'}.</small>
        </div>
      </header>

      <form className="filter-bar" onSubmit={submit}>
        <label className="field"><span>Trạng thái</span><select value={activeFilter} onChange={(event) => setActiveFilter(event.target.value)}><option value="true">Đang kinh doanh</option><option value="false">Đã xóa/ngừng</option><option value="">Tất cả</option></select></label>
        <div className="inline-search"><input value={input} onChange={(event) => setInput(event.target.value)} placeholder="Tìm tên hoặc mã mẫu" /><button className="button" type="submit">Tìm</button></div>
      </form>

      {creating && activeBranchId ? <CreateProductPanel branchId={activeBranchId} onCancel={() => setCreating(false)} onCreated={(id) => { setCreating(false); setSelectedId(id); refresh() }} /> : (
        <div className="module-layout catalog-layout">
          <AsyncState loading={products.loading} error={products.error} empty={products.data?.length === 0}>
            <div className="result-list">
              {products.data?.map((product) => <button type="button" className={selectedId === product.id ? 'panel operation-card operation-card--selected' : 'panel operation-card'} key={product.id} onClick={() => setSelectedId(product.id)}>
                {product.coverImagePath && <StoredImage branchId={activeBranchId!} objectPath={product.coverImagePath} alt={product.name} className="catalog-product-image" />}
                <div className="operation-card__top"><span className="catalog-card__code">{product.code}</span><span className="status-pill">{product.isActive ? 'Đang bán' : 'Đã ngừng'}</span></div>
                <h2>{product.name}</h2><p>{product.category} · Size {product.sizes.join(', ') || '—'}</p>
                <div className="operation-card__meta"><span>{product.availableNowCount}/{product.activeInventoryCount} mã sẵn sàng</span><strong>{product.priceFrom === null ? 'Chưa có giá' : `Từ ${formatMoney(product.priceFrom)}`}</strong></div>
              </button>)}
            </div>
          </AsyncState>
          {activeBranchId && selectedId ? <div className="catalog-detail-layer">
            <button className="catalog-detail-backdrop" type="button" aria-label="Đóng chi tiết sản phẩm" onClick={() => setSelectedId(null)} />
            <div className="catalog-detail-surface" role="dialog" aria-modal={!window.matchMedia('(min-width: 1024px)').matches} aria-label="Chi tiết sản phẩm">
              <header className="catalog-detail-mobile-header"><button type="button" onClick={() => setSelectedId(null)}>←</button><div><small>Sản phẩm</small><strong>Chi tiết và quản lý kho</strong></div></header>
              <ProductDetailPanel branchId={activeBranchId} productId={selectedId} isManager={isManager} canManageBranch={canManageBranch} onChanged={refresh} />
            </div>
          </div> : <div className="state-card detail-empty">Chọn sản phẩm để xem ảnh, thông tin và quản lý kho.</div>}
        </div>
      )}
    </section>
  )
}

function CreateProductPanel({ branchId, onCancel, onCreated }: { branchId: number; onCancel: () => void; onCreated: (id: number) => void }) {
  const [product, setProduct] = useState({ code: '', name: '', category: 'DRESS', description: '', imagePaths: '' })
  const [variant, setVariant] = useState(EMPTY_VARIANT)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<Error | null>(null)
  const setProductField = (field: keyof typeof product, value: string) => setProduct((current) => ({ ...current, [field]: value }))
  const setVariantField = (field: keyof typeof variant, value: string) => setVariant((current) => ({ ...current, [field]: value }))

  const submit = async (event: FormEvent) => {
    event.preventDefault(); setSaving(true); setError(null)
    try {
      const created = await createProduct(branchId, {
        code: product.code, name: product.name, category: product.category, color: null,
        material: null, description: product.description || null, imagePaths: paths(product.imagePaths),
        variants: [{ size: variant.size, measurements: variant.measurements || null, replacementValue: Number(variant.replacementValue), prices: priceInputs(variant.price1d, variant.price2d, variant.price3d), inventoryItems: assetItems(variant.assetCodes) }],
      })
      onCreated(created.id)
    } catch (nextError) { setError(nextError as Error) } finally { setSaving(false) }
  }

  return <form className="panel create-flow" onSubmit={submit}>
    <div className="section-heading"><div><span className="eyebrow">Sản phẩm mới</span><h2>Thông tin mẫu</h2></div><button className="button" type="button" onClick={onCancel}>Đóng</button></div>
    <div className="form-grid">
      <label className="field"><span>Mã mẫu *</span><input required value={product.code} onChange={(event) => setProductField('code', event.target.value)} placeholder="VD: AUR-RED" /></label>
      <label className="field"><span>Tên sản phẩm *</span><input required value={product.name} onChange={(event) => setProductField('name', event.target.value)} /></label>
      <label className="field"><span>Loại *</span><input required value={product.category} onChange={(event) => setProductField('category', event.target.value)} placeholder="DRESS, AO_DAI…" /></label>
      <label className="field"><span>Ảnh sản phẩm</span><ImageUploadInput branchId={branchId} purpose="PRODUCT_IMAGE" value={product.imagePaths} onChange={(value) => setProductField('imagePaths', value)} /></label>
      <label className="field field--wide"><span>Mô tả</span><textarea rows={3} value={product.description} onChange={(event) => setProductField('description', event.target.value)} /></label>
    </div>
    <div className="catalog-subsection"><h3>Size đầu tiên và giá tại chi nhánh này</h3><VariantFields value={variant} onChange={setVariantField} /></div>
    {error && <div className="inline-error">{error.message}</div>}
    <div className="action-buttons"><button className="button button--primary" disabled={saving}>{saving ? 'Đang lưu…' : 'Tạo sản phẩm'}</button><button className="button" type="button" onClick={onCancel}>Hủy</button></div>
  </form>
}

function VariantFields({ value, onChange }: { value: typeof EMPTY_VARIANT; onChange: (field: keyof typeof EMPTY_VARIANT, value: string) => void }) {
  return <div className="form-grid">
    <label className="field"><span>Size *</span><input required value={value.size} onChange={(event) => onChange('size', event.target.value)} placeholder="S, M, L hoặc ONE_SIZE" /></label>
    <MoneyField label="Giá trị thay thế" required value={value.replacementValue} onChange={(next) => onChange('replacementValue', next)} />
    <label className="field"><span>Số đo</span><input value={value.measurements} onChange={(event) => onChange('measurements', event.target.value)} placeholder="Ngực 84 · Eo 66" /></label>
    <MoneyField label="Giá 1D" required value={value.price1d} onChange={(next) => onChange('price1d', next)} />
    <MoneyField label="Giá 2D" value={value.price2d} onChange={(next) => onChange('price2d', next)} />
    <MoneyField label="Giá 3D" value={value.price3d} onChange={(next) => onChange('price3d', next)} />
    <label className="field"><span>Mã vật lý *</span><textarea required rows={3} value={value.assetCodes} onChange={(event) => onChange('assetCodes', event.target.value)} placeholder="Mỗi dòng một mã, VD: HN-AUR-S-01" /></label>
  </div>
}

function ProductDetailPanel({ branchId, productId, isManager, canManageBranch, onChanged }: { branchId: number; productId: number; isManager: boolean; canManageBranch: boolean; onChanged: () => void }) {
  const [version, setVersion] = useState(0)
  const [editing, setEditing] = useState(false)
  const [addingVariant, setAddingVariant] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<Error | null>(null)
  const detail = useApiQuery(() => getProduct(branchId, productId), [branchId, productId, version])
  const refresh = () => { setVersion((value) => value + 1); onChanged() }
  const product = detail.data

  const toggleActive = async () => {
    if (!product || (product.isActive && !window.confirm('Ngừng kinh doanh sản phẩm này? Dữ liệu đơn cũ vẫn được giữ.'))) return
    setError(null)
    try {
      await updateProduct(branchId, product.id, { name: product.name, category: product.category, color: product.color, material: product.material, description: product.description, imagePaths: product.imagePaths, isActive: !product.isActive })
      setMessage(product.isActive ? 'Đã xóa sản phẩm khỏi danh sách kinh doanh.' : 'Đã khôi phục sản phẩm.')
      refresh()
    } catch (nextError) { setError(nextError as Error) }
  }

  return <AsyncState loading={detail.loading} error={detail.error}>
    {product && <aside className="panel detail-panel catalog-detail catalog-detail-panel">
      <div className="operation-card__top"><span className="catalog-card__code">{product.code}</span><span className="status-pill">{product.isActive ? 'Đang kinh doanh' : 'Đã ngừng'}</span></div>
      {product.imagePaths.length > 0 && <div className="product-image-gallery">{product.imagePaths.map((path) => <StoredImage key={path} branchId={branchId} objectPath={path} alt={`${product.name} ${product.code}`} className="product-image-gallery__image" />)}</div>}
      {editing ? <EditProductForm branchId={branchId} product={product} onCancel={() => setEditing(false)} onSaved={() => { setEditing(false); setMessage('Đã cập nhật sản phẩm.'); refresh() }} /> : <>
        <h2>{product.name}</h2><p>{product.category} · {product.color || 'Chưa có màu'} · {product.material || 'Chưa có chất liệu'}</p>
        {product.description && <p>{product.description}</p>}
        {canManageBranch && <div className="action-buttons"><button className="button" onClick={() => setAddingVariant((value) => !value)}>+ Thêm size/kho chi nhánh</button>{isManager && <><button className="button button--primary" onClick={() => setEditing(true)}>Sửa thông tin chung</button><button className={product.isActive ? 'button button--danger' : 'button'} onClick={() => void toggleActive()}>{product.isActive ? 'Xóa / ngừng kinh doanh' : 'Khôi phục'}</button></>}</div>}
      </>}
      {message && <div className="success-note">{message}</div>}{error && <div className="inline-error">{error.message}</div>}
      {addingVariant && <AddVariantForm branchId={branchId} productId={product.id} onCancel={() => setAddingVariant(false)} onSaved={() => { setAddingVariant(false); setMessage('Đã thêm size.'); refresh() }} />}
      <div className="variant-list">{product.variants.map((variant) => <VariantCard key={variant.id} branchId={branchId} variant={variant} canManage={canManageBranch} onChanged={refresh} />)}</div>
    </aside>}
  </AsyncState>
}

function EditProductForm({ branchId, product, onCancel, onSaved }: { branchId: number; product: ProductDetail; onCancel: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({ name: product.name, category: product.category, color: product.color ?? '', material: product.material ?? '', description: product.description ?? '', imagePaths: product.imagePaths.join('\n') })
  const [error, setError] = useState<Error | null>(null)
  const set = (field: keyof typeof form, value: string) => setForm((current) => ({ ...current, [field]: value }))
  const submit = async (event: FormEvent) => { event.preventDefault(); setError(null); try { await updateProduct(branchId, product.id, { name: form.name, category: form.category, color: form.color || null, material: form.material || null, description: form.description || null, imagePaths: paths(form.imagePaths), isActive: product.isActive }); onSaved() } catch (nextError) { setError(nextError as Error) } }
  return <form className="nested-form" onSubmit={submit}><h2>Sửa {product.code}</h2><div className="form-grid"><label className="field"><span>Tên *</span><input required value={form.name} onChange={(event) => set('name', event.target.value)} /></label><label className="field"><span>Loại *</span><input required value={form.category} onChange={(event) => set('category', event.target.value)} /></label><label className="field"><span>Màu</span><input value={form.color} onChange={(event) => set('color', event.target.value)} /></label><label className="field"><span>Chất liệu</span><input value={form.material} onChange={(event) => set('material', event.target.value)} /></label><label className="field"><span>Ảnh sản phẩm</span><ImageUploadInput branchId={branchId} purpose="PRODUCT_IMAGE" value={form.imagePaths} onChange={(value) => set('imagePaths', value)} /></label><label className="field"><span>Mô tả</span><textarea rows={2} value={form.description} onChange={(event) => set('description', event.target.value)} /></label></div>{error && <div className="inline-error">{error.message}</div>}<div className="action-buttons"><button className="button button--primary">Lưu</button><button className="button" type="button" onClick={onCancel}>Hủy</button></div></form>
}

function AddVariantForm({ branchId, productId, onCancel, onSaved }: { branchId: number; productId: number; onCancel: () => void; onSaved: () => void }) {
  const [value, setValue] = useState(EMPTY_VARIANT)
  const [error, setError] = useState<Error | null>(null)
  const set = (field: keyof typeof value, next: string) => setValue((current) => ({ ...current, [field]: next }))
  const submit = async (event: FormEvent) => { event.preventDefault(); setError(null); const input: VariantInput = { size: value.size, measurements: value.measurements || null, replacementValue: Number(value.replacementValue), prices: priceInputs(value.price1d, value.price2d, value.price3d), inventoryItems: assetItems(value.assetCodes) }; try { await addVariant(branchId, productId, input); onSaved() } catch (nextError) { setError(nextError as Error) } }
  return <form className="nested-form" onSubmit={submit}><h3>Thêm size</h3><VariantFields value={value} onChange={set} />{error && <div className="inline-error">{error.message}</div>}<div className="action-buttons"><button className="button button--primary">Thêm size</button><button type="button" className="button" onClick={onCancel}>Hủy</button></div></form>
}

function VariantCard({ branchId, variant, canManage, onChanged }: { branchId: number; variant: ProductVariant; canManage: boolean; onChanged: () => void }) {
  const findPrice = (code: string) => String(variant.prices.find((item) => item.packageCode === code)?.price ?? '')
  const [prices, setPrices] = useState({ one: findPrice('1D'), two: findPrice('2D'), three: findPrice('3D') })
  const [assetCodes, setAssetCodes] = useState('')
  const [error, setError] = useState<Error | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  useEffect(() => setPrices({ one: findPrice('1D'), two: findPrice('2D'), three: findPrice('3D') }), [variant])
  const savePrices = async () => { setError(null); try { await replacePrices(branchId, variant.id, priceInputs(prices.one, prices.two, prices.three)); setMessage('Đã cập nhật giá cho chi nhánh.'); onChanged() } catch (nextError) { setError(nextError as Error) } }
  const addAssets = async () => { setError(null); try { await addInventoryItems(branchId, variant.id, assetItems(assetCodes)); setAssetCodes(''); setMessage('Đã thêm mã vật lý.'); onChanged() } catch (nextError) { setError(nextError as Error) } }
  const changeStatus = async (item: InventoryItem, status: InventoryItem['status']) => { setError(null); try { await updateInventoryItem(branchId, item.id, status); setMessage(`Đã cập nhật ${item.assetCode}.`); onChanged() } catch (nextError) { setError(nextError as Error) } }

  return <section className="variant-card"><div className="section-heading"><div><span className="eyebrow">Size</span><h3>{variant.size}</h3><small>{variant.measurements || 'Chưa có số đo'} · Giá trị {formatMoney(variant.replacementValue)}</small></div><strong>{variant.inventorySummary.usable}/{variant.inventorySummary.total} usable</strong></div>
    <div className="price-editor">{(['one', 'two', 'three'] as const).map((key, index) => <MoneyField key={key} label={`Giá ${index + 1}D`} required={key === 'one'} disabled={!canManage} value={prices[key]} onChange={(next) => setPrices((current) => ({ ...current, [key]: next }))} />)}{canManage && <button className="button" type="button" onClick={() => void savePrices()}>Lưu giá</button>}</div>
    <div className="inventory-list">{variant.inventoryItems.map((item) => <div className="inventory-row" key={item.id}><div><strong>{item.assetCode}</strong></div>{canManage ? <select value={item.status} onChange={(event) => void changeStatus(item, event.target.value as InventoryItem['status'])}><option value="USABLE">Sẵn sàng</option><option value="MAINTENANCE">Bảo trì</option><option value="LOST">Thất lạc</option><option value="RETIRED">Ngừng dùng</option></select> : <span className="status-pill">{item.status}</span>}</div>)}</div>
    {canManage && <div className="add-assets"><label className="field"><span>Thêm mã vật lý</span><textarea rows={2} value={assetCodes} onChange={(event) => setAssetCodes(event.target.value)} placeholder="Mỗi dòng một mã" /></label><button className="button" type="button" disabled={!assetCodes.trim()} onClick={() => void addAssets()}>Thêm vào kho</button></div>}
    {message && <div className="success-note">{message}</div>}{error && <div className="inline-error">{error.message}</div>}
  </section>
}
