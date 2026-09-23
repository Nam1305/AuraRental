import { useId, useState, type FormEvent } from 'react'
import { formatMoney } from '@/shared/format/money'

const VI_DIGITS = ['', 'một', 'hai', 'ba', 'bốn', 'năm', 'sáu', 'bảy', 'tám', 'chín']

function readVietnameseTriple(value: number, full = false) {
  const hundreds = Math.floor(value / 100)
  const tens = Math.floor((value % 100) / 10)
  const units = value % 10
  const words: string[] = []
  if (hundreds > 0 || full) words.push(`${hundreds > 0 ? VI_DIGITS[hundreds] : 'không'} trăm`)
  if (tens > 1) words.push(`${VI_DIGITS[tens]} mươi`)
  else if (tens === 1) words.push('mười')
  else if (units > 0 && (hundreds > 0 || full)) words.push('lẻ')
  if (units > 0) words.push(units === 1 && tens > 1 ? 'mốt' : units === 5 && tens > 0 ? 'lăm' : VI_DIGITS[units])
  return words.join(' ')
}

export function moneyInWords(value: number) {
  if (!Number.isFinite(value) || value <= 0) return ''
  const groups = ['', 'nghìn', 'triệu', 'tỷ']
  const chunks: number[] = []
  let rest = Math.floor(value)
  while (rest > 0) { chunks.push(rest % 1000); rest = Math.floor(rest / 1000) }
  return chunks.reduceRight<string[]>((words, chunk, index) => {
    if (chunk === 0) return words
    const suffix = groups[index] ? ` ${groups[index]}` : ''
    return [...words, `${readVietnameseTriple(chunk, words.length > 0)}${suffix}`]
  }, []).join(' ') + ' đồng'
}

export function MoneyField({ label, value, onChange, required = false, disabled = false }: { label: string; value: string; onChange: (value: string) => void; required?: boolean; disabled?: boolean }) {
  const id = useId()
  const digits = value.replace(/\D/g, '')
  const amount = Number(digits)
  const firstMultiplier = 10 ** Math.max(0, 6 - digits.length)
  const suggestions = digits.length > 0 && digits.length <= 3 && amount > 0
    ? [...new Set([amount * firstMultiplier, amount * firstMultiplier * 10, amount * firstMultiplier * 100])]
    : []

  return <div className="field money-field">
    <label htmlFor={id}>{label}{required ? ' *' : ''}</label>
    <div className="money-input"><input id={id} required={required} disabled={disabled} inputMode="numeric" value={digits} onChange={(event) => onChange(event.target.value.replace(/\D/g, ''))} placeholder="VD: 1500000" /><span>đ</span></div>
    {suggestions.length > 0 ? <div className="money-suggestions"><small>Nhập {digits} — chọn nhanh mệnh giá:</small><div>{suggestions.map((suggestion) => <button type="button" key={suggestion} disabled={disabled} onClick={() => onChange(String(suggestion))}><strong>{formatMoney(suggestion)}</strong><span>{moneyInWords(suggestion)}</span></button>)}</div></div>
      : amount > 0 && <small className="money-spellout">{formatMoney(amount)} · {moneyInWords(amount)}</small>}
  </div>
}

export function MoneyDialog({ title, initialAmount, confirmLabel, onConfirm, onClose }: { title: string; initialAmount: number; confirmLabel: string; onConfirm: (amount: number) => void; onClose: () => void }) {
  const [value, setValue] = useState(String(initialAmount))
  const [error, setError] = useState<string | null>(null)
  const submit = (event: FormEvent) => {
    event.preventDefault()
    const amount = Number(value)
    if (!Number.isFinite(amount) || amount <= 0) { setError('Nhập số tiền lớn hơn 0.'); return }
    onConfirm(amount)
  }

  return <div className="money-dialog-layer" role="dialog" aria-modal="true" aria-label={title}>
    <button className="money-dialog-backdrop" type="button" aria-label="Đóng" onClick={onClose} />
    <form className="money-dialog panel" onSubmit={submit}>
      <div className="section-heading"><div><span className="eyebrow">Nhập mệnh giá</span><h2>{title}</h2></div><button className="button" type="button" onClick={onClose}>Đóng</button></div>
      <MoneyField label="Số tiền" value={value} onChange={setValue} required />
      {error && <div className="inline-error">{error}</div>}
      <div className="action-buttons"><button className="button button--primary">{confirmLabel}</button><button className="button" type="button" onClick={onClose}>Hủy</button></div>
    </form>
  </div>
}
