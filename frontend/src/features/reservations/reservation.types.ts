export type ReservationListItem = {
  id: number
  reservationNo: string
  status: string
  customerId: number | null
  customerName: string
  phoneMasked: string
  itemSummary: string
  rentalStartAt: string
  rentalEndAt: string
  depositConfirmed: number
  depositRemaining: number
  formStatus: string
}

export type ReservationDetail = {
  id: number
  reservationNo: string
  status: string
  branch: { id: number; code: string; name: string }
  customer: { id: number; name: string; phone: string } | null
  rentalStartAt: string
  rentalEndAt: string
  deposit: {
    plan: string
    required: number
    confirmedReceived: number
    remaining: number
  }
  items: Array<{
    inventoryItemId: number
    assetCode: string
    productName: string
    size: string
    packageCode: string
    rentalPrice: number
  }>
  customerForm: null | { url: string; otp: string; expiresAt: string }
  formStatus: string
  createdAt: string
}

export type Quote = {
  branchId: number
  currency: string
  items: Array<{
    inventoryItemId: number
    assetCode: string
    productName: string
    size: string
    packageCode: string
    rentalPrice: number
    replacementValue: number
  }>
  rentalFee: number
  depositRequired: number
  slotDepositAmount: number
  expiresAt: string
}

export type RentalSelection = { inventoryItemId: number; packageCode: string }

export type CreateReservationInput = {
  rentalStartAt: string
  rentalEndAt: string
  depositPlan: string
  items: RentalSelection[]
  receivedPayment: {
    type: string
    amount: number
    method: string
    transactionRef: string | null
    proofPath: string | null
    paidAt: string
  }
}
