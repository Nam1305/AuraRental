export type OrderListItem = {
  id: number
  orderNo: string
  status: string
  customerId: number
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
  orderItemId: number
  inventoryItemId: number
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
  id: number
  reservationId: number
  orderNo: string
  status: string
  branchId: number
  branchCode: string
  branchName: string
  customer: { id: number; nameSnapshot: string; phoneSnapshot: string; deliveryAddressSnapshot: string }
  rentalStartAt: string
  rentalEndAt: string
  items: OrderItem[]
  depositRequired: number
  depositConfirmed: number
  depositRemaining: number
  identityVerification: { required: boolean; verified: boolean; verifiedBy: string | null; verifiedAt: string | null }
  delivery: { status: string; trackingCode: string | null; completedAt: string | null }
  returnDelivery: { status: string; trackingCode: string | null; completedAt: string | null }
  payments: Array<{ id: number; type: string; amount: number; method: string; status: string; transactionRef: string | null; paidAt: string | null }>
  allowedActions: string[]
}
