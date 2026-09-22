export type OrderListItem = {
  id: string
  orderNo: string
  status: string
  customerId: string
  customerName: string
  phoneMasked: string
  itemSummary: string
  rentalStartAt: string
  rentalEndAt: string
  depositRequired: number
  depositConfirmed: number
  depositRemaining: number
  createdAt: string
}

export type OrderItem = {
  orderItemId: string
  inventoryItemId: string
  productName: string
  size: string
  assetCode: string
  packageCode: string
  rentalPrice: number
  condition: string | null
  processingFee: number
  damageNote: string | null
  damagePhotoPaths: string[]
}

export type OrderDetail = {
  id: string
  reservationId: string
  orderNo: string
  status: string
  branchId: string
  branchCode: string
  branchName: string
  customer: { id: string; nameSnapshot: string; phoneSnapshot: string; deliveryAddressSnapshot: string }
  rentalStartAt: string
  rentalEndAt: string
  items: OrderItem[]
  depositRequired: number
  depositConfirmed: number
  depositRemaining: number
  identityVerification: { required: boolean; verified: boolean; verifiedBy: string | null; verifiedAt: string | null }
  delivery: { status: string; trackingCode: string | null; completedAt: string | null }
  returnDelivery: { status: string; trackingCode: string | null; completedAt: string | null }
  payments: Array<{ id: string; type: string; amount: number; method: string; status: string; transactionRef: string | null; paidAt: string | null }>
  allowedActions: string[]
}
