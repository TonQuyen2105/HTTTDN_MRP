-- =========================================================================
-- Migration: 003_SeparateProductionAndHandover.sql
-- Mục đích: Tách biệt nguồn dữ liệu sản lượng sản xuất khỏi phiếu bàn giao kho
--           Bổ sung bảng nhật ký sản xuất và cơ chế chống gửi lặp phiếu bàn giao
-- =========================================================================

-- 1. Tạo bảng nhật ký sản xuất (production_log)
--    Ghi nhận bền vững từng lần sản xuất hoàn thành tại phân xưởng
CREATE TABLE IF NOT EXISTS production_log (
    logid SERIAL PRIMARY KEY,
    requestid INT NOT NULL REFERENCES productionrequest(requestid) ON DELETE CASCADE,
    productid INT NOT NULL REFERENCES product(productid) ON DELETE RESTRICT,
    producedquantity INT NOT NULL CHECK (producedquantity > 0),
    createddate TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    createdby INT REFERENCES useraccount(userid),
    note VARCHAR(255)
);

CREATE INDEX IF NOT EXISTS idx_production_log_requestid ON production_log(requestid);
CREATE INDEX IF NOT EXISTS idx_production_log_productid ON production_log(productid);

-- 2. Bổ sung các cột phục vụ phân loại và chống gửi trùng trên productreceipt
ALTER TABLE productreceipt ADD COLUMN IF NOT EXISTS receipttype VARCHAR(50) DEFAULT 'Handover';
ALTER TABLE productreceipt ADD COLUMN IF NOT EXISTS idempotencykey VARCHAR(100);

-- Đánh dấu dữ liệu ProductReceipt cũ (các bản ghi trước đây chỉ dùng để tích lũy sản lượng)
-- để không bị coi nhầm là phiếu bàn giao kho đang chờ nhận hoặc đã giao
UPDATE productreceipt 
SET receipttype = 'LegacyAccumulator' 
WHERE receiptcode IS NULL AND productreceiptid <= 19 AND receipttype IS NULL;

-- 3. Đảm bảo Unique Index trên idempotencykey để chống gửi lặp tuyệt đối ở tầng CSDL
CREATE UNIQUE INDEX IF NOT EXISTS uq_productreceipt_idempotency 
ON productreceipt (idempotencykey) 
WHERE idempotencykey IS NOT NULL;

-- 4. Khởi tạo dữ liệu ban đầu cho production_log từ các bản ghi tích lũy hợp lệ cũ
INSERT INTO production_log (requestid, productid, producedquantity, createddate, createdby, note)
SELECT pr.requestid, prd.productid, prd.quantity, COALESCE(pr.createddate, CURRENT_TIMESTAMP), pr.requestedby, prd.note
FROM productreceipt pr
JOIN productreceiptdetail prd ON pr.productreceiptid = prd.productreceiptid
WHERE pr.requestid IS NOT NULL 
  AND (pr.receipttype = 'LegacyAccumulator' OR pr.receiptcode IS NULL)
  AND prd.quantity > 0
  AND NOT EXISTS (
      SELECT 1 FROM production_log pl WHERE pl.requestid = pr.requestid
  );

-- GHI CHÚ QUẢN TRỊ VIÊN:
-- File này được chuẩn bị sẵn để chạy trên môi trường Database staging/production
-- khi triển khai nâng cấp chính thức.
