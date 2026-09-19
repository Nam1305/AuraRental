// One canvas is used for the on-screen PNG, clipboard image and download.
const AuraRefundReceipt = (() => {
  const font = 'Arial, sans-serif'
  const vnd = amount => `${new Intl.NumberFormat('vi-VN').format(amount)}đ`

  function create(data, status) {
    const canvas = document.createElement('canvas')
    canvas.width = 900
    canvas.height = 840 + data.items.length * 100
    const ctx = canvas.getContext('2d')
    const left = 64, right = 836
    const text = (value, x, y, size = 24, weight = 400, color = '#28231f', align = 'left') => {
      ctx.font = `${weight} ${size}px ${font}`
      ctx.fillStyle = color
      ctx.textAlign = align
      ctx.fillText(value, x, y)
    }
    const box = (x, y, width, height, fill, radius = 16) => {
      ctx.beginPath()
      ctx.roundRect(x, y, width, height, radius)
      ctx.fillStyle = fill
      ctx.fill()
    }
    const line = y => {
      ctx.beginPath()
      ctx.moveTo(left, y)
      ctx.lineTo(right, y)
      ctx.strokeStyle = '#e9e4df'
      ctx.lineWidth = 2
      ctx.stroke()
    }
    ctx.fillStyle = '#fffdfb'
    ctx.fillRect(0, 0, canvas.width, canvas.height)
    ctx.fillStyle = '#813b4c'
    ctx.fillRect(0, 0, canvas.width, 168)
    text('AURA RENTAL', left, 76, 38, 700, '#ffffff')
    text('TỔNG KẾT THUÊ & HOÀN TIỀN', left, 121, 22, 400, '#f4dce1')
    text(data.id, left, 225, 34, 700)
    text(`Khách hàng: ${data.customer}`, left, 267, 26, 400, '#776f68')
    box(left, 292, right - left, 52, status === 'completed' ? '#eaf6f1' : '#fbf4e9', 9)
    text(status === 'completed' ? 'ĐÃ HOÀN TIỀN CHO KHÁCH' : 'ĐÃ DUYỆT · CHỜ CHUYỂN KHOẢN', left + 18, 326, 21, 700, status === 'completed' ? '#28735c' : '#855c18')
    text('CÁC MÓN ĐÃ THUÊ', left, 399, 22, 700, '#813b4c')
    let y = 443
    for (const item of data.items) {
      text(item.name, left, y, 25, 700)
      text(item.fee ? vnd(item.fee) : 'Miễn phí', right, y, 25, 700, '#28231f', 'right')
      text(`${item.code} · ${item.package}`, left, y + 36, 21, 400, '#776f68')
      line(y + 64)
      y += 100
    }
    const summary = [
      ['Tiền khách cọc ban đầu', vnd(data.deposit)],
      ['Tổng giá thuê', `− ${vnd(data.rentalFee)}`],
      ['Phí xử lý', `− ${vnd(data.damageFee)}`],
    ]
    for (const [label, value] of summary) {
      text(label, left, y, 25, 400, '#776f68')
      text(value, right, y, 26, 700, '#28231f', 'right')
      y += 46
    }
    text(data.damageNote, left, y - 2, 21, 400, '#776f68')
    y += 30
    box(left, y, right - left, 128, '#f9eef0')
    text(status === 'completed' ? 'TIỀN ĐÃ HOÀN LẠI KHÁCH' : 'TIỀN HOÀN LẠI KHÁCH', left + 26, y + 40, 22, 700, '#813b4c')
    text(vnd(data.refund), left + 26, y + 98, 48, 700, '#813b4c')
    text('Cảm ơn bạn đã chọn Aura Rental ♡', 450, canvas.height - 38, 23, 400, '#a65569', 'center')
    return canvas
  }

  function blob(canvas) {
    return new Promise((resolve, reject) => canvas.toBlob(image => image ? resolve(image) : reject(new Error('Không tạo được PNG')), 'image/png'))
  }

  return { create, blob }
})()
