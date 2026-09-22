const root = document.querySelector('#view-root')
const breadcrumb = document.querySelector('#breadcrumb')
const modalBackdrop = document.querySelector('#modal-backdrop')
const modal = document.querySelector('#modal')
const toastArea = document.querySelector('#toast-area')
const loginScreen = document.querySelector('#login-screen')
const loginForm = document.querySelector('#login-form')
const loginEmail = document.querySelector('#login-email')
const loginPassword = document.querySelector('#login-password')
const appShell = document.querySelector('#app-shell')
const mobileNav = document.querySelector('#mobile-nav')
const activeBranchName = document.querySelector('#active-branch-name')

const state = {
  view: 'dashboard',
  issueComplete: false,
  selectedOrder: 'AR-240914-018',
  branchId: 'hanoi',
  rentalPackage: '3D',
  selectedAsset: 'HN-AF-GAM-S-03',
  returnItemConditions: { dress: 'damaged', accessory: 'missing' },
  role: 'staff',
  scheduleExpanded: false,
  idVerified: false,
  refundStatus: 'draft',
  refundSnapshot: null,
  refundPreviousApproval: null,
  refundAdjustmentReason: '',
}

const branches = {
  hanoi: {
    id: 'hanoi', code: 'HN', city: 'Hà Nội', name: 'Hà Nội · Nguyễn Trãi', address: '128 Nguyễn Trãi, Thanh Xuân',
    packages: [{ code: '12H', label: '12 giờ', price: 190000 }, { code: '1D', label: '1 ngày', price: 220000 }, { code: '3D', label: '3 ngày', price: 320000 }],
    assets: ['HN-AF-GAM-S-03', 'HN-AF-GAM-S-05', 'HN-AF-GAM-S-09'], available: 12, renting: 4, holds: 2,
  },
  saigon: {
    id: 'saigon', code: 'SG', city: 'Sài Gòn', name: 'Sài Gòn · Quận 3', address: '112 Võ Văn Tần, Quận 3',
    packages: [{ code: '1D', label: '1 ngày', price: 260000 }, { code: '2D', label: '2 ngày', price: 360000 }, { code: '3D', label: '3 ngày', price: 440000 }],
    assets: ['SG-AF-GAM-S-01', 'SG-AF-GAM-S-04', 'SG-AF-GAM-S-07'], available: 7, renting: 3, holds: 1,
  },
}

function currentBranch() { return branches[state.branchId] }
function currentPackage() { return currentBranch().packages.find(item => item.code === state.rentalPackage) || currentBranch().packages[0] }

const returnOrder = {
  id: 'AR-240912-009', customer: 'Bảo Trâm', deposit: 750000,
  items: [
    {
      id: 'dress', name: 'Selene satin · Size M', code: 'SE-SAT-M-03', fee: 320000, package: '3 ngày',
      issue: {
        damaged: { type: 'Rách nhẹ', severity: 'Cần sửa', fee: 120000, note: 'Rách 2 cm ở lai váy, cần may lại.', photos: 2, outcome: 'Bảo trì · chưa thể cho thuê' },
        missing: { type: 'Không trả váy', severity: 'Mất toàn bộ item', fee: 1200000, note: 'Chưa nhận lại mã SE-SAT-M-03 từ khách.', photos: 0, outcome: 'Mất · loại khỏi tồn kho' },
      },
    },
    {
      id: 'accessory', name: 'Khuyên pha lê', code: 'ACC-CRYS-04', fee: 0, package: 'Đi kèm váy', replacementValue: 180000,
      issue: {
        damaged: { type: 'Gãy chốt', severity: 'Cần thay chốt', fee: 80000, note: 'Chốt khuyên bị gãy, cần thay trước khi cho thuê lại.', photos: 1, outcome: 'Bảo trì · chưa thể cho thuê' },
        missing: { type: 'Mất 1 chiếc', severity: 'Không thể hoàn bộ', fee: 180000, note: 'Khách trả thiếu 1 chiếc khuyên.', photos: 0, outcome: 'Mất · loại khỏi tồn kho' },
      },
    },
  ],
}

function formatVnd(amount) { return `${new Intl.NumberFormat('vi-VN').format(amount)}đ` }
function refundTime() {
  const now = new Date()
  return `${now.toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', day: '2-digit', month: '2-digit', year: 'numeric' })} · ${now.toLocaleTimeString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh', hour: '2-digit', minute: '2-digit', hour12: false })}`
}
function calculateRefund() {
  const baseOrder = state.branchId === 'saigon'
    ? { ...returnOrder, id: 'SG-240913-004', customer: 'Uyên Nhi', deposit: 900000, items: returnOrder.items.map(item => item.id === 'dress' ? { ...item, code: 'SG-SE-SAT-M-02', fee: 440000 } : { ...item, code: 'SG-ACC-CRYS-02' }) }
    : returnOrder
  const items = baseOrder.items.map(item => {
    const condition = state.returnItemConditions[item.id] || 'good'
    const issue = item.issue[condition]
    return { ...item, condition, processingFee: issue?.fee || 0, inspection: issue || null }
  })
  const rentalFee = items.reduce((total, item) => total + item.fee, 0)
  const damageFee = items.reduce((total, item) => total + item.processingFee, 0)
  const issueCount = items.filter(item => item.condition !== 'good').length
  const netSettlement = baseOrder.deposit - rentalFee - damageFee
  return { ...baseOrder, items, rentalFee, damageFee, damageNote: issueCount ? `${issueCount} món có phát sinh xử lý` : 'Không ghi nhận hư hại', refund: Math.max(0, netSettlement), additionalCollection: Math.max(0, -netSettlement) }
}
function refundData() { return state.refundSnapshot || calculateRefund() }

const orders = [
  { id: 'AR-240914-018', branch: 'hanoi', name: 'Ngọc Anh', initials: 'NA', item: 'Afrodille gấm · S', date: '14–17 Thg 9', status: 'Chờ cọc', tone: 'pending', deposit: '500.000đ', staff: 'Minh Lan' },
  { id: 'AR-240913-014', branch: 'hanoi', name: 'Thu Hà', initials: 'TH', item: 'Afrodille trắng · S', date: '14–15 Thg 9', status: 'Đang thuê', tone: 'renting', deposit: '1.550.000đ', staff: 'Minh Lan' },
  { id: 'AR-240912-009', branch: 'hanoi', name: 'Bảo Trâm', initials: 'BT', item: 'Selene satin · M', date: '12–15 Thg 9', status: 'Chờ hoàn', tone: 'refund', deposit: '750.000đ', staff: 'Hạ Vy' },
  { id: 'SG-240914-007', branch: 'saigon', name: 'Khánh Linh', initials: 'KL', item: 'Afrodille gấm · S', date: '21–23 Thg 9', status: 'Chờ cọc', tone: 'pending', deposit: '600.000đ', staff: 'Mai Anh' },
  { id: 'SG-240913-004', branch: 'saigon', name: 'Uyên Nhi', initials: 'UN', item: 'Selene satin · M', date: '20–21 Thg 9', status: 'Đang thuê', tone: 'renting', deposit: '1.200.000đ', staff: 'Mai Anh' },
]

const labels = {
  dashboard: 'Hôm nay', schedule: 'Tìm đồ trống', 'issue-form': 'Giữ chỗ', orders: 'Đơn hàng', returns: 'Trả đồ', products: 'Kho sản phẩm', 'product-add': 'Thêm sản phẩm', 'product-detail': 'Chi tiết sản phẩm', reports: 'Báo cáo', settings: 'Cấu hình', 'order-detail': 'Chi tiết đơn',
}

function badge(text, tone) { return `<span class="badge ${tone}">${text}</span>` }
function avatar(initials, type = 'customer') { return `<span class="avatar tiny avatar-${type}">${initials}</span>` }
function money(value) { return `<strong>${value}</strong>` }

function pageHeader(title, description, actions = '') {
  return `<div class="page-header"><div><h1>${title}</h1><p>${description}</p></div><div class="header-actions">${actions}</div></div>`
}

function branchScopeBar() {
  const branch = currentBranch()
  return `<button class="branch-scope-bar" data-action="open-branch-picker"><span class="branch-pin">⌖</span><span><small>ĐANG LÀM VIỆC TẠI</small><strong>${branch.name}</strong></span><em>Đổi ›</em></button>`
}

function ordersTable(items = orders) {
  return `<div class="table-scroll"><table class="orders-table"><thead><tr><th>Mã đơn</th><th>Khách hàng</th><th>Sản phẩm</th><th>Lịch thuê</th><th>Trạng thái</th><th></th></tr></thead><tbody>${items.map(order => `
    <tr data-order="${order.id}">
      <td><span class="order-code">${order.id}</span></td>
      <td><div class="customer-cell">${avatar(order.initials)}<span>${order.name}</span></div></td>
      <td>${order.item}</td><td>${order.date}</td><td>${badge(order.status, order.tone)}</td>
      <td><button class="inline-link" data-order="${order.id}">Xem <span class="mobile-only">›</span></button></td>
    </tr>`).join('')}</tbody></table></div>`
}

