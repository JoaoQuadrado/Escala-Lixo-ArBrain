-- Fila de espera explícita na tabela escala_posicoes (cada colaborador tem exactamente 1 slot)

-- Trigger e função de movimento primeiro (INSERTs usam o slot fila_espera)
CREATE OR REPLACE FUNCTION public.enforce_max_dupla_por_dia()
RETURNS TRIGGER AS $$
DECLARE
  cnt int;
BEGIN
  IF NEW.slot IN ('bloqueados'::public.escala_slot, 'fila_espera'::public.escala_slot) THEN
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

CREATE OR REPLACE FUNCTION public.mover_colaborador_escala(
  p_colaborador_id uuid,
  p_slot_destino text,
  p_ordem int DEFAULT 0
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
  v_slot_destino public.escala_slot;
  v_slot_origem public.escala_slot;
  v_ordem_origem int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.colaboradores WHERE id = p_colaborador_id) THEN
    RAISE EXCEPTION 'COLABORADOR_INEXISTENTE: colaborador não encontrado'
      USING ERRCODE = 'foreign_key_violation';
  END IF;

  PERFORM 1 FROM public.escala_ativa WHERE id = 1 FOR UPDATE;

  IF lower(trim(p_slot_destino)) IN ('fila_espera', 'waiting') THEN
    p_slot_destino := 'fila_espera';
  END IF;

  BEGIN
    v_slot_destino := trim(p_slot_destino)::public.escala_slot;
  EXCEPTION
    WHEN invalid_text_representation THEN
      RAISE EXCEPTION 'SLOT_INVALIDO: destino "%" inválido', p_slot_destino
        USING ERRCODE = 'invalid_parameter_value';
  END;

  p_ordem := GREATEST(0, p_ordem);

  SELECT slot, ordem
  INTO v_slot_origem, v_ordem_origem
  FROM public.escala_posicoes
  WHERE escala_id = 1 AND colaborador_id = p_colaborador_id
  FOR UPDATE;

  IF v_slot_origem IS NULL THEN
    UPDATE public.escala_posicoes
    SET ordem = ordem + 1
    WHERE escala_id = 1
      AND slot = v_slot_destino
      AND ordem >= p_ordem;

    INSERT INTO public.escala_posicoes (escala_id, colaborador_id, slot, ordem)
    VALUES (1, p_colaborador_id, v_slot_destino, p_ordem);

    PERFORM public.compactir_ordem_slot(1, v_slot_destino);
    RETURN;
  END IF;

  IF v_slot_origem = v_slot_destino THEN
    IF p_ordem = v_ordem_origem THEN
      RETURN;
    END IF;

    IF p_ordem < v_ordem_origem THEN
      UPDATE public.escala_posicoes
      SET ordem = ordem + 1
      WHERE escala_id = 1
        AND slot = v_slot_destino
        AND ordem >= p_ordem
        AND ordem < v_ordem_origem
        AND colaborador_id <> p_colaborador_id;
    ELSE
      UPDATE public.escala_posicoes
      SET ordem = ordem - 1
      WHERE escala_id = 1
        AND slot = v_slot_destino
        AND ordem <= p_ordem
        AND ordem > v_ordem_origem
        AND colaborador_id <> p_colaborador_id;
    END IF;

    UPDATE public.escala_posicoes
    SET ordem = p_ordem
    WHERE escala_id = 1 AND colaborador_id = p_colaborador_id;

    PERFORM public.compactir_ordem_slot(1, v_slot_destino);
    RETURN;
  END IF;

  UPDATE public.escala_posicoes
  SET ordem = ordem + 1
  WHERE escala_id = 1
    AND slot = v_slot_destino
    AND ordem >= p_ordem
    AND colaborador_id <> p_colaborador_id;

  UPDATE public.escala_posicoes
  SET slot = v_slot_destino, ordem = p_ordem
  WHERE escala_id = 1 AND colaborador_id = p_colaborador_id;

  PERFORM public.compactir_ordem_slot(1, v_slot_origem);
  PERFORM public.compactir_ordem_slot(1, v_slot_destino);
END;
$$;

-- Garantir linha singleton antes de inserir posições
INSERT INTO public.escala_ativa (id, inicio_semana, dias, fila_espera, bloqueados, conteudo_hash)
SELECT
  1,
  date_trunc('week', CURRENT_DATE)::date,
  '[]'::jsonb,
  '[]'::jsonb,
  '[]'::jsonb,
  ''
WHERE NOT EXISTS (SELECT 1 FROM public.escala_ativa WHERE id = 1);

-- Colaboradores sem posição → fila de espera
INSERT INTO public.escala_posicoes (escala_id, colaborador_id, slot, ordem)
SELECT
  1,
  c.id,
  'fila_espera'::public.escala_slot,
  (row_number() OVER (ORDER BY c.nome) - 1)::int
FROM public.colaboradores c
WHERE NOT EXISTS (
  SELECT 1 FROM public.escala_posicoes p
  WHERE p.escala_id = 1 AND p.colaborador_id = c.id
)
ON CONFLICT (escala_id, colaborador_id) DO NOTHING;

-- Importar fila do JSON legado (se existir)
INSERT INTO public.escala_posicoes (escala_id, colaborador_id, slot, ordem)
SELECT
  1,
  c.id,
  'fila_espera'::public.escala_slot,
  (n.ord - 1)::int
FROM public.escala_ativa e
CROSS JOIN LATERAL jsonb_array_elements_text(e.fila_espera) WITH ORDINALITY AS n(nome, ord)
JOIN public.colaboradores c ON lower(trim(c.nome)) = lower(trim(n.nome))
WHERE e.id = 1
ON CONFLICT (escala_id, colaborador_id) DO UPDATE
  SET slot = 'fila_espera'::public.escala_slot,
      ordem = EXCLUDED.ordem;
