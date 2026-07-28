-- Movimento atómico: 1 UPDATE/INSERT/DELETE por drag-and-drop (com locks)
CREATE OR REPLACE FUNCTION public.compactir_ordem_slot(p_escala_id int, p_slot public.escala_slot)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
  WITH ranked AS (
    SELECT colaborador_id, (row_number() OVER (ORDER BY ordem, colaborador_id) - 1)::int AS nova_ordem
    FROM public.escala_posicoes
    WHERE escala_id = p_escala_id AND slot = p_slot
  )
  UPDATE public.escala_posicoes p
  SET ordem = r.nova_ordem
  FROM ranked r
  WHERE p.escala_id = p_escala_id AND p.colaborador_id = r.colaborador_id;
END;
$$;

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
  v_destino_fila boolean;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.colaboradores WHERE id = p_colaborador_id) THEN
    RAISE EXCEPTION 'COLABORADOR_INEXISTENTE: colaborador não encontrado'
      USING ERRCODE = 'foreign_key_violation';
  END IF;

  PERFORM 1 FROM public.escala_ativa WHERE id = 1 FOR UPDATE;

  v_destino_fila := lower(trim(p_slot_destino)) IN ('fila_espera', 'waiting');

  SELECT slot, ordem
  INTO v_slot_origem, v_ordem_origem
  FROM public.escala_posicoes
  WHERE escala_id = 1 AND colaborador_id = p_colaborador_id
  FOR UPDATE;

  IF v_destino_fila THEN
    IF v_slot_origem IS NOT NULL THEN
      DELETE FROM public.escala_posicoes
      WHERE escala_id = 1 AND colaborador_id = p_colaborador_id;
      PERFORM public.compactir_ordem_slot(1, v_slot_origem);
    END IF;
    RETURN;
  END IF;

  BEGIN
    v_slot_destino := trim(p_slot_destino)::public.escala_slot;
  EXCEPTION
    WHEN invalid_text_representation THEN
      RAISE EXCEPTION 'SLOT_INVALIDO: destino "%" inválido', p_slot_destino
        USING ERRCODE = 'invalid_parameter_value';
  END;

  p_ordem := GREATEST(0, p_ordem);

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

COMMENT ON FUNCTION public.mover_colaborador_escala IS
  'Move colaborador para um slot (ou fila de espera). Garante exclusividade via PK e trigger de dupla.';
