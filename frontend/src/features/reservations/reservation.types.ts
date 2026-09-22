export type ReservationListItem = {
  id: string
  reservationNo: string
  status: string
  customerId: string
  customerName: string
  phoneMasked: string
  itemSummary: string
  rentalStartAt: string
  rentalEndAt: string
  depositConfirmed: number
  depositRemaining: number
  depositDeadlineAt: string | null
  formStatus: string
}

export type ReservationDetail = {
  id: string
  reservationNo: string
  status: string
  branch: { id: string; code: string; name: string }
  customer: { id: string; name: string; phone: string }
  rentalStartAt: string
  rentalEndAt: string
  deposit: {
    plan: string
    required: number
    confirmedReceived: number
    remaining: number
    deadlineAt: string | null
  }
  items: Array<{
    inventoryItemId: string
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
  branchId: string
  currency: string
  items: Array<{
    inventoryItemId: string
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

export type RentalSelection = { inventoryItemId: string; packageCode: string }

export type CreateReservationInput = {
  customerId: string
  rentalStartAt: string
  rentalEndAt: string
  depositPlan: string
  depositDeadlineAt: string | null
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
