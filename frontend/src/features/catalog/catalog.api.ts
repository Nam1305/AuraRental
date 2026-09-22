import { apiRequest } from '@/shared/api/http-client'
import type { ProductListItem } from './catalog.types'

export function getProducts(branchId: string, query: string) {
  const search = new URLSearchParams({ query, active: 'true', limit: '50' })
  return apiRequest<ProductListItem[]>(`/api/v1/products?${search}`, { branchId })
}
