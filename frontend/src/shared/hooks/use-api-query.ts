import { useCallback, useEffect, useState } from 'react'

type QueryState<T> = {
  data: T | null
  error: Error | null
  loading: boolean
}

export function useApiQuery<T>(query: () => Promise<T>, dependencies: readonly unknown[]) {
  const [state, setState] = useState<QueryState<T>>({ data: null, error: null, loading: true })

  const reload = useCallback(async () => {
    setState((current) => ({ ...current, error: null, loading: true }))
    try {
      const data = await query()
      setState({ data, error: null, loading: false })
    } catch (error) {
      setState({ data: null, error: error as Error, loading: false })
    }
    // The caller controls query identity through this explicit dependency list.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, dependencies)

  useEffect(() => {
    void reload()
  }, [reload])

  return { ...state, reload }
}