function dashboardView() {
  const manager = state.role === 'manager'
  const name = manager ? 'Quỳnh Anh' : 'Minh Lan'
  const description = manager ? 'Thứ Bảy, 14 tháng 9 · Có 2 yêu cầu hoàn tiền và 3 thay đổi cần bạn duyệt.' : 'Thứ Bảy, 14 tháng 9 · Đây là những việc cần ưu tiên hôm nay.'
  const actions = manager ? `<button class="button secondary" data-view="reports">◔ Xem báo cáo</button><button class="button" data-view="returns">Duyệt hoàn tiền</button>` : `<button class="button secondary" data-view="schedule">▦ Xem lịch</button><button class="button" data-view="issue-form">＋ Cấp mã & form</button>`
  return `${pageHeader(`Chào buổi sáng, ${name}`, description, actions)}
  <div class="alert-strip"><span>◷</span><span><strong>2 hold sắp hết hạn</strong> trong 30 phút tới. Hãy nhắc khách hoàn tất form để giữ lịch.</span><button data-view="schedule">Xem hold</button></div>
  <div class="grid stats-grid">
    <article class="card stat-card"><span class="stat-label">Đơn cần xử lý hôm nay</span><div class="stat-number">08</div><span class="stat-detail"><b class="attention">● 3</b> đơn đang chờ cọc</span></article>
    <article class="card stat-card"><span class="stat-label">Lịch giao / nhận</span><div class="stat-number">06</div><span class="stat-detail"><b class="up">↑ 2</b> so với hôm qua</span></article>
    <article class="card stat-card"><span class="stat-label">Tiền cần hoàn</span><div class="stat-number">1.250.000đ</div><span class="stat-detail"><b class="attention">● 2</b> đơn chờ manager duyệt</span></article>
    <article class="card stat-card"><span class="stat-label">Tỷ lệ đồ đang bận</span><div class="stat-number">67%</div><span class="stat-detail"><b class="up">↑ 8%</b> so với tuần trước</span></article>
  </div>
  <div class="grid two-column" style="margin-top:16px">
    <section class="card"><div class="card-heading"><h2>Việc cần làm hôm nay</h2><a href="#" data-view="orders">Xem tất cả</a></div><div class="task-list">
      <div class="task"><i class="task-dot"></i><div class="task-copy"><strong>Xác nhận cọc còn lại — Ngọc Anh</strong><span>AR-240914-018 · Afrodille gấm S</span></div><span class="task-time">Trước 11:30</span></div>
      <div class="task"><i class="task-dot gold"></i><div class="task-copy"><strong>Giao đồ cho Thu Hà</strong><span>AR-240913-014 · Tài xế đến lấy lúc 13:00</span></div><span class="task-time">13:00</span></div>
      <div class="task"><i class="task-dot green"></i><div class="task-copy"><strong>Nhận trả & kiểm tra Selene satin</strong><span>AR-240912-009 · Cần gửi đề nghị hoàn</span></div><span class="task-time">16:30</span></div>
      <div class="task"><i class="task-dot"></i><div class="task-copy"><strong>Hold AF-GAM-S-02 sắp hết hạn</strong><span>Khách chưa gửi form OTP</span></div><span class="task-time">18:10</span></div>
    </div></section>
    <section class="card"><div class="card-heading"><h2>Lịch tuần này</h2><a href="#" data-view="schedule">Mở lịch</a></div><div class="week-labels"><span>T2</span><span>T3</span><span>T4</span><span>T5</span><span>T6</span><span>T7</span><span>CN</span></div><div class="mini-calendar">${[9,10,11,12,13,14,15].map(day => `<span class="mini-day ${day === 14 ? 'today' : ''} ${[11,12,15].includes(day) ? 'rental' : ''}">${day}</span>`).join('')}</div><div class="card-body"><div class="calendar-legend"><span><i class="legend-dot booked"></i>Đang thuê</span><span><i class="legend-dot hold"></i>Giữ slot</span><span><i class="legend-dot clean"></i>Vệ sinh</span></div></div></section>
  </div>
  <section class="card" style="margin-top:16px"><div class="card-heading"><h2>Đơn mới từ form khách</h2><a href="#" data-view="orders">Xem tất cả đơn</a></div>${ordersTable(orders.slice(0, 3))}</section>`
}

function scheduleView() {
  const days = [['T2','09'],['T3','10'],['T4','11'],['T5','12'],['T6','13'],['T7','14'],['CN','15']]
  const product = (name, code, tone = '') => `<div class="cal-product"><span class="product-thumb ${tone}">♧</span><div class="product-name"><strong>${name}</strong><span>${code}</span></div></div>`
  const cell = (text = '', type = '') => `<div class="cal-cell ${type}">${text ? `<span class="cell-text">${text}</span>` : ''}</div>`
  return `${pageHeader('Lịch tồn kho', 'Theo dõi lịch bận, giữ slot và thời gian vệ sinh của từng mã đồ.', `<button class="button secondary">‹ Tuần trước</button><button class="button secondary">Tuần sau ›</button><button class="button" data-view="issue-form">＋ Cấp mã</button>`)}
  <section class="card"><div class="card-body" style="padding-top:19px"><div class="schedule-toolbar"><div class="week-switcher"><button class="button secondary small">‹</button><span class="week-range">09 – 15 Tháng 9, 2024</span><button class="button secondary small">›</button></div><div class="segmented" style="width:190px"><button class="active">Tuần</button><button>Tháng</button></div></div><div class="calendar-wrap"><div class="rental-calendar">
    <div class="cal-head">Sản phẩm / mã</div>${days.map(([d,n]) => `<div class="cal-head ${n === '14' ? 'today-head' : ''}"><span>${d}</span><strong>${n}</strong></div>`).join('')}
    ${product('Afrodille gấm · S', 'AF-GAM-S-01')}${cell()}${cell('Thu Hà','booked')}${cell('Thu Hà','booked')}${cell('Thu Hà','booked')}${cell('Vệ sinh','clean')}${cell()}${cell()}
    ${product('Afrodille gấm · S', 'AF-GAM-S-02')}${cell()}${cell()}${cell()}${cell()}${cell('Ngọc Anh','hold')}${cell('Ngọc Anh','hold')}${cell()}
    ${product('Afrodille gấm · L', 'AF-GAM-L-01', 'dark')}${cell('Lan Phương','booked')}${cell('Lan Phương','booked')}${cell('Lan Phương','booked')}${cell('Vệ sinh','clean')}${cell()}${cell()}${cell()}
    ${product('Afrodille trắng · S', 'AF-TRA-S-01', 'blue')}${cell()}${cell()}${cell()}${cell()}${cell('Mai Vy','booked')}${cell('Mai Vy','booked')}${cell('Vệ sinh','clean')}
    ${product('Selene satin · M', 'SE-SAT-M-03')}${cell('Bảo Trâm','booked')}${cell('Bảo Trâm','booked')}${cell('Bảo Trâm','booked')}${cell('Vệ sinh','clean')}${cell()}${cell()}${cell()}
  </div></div><div class="calendar-legend"><span><i class="legend-dot booked"></i>Đơn đã xác nhận / đang thuê</span><span><i class="legend-dot hold"></i>Đã giữ slot, chờ khách gửi form</span><span><i class="legend-dot clean"></i>Không thể cho thuê trong thời gian vệ sinh</span></div></div></section>
  <div class="grid three-column" style="margin-top:16px"><article class="card"><div class="card-heading"><h3>Hướng dẫn thao tác</h3></div><div class="card-body"><p style="margin:0;color:var(--muted);font-size:12px;line-height:1.65">Chọn chính xác mã đồ trống trước khi gửi form. Hold chỉ khóa lịch tạm và sẽ tự hết hạn.</p></div></article><article class="card"><div class="card-heading"><h3>AF-GAM-S-02</h3>${badge('Hold · 01:42:18', 'hold')}</div><div class="card-body"><p style="margin:0 0 12px;color:var(--muted);font-size:12px">Ngọc Anh · 14–17 Thg 9</p><button class="button secondary small" data-view="issue-form">Mở thông tin cấp mã</button></div></article><article class="card"><div class="card-heading"><h3>Cảnh báo xung đột</h3></div><div class="card-body"><p style="margin:0;color:var(--muted);font-size:12px;line-height:1.65">Không có xung đột. Hệ thống sẽ kiểm tra lại một lần nữa khi khách gửi form.</p></div></article></div>`
}

