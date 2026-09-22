import { apiRequest } from '@/shared/api/http-client'
import type { CurrentUser } from './session.types'

export const getCurrentUser = () => apiRequest<CurrentUser>('/api/v1/me')
