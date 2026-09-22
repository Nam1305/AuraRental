export type ReturnQueueItem = {
  orderId: string
  orderNo: string
  customerName: string
  returnedAt: string | null
  itemCount: number
  inspectionCompleted: number
  inspectionTotal: number
  refundId: string | null
  refundVersion: number | null
  refundStatus: string | null
  refundAmount: number | null
  additionalCollection: number | null
}

export type RefundResult = {
  refundId: string
  orderId: string
  version: number
  status: string
  depositAmount: number
  rentalFee: number
  processingFee: number
  refundAmount: number
  additionalCollection: number
  adjustmentReason: string | null
}