function scheduleScaleView() {
  const stockRow = (name, meta, available, renting, hold, cleaning, open = false) => `<div class="stock-summary-row ${open ? 'expanded' : ''}">
    <div class="stock-summary-name"><span class="product-thumb">♧</span><div><strong>${name}</strong><span>${meta}</span></div></div>
    <div class="availability-numbers"><span><b>${available}</b> trống</span><span><b>${renting}</b> thuê</span><span><b>${hold}</b> hold</span><span><b>${cleaning}</b> vệ sinh</span></div>
    <button class="button secondary small" data-action="toggle-schedule-details">${open ? 'Thu gọn' : 'Xem mã'}</button>
  </div>`
  const timelineRow = (code, name, cells) => `<div class="asset-timeline-row"><div class="asset-meta"><strong>${code}</strong><span>${name}</span></div><div class="asset-days">${cells.map(cell => `<span class="asset-day ${cell[1] || ''}">${cell[0] || ''}</span>`).join('')}</div></div>`
  return `${pageHeader('Lịch tồn kho', 'Tra cứu mã trống trước; chỉ mở timeline cho mẫu hoặc mã đang cần thao tác.', `<button class="button secondary" data-action="show-operations">◷ Việc hôm nay</button><button class="button" data-view="issue-form">＋ Cấp mã & form</button>`)}
  <section class="availability-finder card"><div class="finder-heading"><div><span class="finder-kicker">TRA CỨU AVAILABILITY</span><h2>Tìm mã trống cho khách</h2><p>Không tải toàn bộ kho. Kết quả chỉ trả về mã có thể thuê trong khoảng thời gian chọn.</p></div><span class="finder-count">1.284 mã đang hoạt động</span></div><div class="finder-controls"><div class="finder-field finder-search"><label>Tên mẫu hoặc mã đồ</label><input class="text-input" value="Afrodille gấm" /></div><div class="finder-field"><label>Size</label><select class="text-input"><option>Size S</option><option>Size L</option></select></div><div class="finder-field"><label>Nhận đồ</label><input class="text-input" value="14/09 · 14:00" /></div><div class="finder-field"><label>Trả đồ</label><input class="text-input" value="17/09 · 14:00" /></div><button class="button finder-button" data-action="search-availability">Tìm mã trống</button></div><div class="quick-filters"><button class="quick-filter active">Tất cả kho</button><button class="quick-filter">Chỉ mã trống</button><button class="quick-filter">Có hoạt động hôm nay <b>18</b></button><button class="quick-filter">Hold sắp hết hạn <b>2</b></button><button class="quick-filter">Đang vệ sinh <b>7</b></button></div></section>
  <section class="card availability-results"><div class="card-heading"><div><h2>Kết quả: Afrodille gấm · Size S</h2><p style="margin:4px 0 0;color:var(--muted);font-size:11px">Có 12 mã trống toàn bộ 14:00, 14/09 → 14:00, 17/09</p></div><button class="inline-link">Xem 12 mã</button></div><div class="available-asset-list"><div class="available-asset"><span class="asset-status-dot"></span><div><strong>AF-GAM-S-03</strong><span>Trống cả kỳ thuê · Lần vệ sinh gần nhất: 12/09</span></div>${badge('Có thể chọn', 'available')}<button class="button small" data-action="select-asset">Chọn mã</button></div><div class="available-asset"><span class="asset-status-dot"></span><div><strong>AF-GAM-S-05</strong><span>Trống cả kỳ thuê · Lần vệ sinh gần nhất: 10/09</span></div>${badge('Có thể chọn', 'available')}<button class="button small" data-action="select-asset">Chọn mã</button></div><div class="available-asset"><span class="asset-status-dot"></span><div><strong>AF-GAM-S-09</strong><span>Trống cả kỳ thuê · Lần vệ sinh gần nhất: 13/09</span></div>${badge('Có thể chọn', 'available')}<button class="button small" data-action="select-asset">Chọn mã</button></div></div><div class="result-footer"><span>Không hiện các mã bận, hold hoặc cleaning để giảm nhiễu.</span><button class="inline-link">Xem các mã không khả dụng</button></div></section>
  <section class="card stock-summary"><div class="card-heading"><div><h2>Tổng quan kho theo mẫu / size</h2><p style="margin:4px 0 0;color:var(--muted);font-size:11px">Mặc định hiển thị aggregate. Mở một nhóm để xem timeline của các mã có hoạt động.</p></div><div class="segmented compact"><button class="active">Theo mẫu / size</button><button>Theo mã đồ</button></div></div><div class="stock-summary-head"><span>Mẫu / size</span><span>Tình trạng hiện tại</span><span></span></div>${stockRow('Afrodille gấm · Size S', '19 mã vật lý', '12', '4', '1', '2', state.scheduleExpanded)}${state.scheduleExpanded ? `<div class="expanded-timeline"><div class="timeline-hint"><span>Chỉ hiện 3/19 mã đang có lịch trong tuần này</span><button class="inline-link" data-action="open-all-assets">Xem tất cả mã</button></div><div class="asset-timeline-head"><span>Mã đồ</span><div>${['T2 09','T3 10','T4 11','T5 12','T6 13','T7 14','CN 15'].map(day => `<span>${day}</span>`).join('')}</div></div>${timelineRow('AF-GAM-S-01','Đang thuê · Thu Hà',[[''],['Thu Hà','booked'],['Thu Hà','booked'],['Thu Hà','booked'],['Vệ sinh','clean'],[''],['']])}${timelineRow('AF-GAM-S-02','Hold · Ngọc Anh',[[''],[''],[''],[''],['Ngọc Anh','hold'],['Ngọc Anh','hold'],['']])}${timelineRow('AF-GAM-S-06','Đang vệ sinh',[['Vệ sinh','clean'],[''],[''],[''],[''],[''],['']])}</div>` : ''}${stockRow('Afrodille gấm · Size L', '12 mã vật lý', '8', '3', '0', '1')}${stockRow('Afrodille trắng · Size S', '8 mã vật lý', '5', '2', '1', '0')}${stockRow('Selene satin · Size M', '17 mã vật lý', '10', '5', '0', '2')}</section>
  <div class="grid three-column" style="margin-top:16px"><article class="card"><div class="card-heading"><h3>Lịch vận hành hôm nay</h3>${badge('18 việc', 'pending')}</div><div class="card-body"><p style="margin:0;color:var(--muted);font-size:12px;line-height:1.65">Danh sách riêng cho giao, nhận, cleaning và hold sắp hết hạn — không lẫn với toàn bộ kho.</p><button class="inline-link" style="margin-top:12px" data-action="show-operations">Mở danh sách việc hôm nay</button></div></article><article class="card"><div class="card-heading"><h3>Quy tắc hiển thị</h3></div><div class="card-body"><p style="margin:0;color:var(--muted);font-size:12px;line-height:1.65">Lịch tháng chỉ hiển thị số lượng theo mẫu/size. Timeline từng mã chỉ mở sau khi người dùng lọc.</p></div></article><article class="card"><div class="card-heading"><h3>Hiệu năng</h3></div><div class="card-body"><p style="margin:0;color:var(--muted);font-size:12px;line-height:1.65">Khi build thật: search server-side, cursor pagination và virtual scroll cho nhóm nhiều mã.</p></div></article></div>`
}

function issueFormView() {
  if (state.issueComplete) return issueCompleteView()
  return `${pageHeader('Cấp mã & form cho khách', 'Staff khóa lịch tạm, cấp mã đồ và link form. Đơn sẽ do hệ thống tự tạo khi khách submit.', `<button class="button secondary" data-view="schedule">← Quay lại lịch</button>`)}
  <section class="card form-card"><div class="form-section"><div class="stepper"><div class="step done"><span class="step-number">✓</span> Kiểm tra lịch</div><div class="step active"><span class="step-number">2</span> Cấp mã & form</div><div class="step"><span class="step-number">3</span> Khách tự gửi form</div><div class="step"><span class="step-number">4</span> Hệ thống tạo đơn</div></div><h2>Thông tin slot đã giữ</h2><p>Thông tin này chỉ để cấp mã/form, không tạo đơn thủ công.</p><div class="form-grid"><div class="field"><label>Khách hàng</label><input class="text-input" value="Ngọc Anh · 090 812 34 56" /></div><div class="field"><label>Thời gian thuê</label><input class="text-input" value="14:00, 14/09 → 14:00, 17/09" /></div></div></div>
  <div class="form-section"><h2>Mã đồ gửi cho khách</h2><p>Hệ thống chỉ nhận các mã dưới đây trong form OTP, giúp tránh khách điền sai hoặc spam.</p><div class="choice-list"><div class="product-chip"><div class="chip-left"><span class="product-thumb">♧</span><div><strong>Afrodille gấm · Size S</strong><span>Mã vật lý: AF-GAM-S-02 · đang được hold</span></div></div>${badge('Còn trống', 'available')}</div><div class="product-chip"><div class="chip-left"><span class="product-thumb blue">⌁</span><div><strong>Khuyên ngọc trai</strong><span>Mã phụ kiện: ACC-PEARL-08</span></div></div>${badge('Còn trống', 'available')}</div></div></div>
  <div class="form-section"><div class="form-grid"><div><h2>Gói cọc khách chọn</h2><p>Giá thuê sẽ được khấu trừ trực tiếp từ cọc khi khách trả đồ.</p><div class="choice-list"><label class="choice selected"><input type="radio" checked name="deposit" /><span><strong>Cọc 50% + CCCD</strong><span>Staff kiểm tra CCCD qua Instagram trước khi xác nhận đơn; hệ thống không lưu CCCD.</span></span></label><label class="choice"><input type="radio" name="deposit" /><span><strong>Cọc 100%</strong><span>Không yêu cầu CCCD.</span></span></label></div></div><div class="money-summary"><div class="summary-line"><span>Giá trị váy</span>${money('1.200.000đ')}</div><div class="summary-line"><span>Cọc mục tiêu (50%)</span>${money('600.000đ')}</div><div class="summary-line"><span>Đã giữ slot</span>${money('− 100.000đ')}</div><div class="summary-line total"><span>Cọc còn lại cần thu</span>${money('500.000đ')}</div></div></div></div>
  <div class="form-actions"><span>Hold AF-GAM-S-02 hết hạn lúc 18:10 hôm nay.</span><button class="button" data-action="generate-code">Tạo OTP & link form →</button></div></section>`
}

function issueCompleteView() {
  return `${pageHeader('Đã sẵn sàng gửi form', 'OTP và link được khóa theo hold. Khách tự điền form; hệ thống sẽ tự tạo đơn sau khi xác thực.', `<button class="button secondary" data-action="restart-issue">＋ Cấp form khác</button>`)}
  <section class="card form-card"><div class="code-result"><div><span class="success-icon">✓</span><h2>Đã tạo link form cho Ngọc Anh</h2><p>Gửi phần bên dưới cho khách qua Inbox. Mã và OTP chỉ dùng được một lần trước 18:10 hôm nay.</p><div class="code-box"><div class="code-row"><span>Mã váy</span><strong>AF-GAM-S-02</strong></div><div class="code-row"><span>Mã phụ kiện</span><strong>ACC-PEARL-08</strong></div><div class="code-row"><span>OTP</span><strong>482 917</strong></div><div class="code-row"><span>Tiền cọc còn lại</span><strong>500.000đ</strong></div></div><div class="link-box"><span>aurarental.vn/form/hold/6H82AQ</span><button class="inline-link" data-action="copy-link">Sao chép</button></div><div style="display:flex;gap:8px;margin-top:14px"><button class="button" data-action="copy-link">Sao chép nội dung gửi khách</button><button class="button secondary" data-action="preview-form">Xem form khách</button></div></div><div><div class="qr-placeholder" aria-label="QR code mockup"></div><p style="text-align:center;margin-top:9px">QR form · mockup</p></div></div>
  <div class="form-section" style="background:#fffcfa"><h2>Điều gì xảy ra tiếp theo?</h2><div class="grid three-column"><div><strong style="font-size:12px">1. Khách tự điền</strong><p style="color:var(--muted);font-size:11px;line-height:1.55">Thông tin liên hệ, lịch thuê và mã đồ. Không tải CCCD lên form.</p></div><div><strong style="font-size:12px">2. Hệ thống kiểm tra</strong><p style="color:var(--muted);font-size:11px;line-height:1.55">OTP, mã đồ, reservation và availability được kiểm tra trong một lần submit.</p></div><div><strong style="font-size:12px">3. Staff xác nhận CCCD</strong><p style="color:var(--muted);font-size:11px;line-height:1.55">Nếu dùng gói 50%, staff kiểm tra CCCD qua Instagram rồi tick xác nhận.</p></div></div></div></section>`
}

