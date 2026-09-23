import { apiRequest } from '@/shared/api/http-client'
import type { AvailabilityCriteria, AvailabilityResult } from './availability.types'

export function searchAvailability(branchId: number, criteria: AvailabilityCriteria) {
  const query = new URLSearchParams({
    query: criteria.query,
    size: criteria.size,
    startAt: new Date(criteria.startAt).toISOString(),
    endAt: new Date(criteria.endAt).toISOString(),
    limit: '50',
  })
  return apiRequest<AvailabilityResult>(`/api/v1/availability?${query}`, { branchId })
}
