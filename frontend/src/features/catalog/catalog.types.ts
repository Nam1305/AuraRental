export type ProductListItem = {
  id: string
  code: string
  name: string
  category: string
  coverImagePath: string | null
  sizes: string[]
  activeInventoryCount: number
  availableNowCount: number
  priceFrom: number | null
  packageCodes: string[]
}
