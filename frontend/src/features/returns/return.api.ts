import { apiRequest, idempotencyHeaders } from '@/shared/api/http-client'
import type { RefundResult, ReturnQueueItem } from './return.types'

export const getReturnQueue = (branchId: string) =>
  apiRequest<ReturnQueueItem[]>('/api/v1/returns?limit=50', { branchId })

export const inspectOrderItem = (
  branchId: string,
  orderId: string,
  orderItemId: string,
  body: {
    condition: string
    actualRentalFee: number
    processingFee: number
    damageNote: string | null
    damagePhotoPaths: string[]
    inventoryOutcome: string
  },
) => apiRequest(`/api/v1/orders/${orderId}/items/${orderItemId}/inspection`, {
  method: 'PUT', branchId, headers: idempotencyHeaders(), body: JSON.stringify(body),
})

export const createRefund = (branchId: string, orderId: string, adjustmentReason: string | null) =>
  apiRequest<RefundResult>(`/api/v1/orders/${orderId}/refunds`, {
    method: 'POST', branchId, headers: idempotencyHeaders(), body: JSON.stringify({ adjustmentReason }),
  })

const refundAction = <T>(branchId: string, refundId: string, action: string, body?: unknown) =>
  apiRequest<T>(`/api/v1/refunds/${refundId}/${action}`, {
    method: 'POST', branchId, headers: idempotencyHeaders(),
    body: body === undefined ? undefined : JSON.stringify(body),
  })

export const submitRefund = (branchId: string, refundId: string) =>
  refundAction<RefundResult>(branchId, refundId, 'submit')

export const approveRefund = (branchId: string, refundId: string, expectedVersion: number) =>
  refundAction(branchId, refundId, 'approve', { expectedVersion })

export const returnRefundForReview = (branchId: string, refundId: string, reason: string) =>
  refundAction<RefundResult>(branchId, refundId, 'return-for-review', { reason })

export const settleRefund = (branchId: string, refundId: string, method: string, transactionRef: string | null) =>
  refundAction(branchId, refundId, 'settle', {
    method, transactionRef, proofPath: null, paidAt: new Date().toISOString(),
  })
