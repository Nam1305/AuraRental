import { idempotencyHeaders, publicApiRequest } from '@/shared/api/http-client'
import type { PublicRentalForm, SubmitPublicRentalFormRequest, SubmittedPublicRentalForm } from './public-rental-form.types'

const basePath = '/api/v1/public/rental-forms'

export function getPublicRentalForm(token: string) {
  return publicApiRequest<PublicRentalForm>(`${basePath}/${encodeURIComponent(token)}`)
}

export function submitPublicRentalForm(token: string, request: SubmitPublicRentalFormRequest) {
  return publicApiRequest<SubmittedPublicRentalForm>(`${basePath}/${encodeURIComponent(token)}/submit`, {
    method: 'POST',
    headers: idempotencyHeaders(),
    body: JSON.stringify(request),
  })
}