function ordersView() {
  return `${pageHeader('Đơn hàng', 'Danh sách đơn được hệ thống tự tạo từ form khách gửi.', `<button class="button secondary" data-view="schedule">▦ Lịch tồn kho</button><button class="button" data-view="issue-form">＋ Cấp mã & form</button>`)}
  <div class="filter-row"><input class="search-input" placeholder="Tìm mã đơn, khách hàng, mã váy..." /><select class="select-input"><option>Tất cả trạng thái</option><option>Chờ cọc</option><option>Đang thuê</option><option>Chờ hoàn</option></select><select class="select-input"><option>14 Thg 9</option><option>Tuần này</option></select><span class="filter-spacer"></span><button class="button secondary small">⇩ Xuất file</button></div>
  <section class="card">${ordersTable()}</section>
  <p style="margin:12px 2px;color:var(--muted);font-size:11px">Mẹo: Bấm vào đơn để xem timeline, tiền cọc và các bước staff cần xử lý tiếp theo.</p>`
}

function orderDetailView() {
  const order = orders.find(item => item.id === state.selectedOrder) || orders[0]
  return `${pageHeader(`Đơn ${order.id}`, 'Được hệ thống tạo lúc 09:42 sau khi khách Ngọc Anh gửi form OTP.', `<button class="button secondary" data-view="orders">← Danh sách đơn</button><button class="button" data-action="confirm-deposit">✓ Xác nhận cọc 500.000đ</button>`)}
  <div class="detail-layout"><div class="grid"><section class="card"><div class="card-heading"><div class="detail-title">${avatar(order.initials)}<div><h1>${order.name}</h1><p>090 812 34 56 · Khách mới</p></div></div>${badge(order.status, order.tone)}</div><div class="card-body"><div class="info-list"><div class="info-line"><span>Thời gian thuê</span><strong>14:00, 14/09 → 14:00, 17/09</strong></div><div class="info-line"><span>Giao / nhận</span><strong>Ship 2 chiều · Quận 3, TP.HCM</strong></div><div class="info-line"><span>Staff phụ trách</span><strong>Minh Lan</strong></div></div></div></section>
  <section class="card"><div class="card-heading"><h2>Sản phẩm khách đã xác nhận</h2><button class="inline-link">Xem lịch</button></div><div class="card-body"><div class="product-row"><span class="product-thumb">♧</span><div><strong>Afrodille gấm · Size S</strong><span>AF-GAM-S-02 · 84 × 64–66 × 88</span></div><div class="price">320.000đ<br/><span>Gói 3 ngày</span></div></div><div class="product-row"><span class="product-thumb blue">⌁</span><div><strong>Khuyên ngọc trai</strong><span>ACC-PEARL-08</span></div><div class="price">Miễn phí<br/><span>Đi kèm váy</span></div></div></div></section>
  <section class="card"><div class="card-heading"><h2>Timeline đơn hàng</h2></div><div class="card-body"><div class="timeline"><div class="timeline-item"><i class="timeline-dot"></i><strong>Khách gửi form OTP thành công</strong><span>09:42 · Hệ thống kiểm tra mã, hold và lịch trống</span></div><div class="timeline-item"><i class="timeline-dot"></i><strong>Đơn được tạo tự động</strong><span>09:42 · Hold chuyển thành lịch thuê đã khóa</span></div><div class="timeline-item"><i class="timeline-dot pending"></i><strong>Chờ xác nhận cọc còn lại</strong><span>Cần nhận 500.000đ trước giờ giao đồ</span></div></div></div></section></div>
  <aside class="grid"><section class="card"><div class="card-heading"><h2>Cọc & thanh toán</h2></div><div class="card-body"><div class="money-summary"><div class="summary-line"><span>Cọc mục tiêu (50%)</span>${money('600.000đ')}</div><div class="summary-line"><span>Đã nhận giữ slot</span>${money('100.000đ')}</div><div class="summary-line total"><span>Còn cần nhận</span>${money('500.000đ')}</div></div><div style="margin-top:14px" class="payment-progress"><span></span><i></i></div><div class="progress-caption"><span>Đã nhận 100.000đ</span><span>17%</span></div><button class="button full" style="margin-top:16px" data-action="confirm-deposit">Xác nhận đã nhận 500.000đ</button><p style="margin:10px 0 0;color:var(--muted);font-size:10px;line-height:1.5">Phí thuê 320.000đ sẽ được khấu trừ từ tiền cọc khi khách trả đồ.</p></div></section><section class="card"><div class="card-heading"><h2>Thông tin form</h2></div><div class="card-body"><div class="info-list"><div class="info-line"><span>Gói cọc</span><strong>50% + CCCD</strong></div><div class="info-line"><span>CCCD</span><strong style="color:var(--green)">Đã tải lên ✓</strong></div><div class="info-line"><span>OTP</span><strong>Đã sử dụng</strong></div></div><button class="button secondary full" style="margin-top:13px">Xem ảnh CCCD</button></div></section></aside></div>`
}

function orderDetailWithIdentityView() {
  const verification = state.idVerified
    ? `<div class="info-line"><span>CCCD qua Instagram</span><strong style="color:var(--green)">Đã staff xác nhận ✓</strong></div><div class="info-line"><span>Người xác nhận</span><strong>Minh Lan · 10:20</strong></div>`
    : `<div class="info-line"><span>CCCD qua Instagram</span><strong style="color:var(--gold)">Cần staff kiểm tra</strong></div>`
  const verificationAction = state.idVerified
    ? `<div class="form-message" style="color:#28654f;background:var(--green-pale)">Hệ thống không lưu ảnh hoặc số CCCD.</div>`
    : `<button class="button secondary full" style="margin-top:13px" data-action="verify-identity">✓ Đã kiểm tra CCCD qua Instagram</button><p style="margin:9px 0 0;color:var(--muted);font-size:10px;line-height:1.45">Không tải ảnh hoặc nhập số CCCD vào hệ thống.</p>`
  return orderDetailView().replace(
    /<div class="info-line"><span>CCCD<\/span><strong style="color:var\(--green\)">Đã tải lên ✓<\/strong><\/div><div class="info-line"><span>OTP<\/span><strong>Đã sử dụng<\/strong><\/div><\/div><button class="button secondary full" style="margin-top:13px">Xem ảnh CCCD<\/button>/,
    `${verification}<div class="info-line"><span>OTP</span><strong>Đã sử dụng</strong></div></div>${verificationAction}`,
  )
}

function returnsView() {
  return compactReturnsView()
}

function productsView() {
  const branch = currentBranch()
  const productCard = (name, detail, count, price, tone='') => `<button class="card stock-card" data-view="product-detail"><div class="stock-image ${tone}">♧</div><div class="stock-info"><h3>${name}</h3><p>${detail}</p><div class="stock-meta"><span><strong>${count}</strong> mã đang hoạt động</span><span>${price}</span></div></div></button>`
  const priceFrom = formatVnd(branch.packages[0].price)
  return `${pageHeader('Kho sản phẩm', `Tồn kho và giá đang áp dụng tại ${branch.city}.`, `<button class="button secondary" data-action="import-stock">Nhập kho</button><button class="button" data-view="product-add">＋ Thêm sản phẩm</button>`)}
  ${branchScopeBar()}<button class="button mobile-catalog-add" data-view="product-add">＋ Thêm sản phẩm tại ${branch.code}</button><div class="filter-row"><input class="search-input" placeholder="Tìm tên, mã sản phẩm..." /><select class="select-input"><option>Tất cả danh mục</option><option>Váy</option><option>Phụ kiện</option></select><select class="select-input"><option>Đang hoạt động</option><option>Đang vệ sinh</option><option>Bảo trì</option></select></div><section class="stock-grid">${productCard('Afrodille gấm','S · L · Váy dạ hội', branch.available,`Từ ${priceFrom}`)}${productCard('Afrodille trắng','S fit M · Váy dạ hội', Math.max(2, branch.available - 4),`Từ ${formatVnd(branch.packages[0].price + 50000)}`,'blue')}${productCard('Selene satin','M · Váy dạ hội', Math.max(3, branch.available - 3),`Từ ${formatVnd(branch.packages[0].price + 30000)}`,'black')}${productCard('Luna corset','S · M · Váy dự tiệc', '3',`Từ ${formatVnd(branch.packages[0].price + 40000)}`,'cream')}</section><section class="card" style="margin-top:16px"><div class="card-heading"><h2>Mã cần chú ý · ${branch.code}</h2><a href="#">Xem tất cả</a></div><div class="task-list"><div class="task"><i class="task-dot gold"></i><div class="task-copy"><strong>${branch.code}-AF-GAM-L-01 đang vệ sinh</strong><span>Hoàn thành lúc 02:00 ngày mai</span></div>${badge('Cleaning', 'cleaning')}</div><div class="task"><i class="task-dot"></i><div class="task-copy"><strong>${branch.code}-SE-SAT-M-03 chờ kiểm tra</strong><span>Chỉ hiển thị nghiệp vụ của ${branch.city}</span></div>${badge('Chờ hoàn', 'refund')}</div></div></section>`
}

