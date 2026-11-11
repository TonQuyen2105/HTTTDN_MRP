-- =========================================================================
-- Script bổ sung Unique Index trên cột requestid của bảng productreceipt
-- Ngăn ngừa việc tạo trùng 2 phiếu nhập cho cùng một đơn hàng (ProductionRequest)
-- Chỉ áp dụng cho các bản ghi có requestid IS NOT NULL (giữ nguyên các phiếu độc lập cũ)
-- =========================================================================

CREATE UNIQUE INDEX IF NOT EXISTS uq_productreceipt_requestid 
ON productreceipt (requestid) 
WHERE requestid IS NOT NULL;
