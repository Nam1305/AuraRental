export type ReturnQueueItem = {
  orderId: number
  orderNo: string
  customerName: string
  returnedAt: string | null
  itemCount: number
  inspectionCompleted: number
  inspectionTotal: number
  refundId: number | null
  refundVersion: number | null
  refundStatus: string | null
  refundAmount: number | null
  additionalCollection: number | null
}

export type RefundResult = {
  refundId: number
  orderId: number
  version: number
  status: string
  depositAmount: number
  rentalFee: number
  processingFee: number
  refundAmount: number
  additionalCollection: number
  adjustmentReason: string | null
}

export type RefundReceipt = {
  refundId: number
  orderId: number
  orderNo: string
  branchName: string
  customerName: string
  status: 'APPROVED' | 'SETTLED'
  finalizedAt: string
  depositAmount: number
  rentalFee: number
  processingFee: number
  refundAmount: number
  additionalCollection: number
  items: Array<{ productName: string; size: string; assetCode: string; packageCode: string | null; rentalFee: number }>
}
