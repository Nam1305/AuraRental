export type Branch = {
  id: string
  code: string
  name: string
  address: string
  isActive: boolean
}

export type CurrentUser = {
  id: string
  name: string
  username: string
  email: string
  role: 'STAFF' | 'MANAGER'
  branches: Branch[]
  suggestedBranchId: string | null
}
