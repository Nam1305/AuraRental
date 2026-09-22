import { apiRequest, idempotencyHeaders } from '@/shared/api/http-client'
import type {
  CreateProductInput,
  InventoryItem,
  ProductDetail,
  ProductListItem,
  UpdateProductInput,
  VariantInput,
} from './catalog.types'

export function getProducts(branchId: string, query: string, active: string) {
  const search = new URLSearchParams({ query, limit: '50' })
  if (active) search.set('active', active)
  return apiRequest<ProductListItem[]>(`/api/v1/products?${search}`, { branchId })
}

export const getProduct = (branchId: string, productId: string) =>
  apiRequest<ProductDetail>(`/api/v1/products/${productId}`, { branchId })

export const createProduct = (branchId: string, input: CreateProductInput) =>
  apiRequest<ProductDetail>('/api/v1/products', {
    method: 'POST', branchId, headers: idempotencyHeaders(), body: JSON.stringify(input),
  })

export const updateProduct = (branchId: string, productId: string, input: UpdateProductInput) =>
  apiRequest<ProductDetail>(`/api/v1/products/${productId}`, {
    method: 'PATCH', branchId, headers: idempotencyHeaders(), body: JSON.stringify(input),
  })

export const addVariant = (branchId: string, productId: string, input: VariantInput) =>
  apiRequest(`/api/v1/products/${productId}/variants`, {
    method: 'POST', branchId, headers: idempotencyHeaders(), body: JSON.stringify(input),
  })

export const replacePrices = (
  branchId: string,
  variantId: string,
  prices: Array<{ packageCode: string; price: number }>,
) => apiRequest(`/api/v1/product-variants/${variantId}/rental-prices`, {
  method: 'PUT', branchId, headers: idempotencyHeaders(), body: JSON.stringify({ prices }),
})

export const addInventoryItems = (
  branchId: string,
  variantId: string,
  items: Array<{ assetCode: string }>,
) => apiRequest<InventoryItem[]>(`/api/v1/product-variants/${variantId}/inventory-items`, {
  method: 'POST', branchId, headers: idempotencyHeaders(), body: JSON.stringify({ items }),
})

export const updateInventoryItem = (
  branchId: string,
  inventoryItemId: string,
  status: InventoryItem['status'],
) => apiRequest<InventoryItem>(`/api/v1/inventory-items/${inventoryItemId}`, {
  method: 'PATCH', branchId, headers: idempotencyHeaders(),
  body: JSON.stringify({ status }),
})
