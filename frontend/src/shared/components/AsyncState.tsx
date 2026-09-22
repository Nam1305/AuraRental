import type { ReactNode } from 'react'

type Props = {
  loading: boolean
  error: Error | null
  empty?: boolean
  children: ReactNode
}

export function AsyncState({ loading, error, empty, children }: Props) {
  if (loading) return <div className="state-card">Đang tải dữ liệu…</div>
  if (error) return <div className="state-card state-card--error">{error.message}</div>
  if (empty) return <div className="state-card">Chưa có dữ liệu phù hợp.</div>
  return children
}
