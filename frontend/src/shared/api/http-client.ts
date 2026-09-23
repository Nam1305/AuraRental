import type { ApiErrorResponse, ApiResponse } from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'
const ACCESS_TOKEN_KEY = 'aura.accessToken'
const ACTIVE_BRANCH_KEY = 'aura.activeBranchId'

export class ApiError extends Error {
  constructor(
    readonly code: string,
    message: string,
    readonly status: number,
    readonly requestId?: string,
    readonly fields?: Record<string, string> | null,
  ) {
    super(message)
  }
}

export function getAccessToken() {
  return localStorage.getItem(ACCESS_TOKEN_KEY)
}

export function setAccessToken(token: string | null) {
  if (token) localStorage.setItem(ACCESS_TOKEN_KEY, token)
  else localStorage.removeItem(ACCESS_TOKEN_KEY)
}

export function getStoredBranchId() {
  return localStorage.getItem(ACTIVE_BRANCH_KEY)
}

export function setStoredBranchId(branchId: string) {
  localStorage.setItem(ACTIVE_BRANCH_KEY, branchId)
}

export function idempotencyHeaders() {
  return { 'Idempotency-Key': crypto.randomUUID() }
}

type RequestOptions = RequestInit & { branchId?: string | null }

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  if (options.body) headers.set('Content-Type', 'application/json')

  const token = getAccessToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (options.branchId) headers.set('X-Branch-Id', options.branchId)

  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers })
  if (!response.ok) {
    const payload = (await response.json().catch(() => null)) as ApiErrorResponse | null
    throw new ApiError(
      payload?.error.code ?? 'HTTP_ERROR',
      payload?.error.message ?? 'Không thể kết nối đến máy chủ.',
      response.status,
      payload?.error.requestId,
      payload?.error.fields,
    )
  }

  const payload = (await response.json()) as ApiResponse<T>
  return payload.data
}

/** Calls an anonymous endpoint without a staff session or active-branch context. */
export async function publicApiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  if (options.body) headers.set('Content-Type', 'application/json')

  const response = await fetch(`${API_BASE_URL}${path}`, { ...options, headers })
  if (!response.ok) {
    const payload = (await response.json().catch(() => null)) as ApiErrorResponse | null
    throw new ApiError(
      payload?.error.code ?? 'HTTP_ERROR',
      payload?.error.message ?? 'Không thể kết nối đến máy chủ.',
      response.status,
      payload?.error.requestId,
      payload?.error.fields,
    )
  }

  const payload = (await response.json()) as ApiResponse<T>
  return payload.data
}
