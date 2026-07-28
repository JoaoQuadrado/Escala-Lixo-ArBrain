-- Valor do enum num ficheiro separado: Postgres exige COMMIT antes de usar o novo valor.
ALTER TYPE public.escala_slot ADD VALUE IF NOT EXISTS 'fila_espera';