function productAddView() {
  const branch = currentBranch()
  return `${pageHeader('Thêm sản phẩm', `Khai báo tồn kho và giá cho ${branch.name}.`, `<button class="button secondary" data-view="products">Hủy</button><button class="button" data-action="save-product">Lưu sản phẩm</button>`)}
  ${branchScopeBar()}
  <div class="product-editor-layout"><section class="card"><div class="card-heading"><div><h2>Thông tin mẫu</h2><p>Mỗi mẫu có thể có nhiều size và mã đồ riêng.</p></div></div><div class="form-section"><div class="upload-area"><span>♧</span><strong>Thêm ảnh sản phẩm</strong><small>Kéo ảnh vào đây hoặc chọn từ thiết bị · PNG, JPG tối đa 10MB</small><button class="button secondary small" data-action="upload-product-image">Chọn ảnh</button></div><div class="form-grid"><div class="field"><label>Tên sản phẩm <b>*</b></label><input class="text-input" placeholder="Ví dụ: Afrodille gấm" /></div><div class="field"><label>Danh mục <b>*</b></label><select class="text-input"><option>Váy dạ hội</option><option>Váy dự tiệc</option><option>Phụ kiện</option></select></div><div class="field"><label>Mã mẫu</label><input class="text-input" placeholder="Tự tạo từ tên, ví dụ AF-GAM" /></div><div class="field"><label>Giá trị thay thế <b>*</b></label><input class="text-input" value="1.200.000đ" inputmode="numeric" /></div></div><div class="field"><label>Mô tả / lưu ý cho staff</label><textarea class="text-input" placeholder="Chất liệu, cách bảo quản hoặc lưu ý khi tư vấn..." style="height:76px;padding-top:10px"></textarea></div></div></section><aside class="grid"><section class="card"><div class="card-heading"><h2>Thiết lập vận hành</h2></div><div class="card-body"><div class="field"><label>Thời gian vệ sinh sau trả</label><select class="text-input"><option>12 giờ</option><option>24 giờ</option><option>48 giờ</option></select></div><div class="field"><label>Trạng thái khi tạo</label><select class="text-input"><option>Đang hoạt động</option><option>Tạm ẩn</option></select></div><p class="side-note">Giá thuê được thiết lập theo từng size ở phần bên dưới.</p></div></section></aside></div>
  <section class="card variant-editor"><div class="card-heading"><div><h2>Size, giá thuê & mã vật lý</h2><p>Bảng giá và mã kho bên dưới chỉ thuộc ${branch.city}.</p></div><button class="button secondary small" data-action="add-variant">＋ Thêm size</button></div><div class="variant-form"><div class="form-grid three"><div class="field"><label>Size <b>*</b></label><input class="text-input" value="S" /></div><div class="field"><label>Số đo</label><input class="text-input" value="84 × 64–66 × 88" /></div><div class="field"><label>SKU</label><input class="text-input" value="AF-GAM-S" /></div></div><div class="price-grid">${branch.packages.map(item => `<div class="field"><label>Giá thuê ${item.label}</label><input class="text-input" value="${formatVnd(item.price)}" /></div>`).join('')}</div><div class="asset-code-entry"><div><strong>Mã đồ vật lý · ${branch.code}</strong><span>Mỗi mã thuộc đúng một chi nhánh.</span></div><div class="asset-chips"><span>${branch.assets[0]} <button aria-label="Xóa mã">×</button></span><span>${branch.assets[1]} <button aria-label="Xóa mã">×</button></span><button class="add-code" data-action="add-asset-code">＋ Thêm mã</button></div></div></div></section><div class="mobile-product-actions"><button class="button secondary" data-view="products">Hủy</button><button class="button" data-action="save-product">Lưu tại ${branch.code}</button></div>`
}

function productDetailView() {
  const branch = currentBranch()
  const assets = [[branch.assets[0],'Đang thuê','renting','14–17 Thg 9 · Thu Hà'],[branch.assets[1],'Giữ slot','pending','14–17 Thg 9 · Ngọc Anh'],[branch.assets[2],'Có sẵn','available','Đã vệ sinh 12 Thg 9'],[`${branch.code}-AF-GAM-L-01`,'Cleaning','cleaning','Sẵn sàng lúc 02:00 ngày mai']]
  const priceCells = offset => branch.packages.map(item => `<span>${formatVnd(item.price + offset)}</span>`).join('')
  return `${pageHeader('Afrodille gấm', `Mã mẫu AF-GAM · ${branch.name}`, `<button class="button secondary" data-view="products">← Kho sản phẩm</button><button class="button" data-action="edit-product">Chỉnh sửa</button>`)}
  ${branchScopeBar()}<div class="product-detail-hero"><section class="card product-showcase"><div class="product-hero-image">♧</div><div class="product-hero-copy"><div>${badge('Đang hoạt động', 'available')}</div><h2>Afrodille gấm</h2><p>Váy dạ hội · Gấm hoa · Cần vệ sinh 12 giờ sau khi trả.</p><div class="product-kpis"><span><b>${branch.available}</b> mã tại ${branch.code}</span><span><b>2</b> size</span><span><b>${formatVnd(branch.packages[0].price)}</b> giá từ</span></div></div></section><aside class="card detail-note"><div class="card-heading"><h2>Giá trị & chính sách</h2></div><div class="card-body"><div class="info-list"><div class="info-line"><span>Giá trị thay thế</span><strong>1.200.000đ</strong></div><div class="info-line"><span>Chi nhánh</span><strong>${branch.city}</strong></div><div class="info-line"><span>Thời gian vệ sinh</span><strong>12 giờ</strong></div></div></div></aside></div>
  <section class="card" style="margin-top:16px"><div class="card-heading"><div><h2>Bảng giá · ${branch.code}</h2><p>Chỉ áp dụng cho reservation mới tại ${branch.city}.</p></div><button class="button secondary small" data-action="add-variant">＋ Thêm size</button></div><div class="variant-table"><div class="variant-row variant-head"><span>Size / số đo</span>${branch.packages.map(item => `<span>${item.label}</span>`).join('')}<span>Mã vật lý</span></div><div class="variant-row"><div><strong>S</strong><small>84 × 64–66 × 88</small></div>${priceCells(0)}<button class="inline-link" data-action="show-size-assets">${branch.assets.length} mã ›</button></div><div class="variant-row"><div><strong>L</strong><small>92 × 72–74 × 96</small></div>${priceCells(20000)}<button class="inline-link" data-action="show-size-assets">1 mã ›</button></div></div></section>
  <section class="card" style="margin-top:16px"><div class="card-heading"><div><h2>Mã đồ vật lý</h2><p>Theo dõi tình trạng từng chiếc để tránh double-book.</p></div><button class="button secondary small" data-action="add-asset-code">＋ Thêm mã</button></div><div class="asset-detail-list">${assets.map(asset => `<div class="asset-detail-row"><span class="product-thumb">♧</span><div><strong>${asset[0]}</strong><small>${asset[3]}</small></div>${badge(asset[1], asset[2])}<button class="inline-link" data-action="view-asset-history">Lịch sử</button></div>`).join('')}</div></section>`
}

function placeholderView(title) { return `${pageHeader(title, 'Module này được để làm khung trong bản mockup hiện tại.')}<section class="card empty-view"><div><div class="empty-illustration">◔</div><h2>Đang chờ thiết kế chi tiết</h2><p>Ở bước phát triển tiếp theo, màn hình này sẽ dùng dữ liệu thật từ hệ thống Aura Rental.</p></div></section>` }

function compactDashboardView() {
  const branch = currentBranch()
  const branchOrders = orders.filter(order => order.branch === branch.id)
  const nextOrder = branchOrders[0]
  return `${pageHeader('Hôm nay', `Chủ Nhật, 21 Thg 9 · ${branch.city}`)}
  ${branchScopeBar()}
  <div class="quick-actions"><button class="quick-action" data-view="orders"><b>${branchOrders.filter(order => order.tone === 'pending').length}</b><span>Chờ cọc</span></button><button class="quick-action" data-view="schedule"><b>${branch.holds}</b><span>Giữ chỗ</span></button><button class="quick-action" data-view="returns"><b>1</b><span>Trả đồ</span></button></div>
  <section class="card next-job"><span class="eyebrow">VIỆC TIẾP THEO · ${branch.code}</span><h2>Xác nhận cọc · ${nextOrder.name}</h2><p>${nextOrder.deposit} · ${nextOrder.item}</p><button class="button full" data-view="orders">Mở đơn</button></section>
  <section class="card compact-list"><div class="card-heading"><h2>Hôm nay · ${branch.city}</h2></div>${branchOrders.map((order, index) => `<button class="compact-row" data-view="orders"><i class="task-dot ${index ? 'gold' : ''}"></i><span><strong>${order.name}</strong><small>${order.status} · ${order.item}</small></span><b>${order.deposit.replace('.000đ','k')}</b><em>›</em></button>`).join('')}<button class="compact-row" data-view="returns"><i class="task-dot green"></i><span><strong>${state.branchId === 'saigon' ? 'Uyên Nhi' : 'Bảo Trâm'}</strong><small>Nhận trả lúc 16:30</small></span><b>Trả</b><em>›</em></button></section>
  <button class="soft-action" data-view="issue-form">＋ Tạo giữ chỗ mới</button>`
}

function compactScheduleView() {
  const branch = currentBranch()
  return `${pageHeader('Tìm đồ trống', `Tồn kho và giá tại ${branch.city}`)}
  ${branchScopeBar()}
  <section class="card compact-search"><div class="field"><label>Váy / mã</label><input class="text-input" value="Afrodille gấm" /></div><div class="compact-dates"><div class="field"><label>Nhận</label><input class="text-input" value="14/09" /></div><div class="field"><label>Trả</label><input class="text-input" value="17/09" /></div></div><button class="button full" data-action="search-availability">Tìm mã trống</button></section>
  <section class="card compact-list"><div class="card-heading"><div><h2>Afrodille gấm · S</h2><p class="card-subtitle">Chỉ mã thuộc ${branch.code}</p></div>${badge(`${branch.available} mã trống`, 'available')}</div>${branch.assets.map(code => `<button class="asset-pick" data-action="select-asset" data-asset-code="${code}"><span class="asset-status-dot"></span><div><strong>${code}</strong><small>Trống 14–17/09 · ${branch.city}</small></div><b>Chọn</b></button>`).join('')}</section>
  <section class="card branch-price-card"><div class="card-heading"><div><h2>Bảng giá ${branch.city}</h2><p class="card-subtitle">Giá Afrodille gấm · Size S</p></div></div><div class="branch-price-grid">${branch.packages.map(item => `<div><span>${item.label}</span><strong>${formatVnd(item.price)}</strong></div>`).join('')}</div></section>
  <section class="card compact-list"><div class="card-heading"><h2>Kho ${branch.code}</h2><button class="inline-link" data-action="toggle-schedule-details">${state.scheduleExpanded ? 'Ẩn mã' : 'Xem mã'}</button></div><div class="stock-mini"><span>Afrodille gấm S</span><b>${branch.available} trống · ${branch.renting} thuê</b></div><div class="stock-mini"><span>Afrodille gấm L</span><b>${Math.max(2, branch.available - 3)} trống · 2 thuê</b></div><div class="stock-mini"><span>Selene satin M</span><b>${Math.max(3, branch.available - 2)} trống · 3 thuê</b></div>${state.scheduleExpanded ? `<div class="tiny-timeline">${branch.assets[0]} <span>Kho ${branch.city}</span></div>` : ''}</section>`
}

