export type RentalPrice = {
  packageCode: string
  label: string
  price: number
}

export type AvailableItem = {
  inventoryItemId: number
  assetCode: string
  status: string
  availableForWholePeriod: boolean
  availabilityStatus: string
  availabilityNote: string | null
  busyUntil: string | null
  referenceNo: string | null
}

export type AvailabilityGroup = {
  productId: number
  variantId: number
  productName: string
  imagePaths: string[]
  size: string
  measurements: string | null
  availableCount: number
  prices: RentalPrice[]
  items: AvailableItem[]
}

export type AvailabilityResult = {
  criteria: { startAt: string; endAt: string }
  groups: AvailabilityGroup[]
}

export type AvailabilityCriteria = {
  query: string
  size: string
  startAt: string
  endAt: string
}
