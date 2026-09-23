import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import {
  getAccessToken,
  getStoredBranchId,
  setAccessToken,
  setStoredBranchId,
} from '@/shared/api/http-client'
import { getCurrentUser } from './session.api'
import type { CurrentUser } from './session.types'
import { login as requestLogin } from '@/features/auth/auth.api'

type SessionValue = {
  user: CurrentUser | null
  activeBranchId: number | null
  loading: boolean
  error: Error | null
  login: (identifier: string, password: string) => Promise<void>
  logout: () => void
  selectBranch: (branchId: number) => void
}

const SessionContext = createContext<SessionValue | null>(null)

export function SessionProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [activeBranchId, setActiveBranchId] = useState<number | null>(null)
  const [loading, setLoading] = useState(Boolean(getAccessToken()))
  const [error, setError] = useState<Error | null>(null)

  const applyCurrentUser = async () => {
    const nextUser = await getCurrentUser()
    const storedValue = getStoredBranchId()
    const stored = storedValue && /^\d+$/.test(storedValue) ? Number(storedValue) : null
    const allowedStoredBranch = nextUser.branches.some((branch) => branch.id === stored)
    const nextBranch = allowedStoredBranch
      ? stored
      : nextUser.suggestedBranchId ?? nextUser.branches[0]?.id ?? null
    setUser(nextUser)
    setActiveBranchId(nextBranch)
    if (nextBranch) setStoredBranchId(nextBranch)
  }

  const loadSession = async () => {
    setLoading(true)
    setError(null)
    try {
      await applyCurrentUser()
    } catch (nextError) {
      setError(nextError as Error)
      setUser(null)
      setAccessToken(null)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (getAccessToken()) void loadSession()
  }, [])

  const value = useMemo<SessionValue>(() => ({
    user,
    activeBranchId,
    loading,
    error,
    login: async (identifier, password) => {
      setLoading(true)
      setError(null)
      try {
        const result = await requestLogin(identifier.trim(), password)
        setAccessToken(result.accessToken)
        await applyCurrentUser()
      } catch (nextError) {
        setAccessToken(null)
        setUser(null)
        setActiveBranchId(null)
        setError(nextError as Error)
      } finally {
        setLoading(false)
      }
    },
    logout: () => {
      setAccessToken(null)
      setUser(null)
      setActiveBranchId(null)
    },
    selectBranch: (branchId) => {
      if (!user?.branches.some((branch) => branch.id === branchId)) return
      setActiveBranchId(branchId)
      setStoredBranchId(branchId)
    },
  }), [activeBranchId, error, loading, user])

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession() {
  const value = useContext(SessionContext)
  if (!value) throw new Error('useSession must be used inside SessionProvider')
  return value
}