function compactReservationView() {
  const branch = currentBranch()
  const rentalPackage = currentPackage()
  if (state.issueComplete) return `${pageHeader('Link đã sẵn sàng', `Đơn giữ tại ${branch.city}`)}
  ${branchScopeBar()}<section class="card compact-result"><span class="success-icon">✓</span><h2>Ngọc Anh</h2><div class="code-box"><div class="code-row"><span>Chi nhánh</span><strong>${branch.name}</strong></div><div class="code-row"><span>Mã váy</span><strong>${state.selectedAsset}</strong></div><div class="code-row"><span>Gói thuê</span><strong>${rentalPackage.label} · ${formatVnd(rentalPackage.price)}</strong></div><div class="code-row"><span>OTP</span><strong>482 917</strong></div><div class="code-row"><span>Còn lại</span><strong>500.000đ</strong></div></div><button class="button full" data-action="copy-link">Sao chép link</button><button class="button secondary full" style="margin-top:8px" data-action="preview-form">Xem form khách</button></section>`
  return `${pageHeader('Giữ chỗ', `Giá và tồn kho ${branch.city}`)}
  ${branchScopeBar()}
  <section class="card compact-form"><div class="mini-steps"><b>1</b><i></i><b class="active">2</b><i></i><b>3</b></div><div class="field"><label>Khách</label><input class="text-input" value="Ngọc Anh · 0908 123 456" /></div><div class="field"><label>Đồ đã chọn</label><button class="select-card" data-view="schedule">Afrodille gấm · S <span>${state.selectedAsset}</span></button></div><div class="compact-dates"><div class="field"><label>Nhận</label><input class="text-input" value="14/09 · 14:00" /></div><div class="field"><label>Trả</label><input class="text-input" value="17/09 · 14:00" /></div></div><div class="field"><label>Gói thuê tại ${branch.city}</label><div class="rental-package-options">${branch.packages.map(item => `<button class="${rentalPackage.code === item.code ? 'active' : ''}" data-action="select-rental-package" data-package-code="${item.code}"><span>${item.label}</span><strong>${formatVnd(item.price)}</strong></button>`).join('')}</div></div><div class="compact-total"><span>Giá thuê · ${rentalPackage.label}</span><strong>${formatVnd(rentalPackage.price)}</strong></div><div class="deposit-toggle"><button class="active">Đã cọc 100k</button><button>Cọc đủ</button></div><div class="reservation-balance"><span>Còn cần cọc</span><strong>500.000đ</strong></div><button class="button full" data-action="generate-code">Giữ mã tại ${branch.code} & tạo OTP</button></section>`
}

function compactOrdersView() {
  const branch = currentBranch()
  const branchOrders = orders.filter(order => order.branch === branch.id)
  return `${pageHeader('Đơn hàng', `${branchOrders.length} đơn tại ${branch.city}`)}
  ${branchScopeBar()}
  <div class="simple-filter"><button class="active">Cần xử lý</button><button>Tất cả</button><button data-view="schedule">Tìm đồ</button></div>
  <section class="order-cards">${branchOrders.map(order => `<button class="order-card" data-order="${order.id}"><div><span class="order-code">${order.id}</span>${badge(order.status, order.tone)}</div><strong>${order.name}</strong><span>${order.item}</span><footer><span>${order.date}</span><span>${branch.code} <b>›</b></span></footer></button>`).join('')}</section>`
}

function compactReturnsView() {
  const data = refundData()
  const manager = state.role === 'manager'
  const draft = state.refundStatus === 'draft'
  const pending = state.refundStatus === 'pending'
  const approved = ['approved', 'completed'].includes(state.refundStatus)
  const completed = state.refundStatus === 'completed'
  const adjusting = state.refundStatus === 'adjusting'
  const editable = draft || (manager && (pending || adjusting))
  const status = adjusting ? 'Đang điều chỉnh' : draft ? 'Đang kiểm tra' : pending ? 'Chờ manager duyệt' : completed ? 'Đã hoàn tiền' : 'Đã duyệt · Chờ chuyển khoản'
  const settlementLabel = data.additionalCollection ? 'Cần thu thêm khách' : completed ? 'Đã hoàn lại khách' : 'Tiền hoàn lại khách'
  const settlementAmount = data.additionalCollection || data.refund
  let action = ''
  if (draft) action = manager
    ? `<button class="button full" data-action="approve-refund">✓ Lưu kiểm tra & duyệt hoàn</button><p class="refund-help">Bạn có thể tự kiểm tra và duyệt trực tiếp, không cần chờ staff gửi yêu cầu.</p>`
    : `<button class="button full" data-action="request-refund">Gửi manager duyệt đối soát</button><p class="refund-help">Manager sẽ duyệt số tiền hoàn hoặc khoản cần thu thêm. Ảnh tổng kết được tạo sau khi duyệt.</p>`
  if (pending) action = manager
    ? `<button class="button full" data-action="approve-refund">✓ Duyệt ${data.additionalCollection ? `thu thêm ${formatVnd(data.additionalCollection)}` : `hoàn ${formatVnd(data.refund)}`}</button><button class="button secondary full" data-action="return-refund-review">Yêu cầu staff kiểm tra lại</button><p class="refund-help">Duyệt sẽ tạo ảnh tổng kết; chưa xác nhận đã đối soát tiền với khách.</p>`
    : `<button class="button full" disabled>Đã gửi · Chờ manager duyệt</button><p class="refund-help">Đã gửi bởi Minh Lan. Manager có thể kiểm tra và điều chỉnh trước khi duyệt.</p>`
  if (adjusting) action = manager
    ? `<div class="field"><label for="refund-adjustment-reason">Lý do điều chỉnh <small>(bắt buộc)</small></label><textarea id="refund-adjustment-reason" class="text-input" rows="3" placeholder="Ví dụ: đối chiếu lại tình trạng đồ, bỏ phí xử lý"></textarea></div><button class="button full" data-action="approve-refund">✓ Duyệt lại ${formatVnd(data.refund)}</button><button class="button secondary full" data-action="cancel-refund-adjustment">Hủy điều chỉnh</button><p class="refund-help">Ảnh mới chỉ được tạo sau khi duyệt lại. Nếu đã gửi ảnh cũ, hãy gửi lại ảnh mới cho khách.</p>`
    : `<button class="button full" disabled>Manager đang điều chỉnh</button><p class="refund-help">Chờ manager duyệt lại để lấy ảnh tổng kết mới.</p>`
  if (approved) action = `<div class="refund-approved-note"><span>✓</span><div><strong>Quỳnh Anh đã duyệt ${data.additionalCollection ? `cần thu ${formatVnd(data.additionalCollection)}` : `hoàn ${formatVnd(data.refund)}`}</strong></div></div>${!completed && manager ? `<button class="button full" data-action="confirm-refund-transfer">${data.additionalCollection ? 'Xác nhận đã thu thêm từ khách' : 'Xác nhận đã chuyển khoản hoàn khách'}</button><button class="button secondary full" data-action="adjust-refund">Điều chỉnh & duyệt lại</button>` : ''}<p class="refund-help">${completed ? 'Đã ghi nhận đối soát. Mã đồ chuyển sang Cleaning 12 giờ.' : data.additionalCollection ? 'Xác nhận sau khi đã thu đủ khoản chênh lệch từ khách.' : 'Ảnh hiện ghi “Chờ chuyển khoản”. Xác nhận chuyển khoản sẽ cập nhật ảnh thành “Đã hoàn tiền”.'}</p>`
  return `${pageHeader('Trả đồ & hoàn tiền', `${data.customer} · ${data.id} · ${currentBranch().code}`)}
  ${branchScopeBar()}<div class="refund-demo-toolbar"><span>Xem luồng demo theo vai trò</span><div class="segmented"><button data-refund-role="staff" class="${manager ? '' : 'active'}" aria-pressed="${!manager}">Staff</button><button data-refund-role="manager" class="${manager ? 'active' : ''}" aria-pressed="${manager}">Manager</button></div></div>
  <ol class="refund-steps"><li class="${draft || adjusting ? 'current' : 'done'}"><b>1</b><span>Kiểm tra & đối soát</span></li><li class="${approved ? 'done' : pending ? 'current' : ''}"><b>2</b><span>Manager duyệt</span></li><li class="${approved ? 'current' : ''}"><b>3</b><span>Ảnh gửi khách</span></li></ol>
  <div class="refund-workspace"><div class="refund-operation-column">
    <section class="card refund-inspection"><div class="card-heading"><div><h2>Kiểm tra từng món trả về</h2><p class="card-subtitle">Chọn tình trạng, lưu bằng chứng và hướng xử lý kho.</p></div>${badge(status, approved ? 'available' : 'pending')}</div><div class="card-body inspection-list">
      ${data.items.map(item => `<article class="inspection-card ${item.condition}">
        <div class="inspection-card-head"><span class="inspection-icon">${item.condition === 'good' ? '✓' : item.condition === 'missing' ? '!' : '↯'}</span><div><strong>${item.name}</strong><small>${item.code} · ${item.package}</small></div><span class="inspection-fee">${item.processingFee ? `− ${formatVnd(item.processingFee)}` : 'Không phí'}</span></div>
        ${editable ? `<div class="item-condition-control" role="group" aria-label="Tình trạng ${item.name}"><button class="${item.condition === 'good' ? 'active good' : ''}" data-action="set-item-condition" data-item-id="${item.id}" data-condition="good">Tốt</button><button class="${item.condition === 'damaged' ? 'active damaged' : ''}" data-action="set-item-condition" data-item-id="${item.id}" data-condition="damaged">Hư hỏng</button><button class="${item.condition === 'missing' ? 'active missing' : ''}" data-action="set-item-condition" data-item-id="${item.id}" data-condition="missing">Mất</button></div>` : `<div class="item-condition-result">${item.condition === 'good' ? 'Tình trạng tốt' : item.condition === 'missing' ? 'Đã xác nhận mất' : 'Đã xác nhận hư hỏng'}</div>`}
        ${item.inspection ? `<div class="item-issue-detail"><div><span>Vấn đề</span><strong>${item.inspection.type}</strong></div><div><span>Mức độ</span><strong>${item.inspection.severity}</strong></div><div><span>Kho sau xử lý</span><strong>${item.inspection.outcome}</strong></div><p>${item.inspection.note}</p><footer><span>${item.inspection.photos ? `▣ ${item.inspection.photos} ảnh tình trạng` : '○ Chưa có ảnh — ghi nhận khi khách vắng mặt'}</span><button class="inline-link" data-action="open-item-evidence">${editable ? 'Sửa chi tiết' : 'Xem bằng chứng'}</button></footer></div>` : `<div class="item-good-detail">✓ Đủ mã đồ · chuyển sang vệ sinh 12 giờ sau khi đối soát.</div>`}
      </article>`).join('')}
      <div class="inspection-summary ${data.damageFee ? 'has-issues' : ''}"><span>${data.damageFee ? '!' : '✓'}</span><div><strong>${data.damageNote}</strong><small>${data.damageFee ? `Tổng phí xử lý: ${formatVnd(data.damageFee)} · phí được tính theo từng món.` : 'Đã đối chiếu đủ mã đồ · Không phát sinh phí xử lý.'}</small></div></div>
    </div></section>
    <section class="card refund-calculation"><div class="card-heading"><h2>${manager && pending ? 'Đối soát & duyệt hoàn' : 'Đối soát tiền cọc'}</h2></div><div class="card-body"><div class="info-list"><div class="info-line"><span>Tiền khách cọc ban đầu</span><strong>${formatVnd(data.deposit)}</strong></div><div class="info-line"><span>Tổng giá thuê</span><strong>− ${formatVnd(data.rentalFee)}</strong></div><div class="info-line"><span>Phí xử lý ${data.damageFee ? '(hư hại/mất)' : '(không phát sinh)'}</span><strong>− ${formatVnd(data.damageFee)}</strong></div></div><div class="refund-amount ${data.additionalCollection ? 'additional-amount' : ''}"><span>${settlementLabel}</span><strong>${formatVnd(settlementAmount)}</strong><small>${formatVnd(data.deposit)} − ${formatVnd(data.rentalFee)} − ${formatVnd(data.damageFee)}${data.additionalCollection ? ' · tiền cọc không đủ bù phí' : ''}</small></div><div class="refund-action-stack">${action}</div></div></section>
  </div><aside class="card refund-image-panel"><div class="card-heading"><div><span class="eyebrow">GỬI KHÁCH QUA INSTAGRAM</span><h2>Ảnh tổng kết hoàn tiền</h2></div>${badge(approved ? 'PNG sẵn sàng' : 'Chưa tạo ảnh', approved ? 'available' : 'cancelled')}</div>
    ${approved ? `<div class="refund-image-stage"><img id="refund-receipt-image" alt="Ảnh tổng kết đơn ${data.id}: cọc ${formatVnd(data.deposit)}, giá thuê ${formatVnd(data.rentalFee)}, phí xử lý ${formatVnd(data.damageFee)}, hoàn ${formatVnd(data.refund)}; ${completed ? 'đã hoàn tiền' : 'đã duyệt, chờ chuyển khoản'}" /></div><div class="refund-image-actions"><button class="button full" data-action="copy-refund-image">▣ Sao chép ảnh</button><button class="button secondary full" data-action="download-refund-image">↓ Tải ảnh PNG</button><p>Copy ảnh → mở chat Instagram → dán để gửi khách.</p><small>Nếu thiết bị không hỗ trợ copy ảnh, dùng “Tải ảnh PNG” rồi gửi trong chat.</small></div>` : `<div class="refund-image-empty"><span>▣</span><h3>${adjusting ? 'Ảnh mới chờ duyệt lại' : pending ? 'Đang chờ manager duyệt' : 'Ảnh được tạo sau khi duyệt'}</h3><p>Mã đơn, đồ đã thuê, giá thuê, cọc, phí xử lý và tiền hoàn sẽ nằm trong một ảnh gọn để gửi khách.</p><div class="refund-image-placeholder"><span>Aura Rental</span><i></i><i></i><i></i><b>Tiền hoàn lại khách</b></div></div>`}
  </aside></div>`
}

