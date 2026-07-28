-- Posição única por colaborador na escala (impossível duplicar no banco)
DO $$ BEGIN
  CREATE TYPE public.escala_slot AS ENUM (
    'segunda-feira',
    'terça-feira',
    'quarta-feira',
    'quinta-feira',
    'sexta-feira',
    'bloqueados'
  );
EXCEPTION
  WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS public.escala_posicoes (
  escala_id int NOT NULL DEFAULT 1 REFERENCES public.escala_ativa(id) ON DELETE CASCADE,
  colaborador_id uuid NOT NULL REFERENCES public.colaboradores(id) ON DELETE CASCADE,
  slot public.escala_slot NOT NULL,
  ordem int NOT NULL DEFAULT 0,
  PRIMARY KEY (escala_id, colaborador_id),
  CONSTRAINT escala_posicoes_ordem_nonneg CHECK (ordem >= 0)
);

CREATE INDEX IF NOT EXISTS escala_posicoes_slot_idx
  ON public.escala_posicoes (escala_id, slot, ordem);

-- Máximo 2 colaboradores por dia útil (dupla)
CREATE OR REPLACE FUNCTION public.enforce_max_dupla_por_dia()
RETURNS TRIGGER AS $$
DECLARE
  cnt int;
BEGIN
  IF NEW.slot = 'bloqueados'::public.escala_slot THEN
    RETURN NEW;
  END IF;

  SELECT COUNT(*) INTO cnt
  FROM public.escala_posicoes
  WHERE escala_id = NEW.escala_id
    AND slot = NEW.slot
    AND colaborador_id <> NEW.colaborador_id;

  IF cnt >= 2 THEN
    RAISE EXCEPTION 'DUPLA_CHEIA: máximo de 2 colaboradores em %', NEW.slot
      USING ERRCODE = 'check_violation';
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_escala_posicoes_max_dupla ON public.escala_posicoes;
CREATE TRIGGER trg_escala_posicoes_max_dupla
  BEFORE INSERT OR UPDATE OF slot ON public.escala_posicoes
  FOR EACH ROW EXECUTE FUNCTION public.enforce_max_dupla_por_dia();

ALTER TABLE public.escala_ativa
  ADD COLUMN IF NOT EXISTS bloqueados jsonb NOT NULL DEFAULT '[]'::jsonb;

ALTER TABLE public.escala_posicoes ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "escala_posicoes_select_anon" ON public.escala_posicoes;
CREATE POLICY "escala_posicoes_select_anon"
  ON public.escala_posicoes FOR SELECT TO anon, authenticated USING (true);

DROP POLICY IF EXISTS "escala_posicoes_write_authenticated" ON public.escala_posicoes;
CREATE POLICY "escala_posicoes_write_authenticated"
  ON public.escala_posicoes FOR ALL TO authenticated USING (true) WITH CHECK (true);

-- Importar dados existentes do JSON (dias)
INSERT INTO public.escala_posicoes (escala_id, colaborador_id, slot, ordem)
SELECT
  1,
  c.id,
  CASE lower(trim(d.elem->>'dia_semana'))
    WHEN 'segunda-feira' THEN 'segunda-feira'::public.escala_slot
    WHEN 'terça-feira' THEN 'terça-feira'::public.escala_slot
    WHEN 'terca-feira' THEN 'terça-feira'::public.escala_slot
    WHEN 'quarta-feira' THEN 'quarta-feira'::public.escala_slot
    WHEN 'quinta-feira' THEN 'quinta-feira'::public.escala_slot
    WHEN 'sexta-feira' THEN 'sexta-feira'::public.escala_slot
    ELSE NULL
  END,
  (n.ord - 1)::int
FROM public.escala_ativa e
CROSS JOIN LATERAL jsonb_array_elements(e.dias) AS d(elem)
CROSS JOIN LATERAL jsonb_array_elements_text(d.elem->'nomes') WITH ORDINALITY AS n(nome, ord)
JOIN public.colaboradores c ON lower(trim(c.nome)) = lower(trim(n.nome))
WHERE e.id = 1
  AND CASE lower(trim(d.elem->>'dia_semana'))
    WHEN 'segunda-feira' THEN 'segunda-feira'::public.escala_slot
    WHEN 'terça-feira' THEN 'terça-feira'::public.escala_slot
    WHEN 'terca-feira' THEN 'terça-feira'::public.escala_slot
    WHEN 'quarta-feira' THEN 'quarta-feira'::public.escala_slot
    WHEN 'quinta-feira' THEN 'quinta-feira'::public.escala_slot
    WHEN 'sexta-feira' THEN 'sexta-feira'::public.escala_slot
    ELSE NULL
  END IS NOT NULL
ON CONFLICT (escala_id, colaborador_id) DO NOTHING;

-- Importar bloqueados do JSON (se existirem)
INSERT INTO public.escala_posicoes (escala_id, colaborador_id, slot, ordem)
SELECT
  1,
  c.id,
  'bloqueados'::public.escala_slot,
  (n.ord - 1)::int
FROM public.escala_ativa e
CROSS JOIN LATERAL jsonb_array_elements_text(e.bloqueados) WITH ORDINALITY AS n(nome, ord)
JOIN public.colaboradores c ON lower(trim(c.nome)) = lower(trim(n.nome))
WHERE e.id = 1
ON CONFLICT (escala_id, colaborador_id) DO NOTHING;

COMMENT ON TABLE public.escala_posicoes IS 'Uma linha por colaborador — slot único na semana (fila de espera = sem linha)';
