import { apiRequest, idempotencyHeaders } from '@/shared/api/http-client'
import type {
  CreateReservationInput,
  Quote,
  RentalSelection,
  ReservationDetail,
  ReservationListItem,
} from './reservation.types'

export function searchReservations(branchId: string, status: string, query: string) {
  const search = new URLSearchParams({ status, query, limit: '50' })
  return apiRequest<ReservationListItem[]>(`/api/v1/reservations?${search}`, { branchId })
}

export const getReservation = (branchId: string, reservationId: string) =>
  apiRequest<ReservationDetail>(`/api/v1/reservations/${reservationId}`, { branchId })

export function createQuote(
  branchId: string,
  input: Omit<CreateReservationInput, 'depositDeadlineAt' | 'receivedPayment'>,
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

export function createReservation(branchId: string, input: CreateReservationInput) {
  return apiRequest<ReservationDetail>('/api/v1/reservations', {
    method: 'POST',
    branchId,
    headers: idempotencyHeaders(),
    body: JSON.stringify({
      ...input,
      rentalStartAt: new Date(input.rentalStartAt).toISOString(),
      rentalEndAt: new Date(input.rentalEndAt).toISOString(),
      depositDeadlineAt: input.depositDeadlineAt
        ? new Date(input.depositDeadlineAt).toISOString()
        : null,
    }),
  })
}

export const reissueOtp = (branchId: string, reservationId: string, reason: string) =>
  apiRequest<{ formUrl: string; otp: string; expiresAt: string }>(
    `/api/v1/reservations/${reservationId}/otp/reissue`,
    {
      method: 'POST',
      branchId,
      headers: idempotencyHeaders(),
      body: JSON.stringify({ reason }),
    },
  )

export const extendDeadline = (
  branchId: string,
  reservationId: string,
  depositDeadlineAt: string,
  reason: string,
) =>
  apiRequest<ReservationDetail>(`/api/v1/reservations/${reservationId}/extend-deadline`, {
    method: 'POST',
    branchId,
    headers: idempotencyHeaders(),
    body: JSON.stringify({ depositDeadlineAt: new Date(depositDeadlineAt).toISOString(), reason }),
  })

export const cancelReservation = (branchId: string, reservationId: string, reason: string) =>
  apiRequest(`/api/v1/reservations/${reservationId}/cancel`, {
    method: 'POST',
    branchId,
    headers: idempotencyHeaders(),
    body: JSON.stringify({ reason, paymentDecision: 'REVIEW_SEPARATELY' }),
  })

export const recordReservationPayment = (
  branchId: string,
  reservationId: string,
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