function render() {
  breadcrumb.textContent = labels[state.view]
  activeBranchName.textContent = currentBranch().name
  const views = { dashboard: compactDashboardView, schedule: compactScheduleView, 'issue-form': compactReservationView, orders: compactOrdersView, returns: compactReturnsView, products: productsView, 'product-add': productAddView, 'product-detail': productDetailView, 'order-detail': orderDetailWithIdentityView, reports: () => placeholderView('Báo cáo'), settings: () => placeholderView('Cấu hình') }
  root.dataset.currentView = state.view
  delete root.dataset.view
  root.innerHTML = (views[state.view] || views.dashboard)()
  const adjustmentReason = document.querySelector('#refund-adjustment-reason')
  if (adjustmentReason) adjustmentReason.value = state.refundAdjustmentReason
  if (state.view === 'returns' && ['approved', 'completed'].includes(state.refundStatus)) {
    document.querySelector('#refund-receipt-image').src = AuraRefundReceipt.create(refundData(), state.refundStatus).toDataURL('image/png')
  }
  document.querySelectorAll('[data-view]').forEach(button => button.classList.toggle('active', button.dataset.view === state.view && ['dashboard','schedule','issue-form','orders','returns','products'].includes(state.view)))
  document.querySelector('#profile-name').textContent = state.role === 'manager' ? 'Quỳnh Anh' : 'Minh Lan'
  document.querySelector('#profile-role').textContent = state.role === 'manager' ? 'Manager' : 'Staff vận hành'
  document.querySelector('.sidebar-user .avatar').textContent = state.role === 'manager' ? 'QA' : 'ML'
}

function showToast(message) {
  const toast = document.createElement('div')
  toast.className = 'toast'
  toast.innerHTML = `<i>✓</i><span>${message}</span>`
  toastArea.append(toast)
  setTimeout(() => toast.remove(), 3900)
}

async function copyRefundImage(button) {
  if (!['approved', 'completed'].includes(state.refundStatus)) return
  if (!navigator.clipboard?.write || typeof ClipboardItem === 'undefined' || !window.isSecureContext) {
    showToast('Thiết bị này chưa hỗ trợ sao chép ảnh. Bạn có thể tải ảnh PNG để gửi khách.')
    return
  }
  const previous = button.textContent
  button.disabled = true
  button.textContent = 'Đang sao chép ảnh…'
  try {
    const image = AuraRefundReceipt.blob(AuraRefundReceipt.create(refundData(), state.refundStatus))
    await navigator.clipboard.write([new ClipboardItem({ 'image/png': image })])
    showToast('Đã sao chép ảnh PNG. Mở chat Instagram và dán để gửi khách.')
  } catch {
    showToast('Không thể sao chép ảnh. Cho phép clipboard hoặc dùng nút Tải ảnh PNG.')
  } finally {
    button.disabled = false
    button.textContent = previous
  }
}

function downloadRefundImage() {
  if (!['approved', 'completed'].includes(state.refundStatus)) return
  const link = document.createElement('a')
  link.href = AuraRefundReceipt.create(refundData(), state.refundStatus).toDataURL('image/png')
  link.download = `Aura-Rental-${refundData().id}-v${refundData().revision || 1}-${state.refundStatus}.png`
  document.body.append(link)
  link.click()
  link.remove()
}

function openCustomerForm() {
  const branch = currentBranch()
  const rentalPackage = currentPackage()
  modal.innerHTML = `<div class="modal-header"><div><h2>Preview — Form khách thuê</h2><p>Đây là màn khách mở từ link OTP staff gửi.</p></div><button class="close-modal" data-action="close-modal">×</button></div><div class="modal-body"><form class="customer-form" id="customer-form"><div class="customer-top"><div class="brand-mark">A</div><div><strong>Aura Rental</strong><span>${branch.name}</span></div></div><div class="form-summary"><strong>Afrodille gấm · Size S</strong><span>${state.selectedAsset} · ${rentalPackage.label} / ${formatVnd(rentalPackage.price)} · 14/09 → 17/09</span></div><div class="field"><label>OTP được shop gửi</label><input class="text-input" value="482 917" inputmode="numeric" /></div><div class="field"><label>Họ và tên</label><input class="text-input" value="Ngọc Anh" /></div><div class="field"><label>Số điện thoại</label><input class="text-input" value="090 812 34 56" /></div><div class="field"><label>Địa chỉ nhận / trả đồ</label><textarea class="text-input" style="height:64px;padding-top:10px">112 Võ Văn Tần, Phường 6, Quận 3, TP.HCM</textarea></div><div class="field"><label>Ảnh CCCD <small>(bắt buộc cho gói 50%)</small></label><button class="button secondary" type="button">⌁ Tải ảnh CCCD (mock)</button></div><div class="form-message">Đơn được giữ tại ${branch.city}. Hệ thống kiểm tra OTP, mã đồ và lịch trống đúng chi nhánh trước khi tạo đơn.</div><button class="button full" style="margin-top:15px" type="submit">Gửi form xác nhận</button></form></div>`
  const identityField = [...modal.querySelectorAll('.field')].find(field => field.textContent.includes('Ảnh CCCD'))
  identityField.outerHTML = `<div class="form-message">Với gói cọc 50%, shop sẽ nhận và kiểm tra CCCD qua Instagram. Form này không thu thập hoặc lưu CCCD.</div>`
  modalBackdrop.hidden = false
}

function openBranchPicker() {
  modal.innerHTML = `<div class="modal-header"><div><h2>Chọn chi nhánh làm việc</h2><p>Tồn kho, lịch và bảng giá sẽ đổi theo chi nhánh.</p></div><button class="close-modal" data-action="close-modal">×</button></div><div class="modal-body branch-picker-list">${Object.values(branches).map(branch => `<button class="branch-picker-option ${branch.id === state.branchId ? 'active' : ''}" data-action="select-branch" data-branch-id="${branch.id}"><span class="branch-picker-code">${branch.code}</span><span><strong>${branch.name}</strong><small>${branch.address}</small><em>${branch.available} mã trống · Gói ${branch.packages.map(item => item.code).join(' / ')}</em></span><b>${branch.id === state.branchId ? '✓' : '›'}</b></button>`).join('')}</div>`
  modalBackdrop.hidden = false
}

