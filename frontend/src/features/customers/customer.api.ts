import { apiRequest } from '@/shared/api/http-client'
import type { Customer, CustomerListItem, CustomerOrderHistory } from './customer.types'

export function searchCustomers(query: string) {
  const search = new URLSearchParams({ query, limit: '20' })
  return apiRequest<CustomerListItem[]>(`/api/v1/customers?${search}`)
}

export const getCustomer = (customerId: string) =>
  apiRequest<Customer>(`/api/v1/customers/${customerId}`)

export const getCustomerOrders = (customerId: string) =>
  apiRequest<CustomerOrderHistory[]>(`/api/v1/customers/${customerId}/orders?limit=50`)
