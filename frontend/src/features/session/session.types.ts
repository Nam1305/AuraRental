export type Branch = {
  id: number
  code: string
  name: string
  address: string
  isActive: boolean
}

export type CurrentUser = {
  id: number
  name: string
  username: string
  email: string
  role: 'STAFF' | 'MANAGER'
  branches: Branch[]
  suggestedBranchId: number | null
}