function closeModal() { modalBackdrop.hidden = true; modal.innerHTML = '' }

function openWorkspace() {
  state.view = 'dashboard'
  loginScreen.hidden = true
  appShell.hidden = false
  mobileNav.hidden = false
  document.body.classList.remove('login-active')
  render()
  showToast(`Đăng nhập thành công với vai trò ${state.role === 'manager' ? 'Manager' : 'Staff'}.`)
}

function logout() {
  appShell.hidden = true
  mobileNav.hidden = true
  loginScreen.hidden = false
  document.body.classList.add('login-active')
  loginPassword.value = 'demo1234'
  loginEmail.focus()
}

document.addEventListener('input', event => {
  if (event.target.id === 'refund-adjustment-reason' && state.role === 'manager' && state.refundStatus === 'adjusting') {
    state.refundAdjustmentReason = event.target.value
  }
})

document.addEventListener('click', event => {
  const refundRoleButton = event.target.closest('[data-refund-role]')
  if (refundRoleButton) {
    state.role = refundRoleButton.dataset.refundRole
    render()
    return
  }
  const loginRoleButton = event.target.closest('[data-login-role]')
  if (loginRoleButton) {
    state.role = loginRoleButton.dataset.loginRole
    document.querySelectorAll('[data-login-role]').forEach(button => {
      const active = button.dataset.loginRole === state.role
      button.classList.toggle('active', active)
      button.setAttribute('aria-pressed', String(active))
    })
    loginEmail.value = `${state.role}@demo.aurarental.vn`
    return
  }
  const viewButton = event.target.closest('[data-view]')
  if (viewButton) { event.preventDefault(); state.view = viewButton.dataset.view; render(); document.querySelector('.sidebar').classList.remove('open'); return }
  const orderButton = event.target.closest('[data-order]')
  if (orderButton) { state.selectedOrder = orderButton.dataset.order; state.view = 'order-detail'; render(); return }
  const action = event.target.closest('[data-action]')?.dataset.action
  if (!action) return
  if (action === 'toggle-password') {
    const showing = loginPassword.type === 'text'
    loginPassword.type = showing ? 'password' : 'text'
    event.target.closest('[data-action]').setAttribute('aria-label', showing ? 'Hiện mật khẩu' : 'Ẩn mật khẩu')
  }
  if (action === 'forgot-password') { event.preventDefault(); showToast('Mockup: yêu cầu đặt lại mật khẩu sẽ được gửi tới email công việc.') }
  if (action === 'logout') { logout(); return }
  if (action === 'open-branch-picker') { openBranchPicker(); return }
  if (action === 'select-branch') {
    state.branchId = event.target.closest('[data-action]').dataset.branchId
    state.rentalPackage = currentBranch().packages[0].code
    state.selectedAsset = currentBranch().assets[0]
    state.issueComplete = false
    state.refundStatus = 'draft'
    state.refundSnapshot = null
    closeModal()
    render()
    showToast(`Đã chuyển sang ${currentBranch().name}. Tồn kho và bảng giá đã được cập nhật.`)
    return
  }
  if (action === 'select-rental-package') {
    state.rentalPackage = event.target.closest('[data-action]').dataset.packageCode
    render()
    return
  }
  if (action === 'generate-code') { state.issueComplete = true; render(); showToast(`Đã giữ ${state.selectedAsset} tại ${currentBranch().code} và tạo OTP.`) }
  if (action === 'restart-issue') { state.issueComplete = false; render() }
  if (action === 'copy-link') { showToast('Đã sao chép nội dung gửi khách (mockup).') }
  if (action === 'preview-form') openCustomerForm()
  if (action === 'close-modal') closeModal()
  if (action === 'verify-identity') { state.idVerified = true; render(); showToast('Đã ghi nhận staff kiểm tra CCCD qua Instagram. Không có dữ liệu CCCD được lưu.') }
  if (action === 'confirm-deposit') {
    if (!state.idVerified) showToast('Cần staff xác nhận đã kiểm tra CCCD qua Instagram trước khi xác nhận đơn cọc 50%.')
    else showToast('Đã xác nhận cọc còn lại. Đơn chuyển sang “Đã xác nhận”.')
  }
  if (action === 'set-item-condition' && (state.refundStatus === 'draft' || (state.role === 'manager' && ['pending', 'adjusting'].includes(state.refundStatus)))) {
    const button = event.target.closest('[data-action]')
    state.returnItemConditions[button.dataset.itemId] = button.dataset.condition
    if (state.refundSnapshot) state.refundSnapshot = { ...state.refundSnapshot, ...calculateRefund() }
    render()
  }
  if (action === 'open-item-evidence') showToast('Mockup: mở ảnh, mô tả, mức độ và hướng xử lý kho của món đã chọn.')
  if (action === 'request-refund' && state.role === 'staff' && state.refundStatus === 'draft') {
    state.refundSnapshot = { ...refundData(), requestedAt: refundTime() }
    state.refundStatus = 'pending'
    render()
    showToast('Đã gửi yêu cầu hoàn tiền cho manager Quỳnh Anh duyệt.')
  }
  if (action === 'approve-refund' && state.role === 'manager' && ['draft', 'pending', 'adjusting'].includes(state.refundStatus)) {
    const adjusting = state.refundStatus === 'adjusting'
    if (adjusting && !state.refundAdjustmentReason.trim()) {
      showToast('Vui lòng nhập lý do điều chỉnh trước khi duyệt lại.')
      document.querySelector('#refund-adjustment-reason')?.focus()
      return
    }
    state.refundSnapshot = { ...refundData(), approvedAt: refundTime(), revision: adjusting ? state.refundPreviousApproval.revision + 1 : 1, ...(adjusting ? { adjustmentReason: state.refundAdjustmentReason.trim(), previousApproval: state.refundPreviousApproval } : {}) }
    state.refundPreviousApproval = null
    state.refundAdjustmentReason = ''
    state.refundStatus = 'approved'
    render()
    showToast('Đã duyệt hoàn tiền. Ảnh tổng kết đã sẵn sàng để sao chép gửi khách.')
  }
  if (action === 'adjust-refund' && state.role === 'manager' && state.refundStatus === 'approved') {
    state.refundPreviousApproval = state.refundSnapshot
    state.refundSnapshot = null
    state.refundAdjustmentReason = ''
    state.refundStatus = 'adjusting'
    render()
  }
  if (action === 'cancel-refund-adjustment' && state.role === 'manager' && state.refundStatus === 'adjusting') {
    state.refundSnapshot = state.refundPreviousApproval
    state.refundPreviousApproval = null
    state.refundAdjustmentReason = ''
    state.refundStatus = 'approved'
    render()
    showToast('Đã hủy điều chỉnh, giữ nguyên số tiền và ảnh đã duyệt.')
  }
  if (action === 'return-refund-review' && state.role === 'manager' && state.refundStatus === 'pending') {
    state.refundStatus = 'draft'
    state.refundSnapshot = null
    render()
    showToast('Đã trả yêu cầu cho staff kiểm tra và gửi duyệt lại.')
  }
  if (action === 'confirm-refund-transfer' && state.role === 'manager' && state.refundStatus === 'approved') {
    state.refundSnapshot = { ...state.refundSnapshot, refundedAt: refundTime() }
    state.refundStatus = 'completed'
    render()
    showToast('Đã ghi nhận chuyển khoản hoàn khách và cập nhật ảnh. Mã đồ chuyển sang Cleaning 12 giờ (demo).')
  }
  if (action === 'copy-refund-image') copyRefundImage(event.target.closest('[data-action]'))
  if (action === 'download-refund-image') downloadRefundImage()
  if (action === 'search-availability') { showToast(`Đã tìm ${currentBranch().available} mã trống tại ${currentBranch().name}.`) }
  if (action === 'select-asset') {
    state.selectedAsset = event.target.closest('[data-action]').dataset.assetCode
    state.view = 'issue-form'
    render()
    showToast(`Đã chọn ${state.selectedAsset} tại ${currentBranch().code}.`)
  }
  if (action === 'toggle-schedule-details') { state.scheduleExpanded = !state.scheduleExpanded; render() }
  if (action === 'show-operations') { showToast('Mockup: mở danh sách 18 việc vận hành cần xử lý hôm nay.') }
  if (action === 'open-all-assets') { showToast('Mockup: danh sách sẽ dùng virtual scroll và chỉ tải theo nhóm đã chọn.') }
  if (action === 'save-product') { state.view = 'product-detail'; render(); showToast('Đã lưu sản phẩm mới và tạo các mã đồ vật lý.') }
  if (action === 'upload-product-image') { showToast('Mockup: ảnh sản phẩm đã sẵn sàng để tải lên.') }
  if (action === 'add-variant') { showToast('Mockup: đã thêm một dòng size mới để khai báo.') }
  if (action === 'add-asset-code') { showToast('Mockup: thêm mã đồ vật lý mới.') }
  if (action === 'import-stock') { showToast('Mockup: bạn có thể nhập danh sách mã đồ từ file ở đây.') }
  if (action === 'edit-product') { state.view = 'product-add'; render() }
  if (action === 'show-size-assets') { showToast('Đang hiển thị các mã vật lý thuộc size đã chọn.') }
  if (action === 'view-asset-history') { showToast('Mockup: xem timeline reservation, thuê và vệ sinh của mã đồ.') }
})

document.addEventListener('submit', event => {
  if (event.target === loginForm) {
    event.preventDefault()
    openWorkspace()
    return
  }
  if (event.target.id === 'customer-form') {
    event.preventDefault()
    closeModal()
    state.view = 'orders'
    render()
    showToast('Khách đã gửi form. Hệ thống đã tự tạo đơn AR-240914-018.')
  }
})

document.querySelector('#mobile-menu').addEventListener('click', () => document.querySelector('.sidebar').classList.toggle('open'))
document.querySelector('#notification-button').addEventListener('click', () => showToast('3 thông báo: đơn mới, hold sắp hết hạn, yêu cầu hoàn tiền.'))
modalBackdrop.addEventListener('click', event => { if (event.target === modalBackdrop) closeModal() })

render()
