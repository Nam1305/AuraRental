import { useEffect, useRef } from 'react'
import { formatMoney } from '@/shared/format/money'
import type { RefundReceipt } from './return.types'

function drawReceipt(canvas: HTMLCanvasElement, receipt: RefundReceipt) {
  const context = canvas.getContext('2d')
  if (!context) return
  const width = 900
  const left = 64
  const right = width - 64
  const itemHeight = 92
  canvas.width = width
  canvas.height = 780 + receipt.items.length * itemHeight
  const text = (value: string, x: number, y: number, size = 24, weight = 400, color = '#30272c', align: CanvasTextAlign = 'left') => {
    context.font = `${weight} ${size}px Arial, sans-serif`
    context.fillStyle = color
    context.textAlign = align
    context.fillText(value, x, y)
  }
  const line = (y: number) => {
    context.beginPath(); context.moveTo(left, y); context.lineTo(right, y)
    context.strokeStyle = '#e8dedb'; context.lineWidth = 2; context.stroke()
  }
  const box = (y: number, height: number, color: string) => {
    context.beginPath(); context.roundRect(left, y, right - left, height, 14)
    context.fillStyle = color; context.fill()
  }
  const isAdditionalCollection = receipt.additionalCollection > 0
  const settledLabel = isAdditionalCollection ? 'ĐÃ ĐỐI SOÁT KHOẢN CHÊNH LỆCH' : 'ĐÃ HOÀN TIỀN CHO KHÁCH'
  const pendingLabel = isAdditionalCollection ? 'ĐÃ DUYỆT · CHỜ KHÁCH THANH TOÁN' : 'ĐÃ DUYỆT · CHỜ CHUYỂN KHOẢN'

  context.fillStyle = '#fffdfc'; context.fillRect(0, 0, canvas.width, canvas.height)
  context.fillStyle = '#813b4c'; context.fillRect(0, 0, width, 164)
  text('AURA RENTAL', left, 72, 38, 700, '#fff')
  text('TỔNG KẾT THUÊ & HOÀN TIỀN', left, 117, 22, 400, '#f5dfe4')
  text(receipt.orderNo, left, 221, 32, 700)
  text(`Khách hàng: ${receipt.customerName}`, left, 260, 24, 400, '#786d72')
  text(receipt.branchName, left, 292, 19, 400, '#786d72')
  box(318, 52, receipt.status === 'SETTLED' ? '#eaf6f1' : '#fbf4e9')
  text(receipt.status === 'SETTLED' ? settledLabel : pendingLabel, left + 18, 351, 20, 700, receipt.status === 'SETTLED' ? '#28735c' : '#855c18')
  text('CÁC MÓN ĐÃ THUÊ', left, 420, 21, 700, '#813b4c')
  let y = 462
  receipt.items.forEach((item) => {
    text(`${item.productName} · ${item.size}`, left, y, 24, 700)
    text(item.rentalFee > 0 ? formatMoney(item.rentalFee) : 'Miễn phí', right, y, 24, 700, '#30272c', 'right')
    text(`${item.assetCode}${item.packageCode ? ` · ${item.packageCode}` : ''}`, left, y + 32, 19, 400, '#786d72')
    line(y + 56); y += itemHeight
  })
  ;[['Tiền khách cọc ban đầu', formatMoney(receipt.depositAmount)], ['Tổng giá thuê', `− ${formatMoney(receipt.rentalFee)}`], ['Phí xử lý', `− ${formatMoney(receipt.processingFee)}`]].forEach(([label, value]) => {
    text(label, left, y, 23, 400, '#786d72'); text(value, right, y, 24, 700, '#30272c', 'right'); y += 44
  })
  const isRefund = receipt.refundAmount > 0
  box(y + 4, 124, isRefund ? '#f9eef0' : '#fff0f0')
  text(isRefund ? 'TIỀN HOÀN LẠI KHÁCH' : 'KHÁCH CẦN THANH TOÁN THÊM', left + 24, y + 43, 21, 700, isRefund ? '#813b4c' : '#9b333e')
  text(formatMoney(isRefund ? receipt.refundAmount : receipt.additionalCollection), left + 24, y + 95, 45, 700, isRefund ? '#813b4c' : '#9b333e')
  text('Cảm ơn bạn đã chọn Aura Rental ♡', width / 2, canvas.height - 34, 21, 400, '#a65569', 'center')
}

export function RefundReceiptImage({ receipt, onMessage }: { receipt: RefundReceipt; onMessage: (message: string) => void }) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  useEffect(() => { if (canvasRef.current) drawReceipt(canvasRef.current, receipt) }, [receipt])
  const toBlob = () => new Promise<Blob>((resolve, reject) => canvasRef.current?.toBlob((blob) => blob ? resolve(blob) : reject(new Error('Không tạo được ảnh PNG')), 'image/png'))
  const copy = async () => {
    try {
      const blob = await toBlob()
      if (!navigator.clipboard?.write || typeof ClipboardItem === 'undefined') throw new Error('unsupported')
      await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })])
      onMessage('Đã sao chép ảnh. Mở chat với khách và dán để gửi.')
    } catch { onMessage('Thiết bị chưa hỗ trợ copy ảnh. Hãy tải PNG rồi gửi khách.') }
  }
  const download = async () => {
    const blob = await toBlob(); const url = URL.createObjectURL(blob)
    const link = document.createElement('a'); link.href = url; link.download = `${receipt.orderNo}-tong-ket.png`; link.click(); URL.revokeObjectURL(url)
  }
  return <section className="refund-receipt">
    <div className="section-heading"><div><span className="eyebrow">Ảnh gửi khách</span><h3>Tổng kết thuê & hoàn tiền</h3><p>Ảnh dùng dữ liệu đã được duyệt; copy rồi dán trực tiếp vào chat.</p></div></div>
    <canvas ref={canvasRef} className="refund-receipt__image" aria-label={`Ảnh tổng kết đơn ${receipt.orderNo}`} />
    <div className="action-buttons"><button className="button button--primary" type="button" onClick={() => void copy()}>Sao chép ảnh</button><button className="button" type="button" onClick={() => void download()}>Tải PNG</button></div>
  </section>
}
