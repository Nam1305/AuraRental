export type ProductListItem = {
  id: string
  code: string
  name: string
  category: string
  isActive: boolean
  coverImagePath: string | null
  sizes: string[]
  activeInventoryCount: number
  availableNowCount: number
  priceFrom: number | null
  packageCodes: string[]
}

export type RentalPrice = { packageCode: string; label: string; price: number }
export type InventoryItem = {
  id: string
  assetCode: string
  status: 'USABLE' | 'MAINTENANCE' | 'LOST' | 'RETIRED'
}

export type ProductVariant = {
  id: string
  size: string
  measurements: string | null
  replacementValue: number
  prices: RentalPrice[]
  inventorySummary: { total: number; usable: number; maintenance: number; lost: number; retired: number }
  inventoryItems: InventoryItem[]
}

export type ProductDetail = {
  id: string
  code: string
  name: string
  category: string
  color: string | null
  material: string | null
  description: string | null
  imagePaths: string[]
  isActive: boolean
  variants: ProductVariant[]
}

export type VariantInput = {
  size: string
  measurements: string | null
  replacementValue: number
  prices: Array<{ packageCode: string; price: number }>
  inventoryItems: Array<{ assetCode: string }>
}

export type CreateProductInput = {
  code: string
  name: string
  category: string
  color: string | null
  material: string | null
  description: string | null
  imagePaths: string[]
  variants: VariantInput[]
}

export type UpdateProductInput = Omit<CreateProductInput, 'code' | 'variants'> & { isActive: boolean }
