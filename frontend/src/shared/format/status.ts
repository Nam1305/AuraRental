const labels: Record<string, string> = {
  ACTIVE: 'Đang giữ chỗ',
  OVERDUE: 'Quá hạn cọc',
  CONVERTED_TO_ORDER: 'Đã lên đơn',
  CANCELLED: 'Đã hủy',
  PENDING_DEPOSIT: 'Chờ đủ cọc',
  PENDING_VERIFICATION: 'Chờ xác minh',
  CONFIRMED: 'Đã xác nhận',
  PREPARING: 'Đang chuẩn bị',
  RENTING: 'Đang thuê',
  INSPECTING: 'Đang kiểm hàng',
  COMPLETED: 'Hoàn tất',
  DRAFT: 'Bản nháp',
  SUBMITTED: 'Chờ duyệt',
  APPROVED: 'Đã duyệt',
  GOOD: 'Tốt',
  DAMAGED: 'Hư hỏng',
  MISSING: 'Thất lạc',
  USABLE: 'Sẵn sàng',
  MAINTENANCE: 'Bảo trì',
  LOST: 'Mất',
  SLOT_DEPOSIT: 'Cọc giữ chỗ',
  TARGET_DEPOSIT: 'Cọc đích',
  ADDITIONAL_COLLECTION: 'Thu thêm',
  REFUND: 'Hoàn cọc',
}

export const statusLabel = (status: string | null | undefined) =>
  status ? labels[status] ?? status.replaceAll('_', ' ') : 'Chưa tạo'
