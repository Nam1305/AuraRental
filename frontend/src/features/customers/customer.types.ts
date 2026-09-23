export type CustomerListItem = {
  id: number
  name: string
  phone: string
  instagramHandle: string | null
  address: string | null
  completedOrderCount: number
  lastOrderAt: string | null
}

export type Customer = {
  id: number
  name: string
  phone: string
  instagramHandle: string | null
  address: string | null
}

export type CustomerOrderHistory = {
  orderId: number
  orderNo: string
  branchCode: string
  branchName: string
  status: string
  rentalStartAt: string
  rentalEndAt: string
  items: Array<{ productName: string; size: string; assetCode: string }>
  rentalFee: number
  processingFee: number
  createdAt: string
}
