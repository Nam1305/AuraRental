import { apiRequest, idempotencyHeaders } from '@/shared/api/http-client'
import type {
  CreateReservationInput,
  Quote,
  RentalSelection,
  ReservationDetail,
  ReservationListItem,
} from './reservation.types'

export function searchReservations(branchId: number, status: string, query: string) {
  const search = new URLSearchParams({ status, query, limit: '50' })
  return apiRequest<ReservationListItem[]>(`/api/v1/reservations?${search}`, { branchId })
}

export const getReservation = (branchId: number, reservationId: number) =>
  apiRequest<ReservationDetail>(`/api/v1/reservations/${reservationId}`, { branchId })

export function createQuote(
  branchId: number,
  input: Omit<CreateReservationInput, 'receivedPayment'>,
) {
  return apiRequest<Quote>('/api/v1/quotes', {
    method: 'POST',
    branchId,
    body: JSON.stringify({
      ...input,
      rentalStartAt: new Date(input.rentalStartAt).toISOString(),
      rentalEndAt: new Date(input.rentalEndAt).toISOString(),
    }),
  })
}

export function createReservation(branchId: number, input: CreateReservationInput) {
  return apiRequest<ReservationDetail>('/api/v1/reservations', {
    method: 'POST',
    branchId,
    headers: idempotencyHeaders(),
    body: JSON.stringify({
      ...input,
      rentalStartAt: new Date(input.rentalStartAt).toISOString(),
      rentalEndAt: new Date(input.rentalEndAt).toISOString(),
    }),
  })
}

export const reissueOtp = (branchId: number, reservationId: number, reason: string) =>
  apiRequest<{ formUrl: string; otp: string; expiresAt: string }>(
    `/api/v1/reservations/${reservationId}/otp/reissue`,
    {
      method: 'POST',
      branchId,
      headers: idempotencyHeaders(),
      body: JSON.stringify({ reason }),
    },
  )

export const cancelReservation = (branchId: number, reservationId: number, reason: string) =>
  apiRequest(`/api/v1/reservations/${reservationId}/cancel`, {
    method: 'POST',
    branchId,
    headers: idempotencyHeaders(),
    body: JSON.stringify({ reason, paymentDecision: 'REVIEW_SEPARATELY' }),
  })

export const recordReservationPayment = (
  branchId: number,
  reservationId: number,
  type: 'TARGET_DEPOSIT' | 'ADDITIONAL_COLLECTION',
  amount: number,
  method = 'BANK_TRANSFER',
) =>
  apiRequest(`/api/v1/reservations/${reservationId}/payments`, {
    method: 'POST',
    branchId,
    headers: idempotencyHeaders(),
    body: JSON.stringify({
      type,
      amount,
      method,
      transactionRef: null,
      proofPath: null,
      paidAt: new Date().toISOString(),
      confirmNow: true,
      note: null,
    }),
  })

export type { RentalSelection }
