import { apiRequest, idempotencyHeaders } from '@/shared/api/http-client'
import type { RefundReceipt, RefundResult, ReturnQueueItem } from './return.types'

export const getReturnQueue = (branchId: number) =>
  apiRequest<ReturnQueueItem[]>('/api/v1/returns?limit=50', { branchId })

export const inspectOrderItem = (
  branchId: number,
  orderId: number,
  orderItemId: number,
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

export const createRefund = (branchId: number, orderId: number, adjustmentReason: string | null) =>
  apiRequest<RefundResult>(`/api/v1/orders/${orderId}/refunds`, {
    method: 'POST', branchId, headers: idempotencyHeaders(), body: JSON.stringify({ adjustmentReason }),
  })

const refundAction = <T>(branchId: number, refundId: number, action: string, body?: unknown) =>
  apiRequest<T>(`/api/v1/refunds/${refundId}/${action}`, {
    method: 'POST', branchId, headers: idempotencyHeaders(),
    body: body === undefined ? undefined : JSON.stringify(body),
  })

export const submitRefund = (branchId: number, refundId: number) =>
  refundAction<RefundResult>(branchId, refundId, 'submit')

export const approveRefund = (branchId: number, refundId: number, expectedVersion: number) =>
  refundAction(branchId, refundId, 'approve', { expectedVersion })

export const returnRefundForReview = (branchId: number, refundId: number, reason: string) =>
  refundAction<RefundResult>(branchId, refundId, 'return-for-review', { reason })

export const settleRefund = (branchId: number, refundId: number, method: string, transactionRef: string | null) =>
  refundAction(branchId, refundId, 'settle', {
    method, transactionRef, proofPath: null, paidAt: new Date().toISOString(),
  })

export const getRefundReceipt = (branchId: number, refundId: number) =>
  apiRequest<RefundReceipt>(`/api/v1/refunds/${refundId}/receipt`, { branchId })
