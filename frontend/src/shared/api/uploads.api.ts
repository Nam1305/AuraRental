import { apiRequest } from './http-client'

type UploadPurpose = 'PRODUCT_IMAGE' | 'DAMAGE_EVIDENCE'

type PresignedUpload = {
  objectPath: string
  uploadUrl: string
  expiresAt: string
}

type PresignedDownload = {
  downloadUrl: string
  expiresAt: string
}

const MAX_IMAGE_BYTES = 10 * 1024 * 1024
const ALLOWED_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp'])

export async function uploadImage(branchId: string, purpose: UploadPurpose, file: File) {
  if (!ALLOWED_TYPES.has(file.type)) throw new Error('Chỉ chấp nhận ảnh JPG, PNG hoặc WebP.')
  if (file.size === 0 || file.size > MAX_IMAGE_BYTES) throw new Error('Mỗi ảnh phải lớn hơn 0 và không quá 10 MB.')

  const presign = await apiRequest<PresignedUpload>('/api/v1/uploads/presign', {
    method: 'POST', branchId,
    body: JSON.stringify({ purpose, fileName: file.name, contentType: file.type, sizeBytes: file.size }),
  })
  const response = await fetch(presign.uploadUrl, {
    method: 'PUT', headers: { 'Content-Type': file.type }, body: file,
  })
  if (!response.ok) throw new Error('Không thể tải ảnh lên kho lưu trữ. Vui lòng thử lại.')
  return presign.objectPath
}

export const getImageUrl = (branchId: string, objectPath: string) =>
  apiRequest<PresignedDownload>(`/api/v1/uploads/presign-read?${new URLSearchParams({ objectPath })}`, { branchId })
    .then((result) => result.downloadUrl)
