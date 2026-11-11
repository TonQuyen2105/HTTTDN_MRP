-- Approved production expiry migration.
-- Luu han su dung tai thoi diem ghi nhan san xuat.
-- Moi phieu ban giao moi tham chieu mot lan san xuat, co the giao mot phan.
-- Khong suy dien han su dung hoac nguon san xuat cho du lieu cu.
BEGIN;
SET LOCAL lock_timeout = '5s';
SET LOCAL statement_timeout = '30s';

ALTER TABLE public.production_log
    ADD COLUMN expireddate date NULL;

ALTER TABLE public.productreceiptdetail
    ADD COLUMN productionlogid integer NULL
    REFERENCES public.production_log(logid) ON DELETE RESTRICT;

CREATE INDEX ix_productreceiptdetail_productionlogid
    ON public.productreceiptdetail(productionlogid);

-- Cot public.productreceiptdetail.expireddate da co trong mapping hien tai.
-- Code se sao chep han tu production_log khi lap phieu, khong tinh lai theo ngay giao.
-- Schema da duoc doi chieu truoc khi chay.
COMMIT;
