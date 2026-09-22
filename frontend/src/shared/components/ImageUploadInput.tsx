import { useRef, useState } from 'react'
import { uploadImage } from '@/shared/api/uploads.api'

const splitPaths = (value: string) => value.split('\n').map((path) => path.trim()).filter(Boolean)

export function ImageUploadInput({
  branchId, purpose, value, onChange, multiple = true,
}: {
  branchId: string
  purpose: 'PRODUCT_IMAGE' | 'DAMAGE_EVIDENCE'
  value: string
  onChange: (value: string) => void
  multiple?: boolean
}) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const selectedPaths = splitPaths(value)

  const selectFiles = async (files: FileList | null) => {
    if (!files?.length) return
    setUploading(true); setError(null)
    try {
      const uploaded = await Promise.all([...files].map((file) => uploadImage(branchId, purpose, file)))
      onChange([...selectedPaths, ...uploaded].join('\n'))
    } catch (nextError) {
      setError((nextError as Error).message)
    } finally {
      setUploading(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  return <div className="image-upload">
    <input ref={inputRef} className="sr-only" type="file" accept="image/jpeg,image/png,image/webp" multiple={multiple} onChange={(event) => void selectFiles(event.target.files)} />
    <button className="button" type="button" disabled={uploading} onClick={() => inputRef.current?.click()}>{uploading ? 'Đang tải ảnh…' : 'Chọn ảnh từ máy'}</button>
    <small>JPG, PNG hoặc WebP · tối đa 10 MB/ảnh.</small>
    {selectedPaths.length > 0 && <ul className="image-upload__list">{selectedPaths.map((path) => <li key={path}><span>{path}</span><button type="button" aria-label="Xóa ảnh" onClick={() => onChange(selectedPaths.filter((item) => item !== path).join('\n'))}>×</button></li>)}</ul>}
    {error && <div className="inline-error">{error}</div>}
  </div>
}
