import { apiRequest, idempotencyHeaders } from '@/shared/api/http-client'
import type { OrderDetail, OrderListItem } from './order.types'

export function searchOrders(branchId: string, status: string, query: string) {
  const search = new URLSearchParams({ status, query, limit: '50' })
  return apiRequest<OrderListItem[]>(`/api/v1/orders?${search}`, { branchId })
}

export const getOrder = (branchId: string, orderId: string) =>
  apiRequest<OrderDetail>(`/api/v1/orders/${orderId}`, { branchId })

const postAction = <T>(branchId: string, path: string, body?: unknown) =>
  apiRequest<T>(path, {
    method: 'POST',
    branchId,
    headers: idempotencyHeaders(),
    body: body === undefined ? undefined : JSON.stringify(body),
  })

export const verifyIdentity = (branchId: string, orderId: string) =>
  postAction(branchId, `/api/v1/orders/${orderId}/identity-verification`, { verified: true })

export const prepareOrder = (branchId: string, orderId: string) =>
  postAction(branchId, `/api/v1/orders/${orderId}/prepare`)

export const startDelivery = (branchId: string, orderId: string, trackingCode: string | null) =>
  postAction(branchId, `/api/v1/orders/${orderId}/delivery/start`, { trackingCode, startedAt: new Date().toISOString() })

export const completeDelivery = (branchId: string, orderId: string) =>
  postAction(branchId, `/api/v1/orders/${orderId}/delivery/complete`, { deliveredAt: new Date().toISOString() })

export const startReturn = (branchId: string, orderId: string, trackingCode: string | null) =>
  postAction(branchId, `/api/v1/orders/${orderId}/return-delivery/start`, { trackingCode, startedAt: new Date().toISOString() })

export const completeReturn = (branchId: string, orderId: string) =>
  postAction(branchId, `/api/v1/orders/${orderId}/return-delivery/complete`, { returnedAt: new Date().toISOString() })

export const cancelOrder = (branchId: string, orderId: string, reason: string) =>
  postAction(branchId, `/api/v1/orders/${orderId}/cancel`, { reason, paymentDecision: 'REVIEW_SEPARATELY' })
