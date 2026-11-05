-- =========================================================================
-- Script cập nhật ràng buộc useraccount_role_check
-- Cho phép thêm các vai trò: 'Production', 'Warehouse', 'Sales', 'Kho', 'BanHang'
-- Giữ nguyên vẹn dữ liệu cũ: 'Admin', 'Manager', 'Employee'
-- =========================================================================

ALTER TABLE useraccount 
DROP CONSTRAINT IF EXISTS useraccount_role_check;

ALTER TABLE useraccount 
ADD CONSTRAINT useraccount_role_check 
CHECK (role IN ('Admin', 'Manager', 'Employee', 'Production', 'Warehouse', 'Sales', 'Kho', 'BanHang'));
