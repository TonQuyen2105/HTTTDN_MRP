-- Additive migration: no existing BOM or production data is changed.
CREATE TABLE public.bom_history (
    historyid bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    operationkey uuid NOT NULL UNIQUE,
    productid integer NOT NULL REFERENCES public.product(productid),
    bomid integer NOT NULL,
    changedby integer NOT NULL REFERENCES public.useraccount(userid),
    changedbyname text NOT NULL,
    changedat timestamptz NOT NULL DEFAULT now(),
    reason varchar(1000) NOT NULL,
    beforedata jsonb,
    afterdata jsonb NOT NULL,
    CONSTRAINT bom_history_snapshots_objects CHECK (
        (beforedata IS NULL OR jsonb_typeof(beforedata) = 'object') AND jsonb_typeof(afterdata) = 'object')
);
CREATE INDEX ix_bom_history_formula_time ON public.bom_history(bomid, changedat DESC, historyid DESC);
ALTER TABLE public.bom_history ENABLE ROW LEVEL SECURITY;
COMMENT ON TABLE public.bom_history IS 'Read-only audit history in the Production UI; captures BOM changes from deployment onward.';
