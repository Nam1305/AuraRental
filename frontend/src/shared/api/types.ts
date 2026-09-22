export type ApiMeta = {
  requestId: string
  nextCursor?: string | null
  hasMore?: boolean | null
}

export type ApiResponse<T> = {
  data: T
  meta: ApiMeta
}

export type ApiErrorResponse = {
  error: {
    code: string
    message: string
    fields?: Record<string, string> | null
    requestId: string
  }
}
