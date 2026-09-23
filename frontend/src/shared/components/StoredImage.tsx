import { useEffect, useState } from 'react'
import { getImageUrl } from '@/shared/api/uploads.api'

export function StoredImage({ branchId, objectPath, alt, className }: { branchId: number; objectPath: string; alt: string; className?: string }) {
  const [url, setUrl] = useState<string | null>(objectPath.startsWith('http://') || objectPath.startsWith('https://') ? objectPath : null)

  useEffect(() => {
    let active = true
    if (objectPath.startsWith('http://') || objectPath.startsWith('https://')) { setUrl(objectPath); return () => { active = false } }
    setUrl(null)
    void getImageUrl(branchId, objectPath).then((nextUrl) => { if (active) setUrl(nextUrl) }).catch(() => { if (active) setUrl(null) })
    return () => { active = false }
  }, [branchId, objectPath])

  return url ? <img className={className} src={url} alt={alt} /> : null
}
