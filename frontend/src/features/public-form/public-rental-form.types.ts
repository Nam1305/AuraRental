export type PublicRentalForm = {
  reservationNo: string
  branch: { name: string; address: string }
  items: Array<{
    productName: string
    size: string
    assetCode: string
    packageCode: string
    packageLabel: string
    rentalPrice: number
  }>
  rentalStartAt: string
  rentalEndAt: string
  depositPlan: string
  depositConfirmed: number
  depositRemaining: number
  otpRequired: boolean
}

export type SubmitPublicRentalFormRequest = {
  otp: string
  customerName: string
  customerPhone: string
  deliveryAddress: string
}

export type SubmittedPublicRentalForm = {
  orderId: number
  orderNo: string
  status: string
  branchName: string
  depositRemaining: number
  message: string
}
