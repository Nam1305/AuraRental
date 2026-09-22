import { apiRequest } from '@/shared/api/http-client'

export type LoginResult = {
  accessToken: string
  tokenType: 'Bearer'
  expiresAt: string
}

export const login = (identifier: string, password: string) =>
  apiRequest<LoginResult>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ identifier, password }),
  })
