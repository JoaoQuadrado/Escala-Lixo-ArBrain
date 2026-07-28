-- Arquivo de escalas semanais (snapshots ao gerar nova escala)
create table if not exists public.escala_historico (
  id uuid primary key default gen_random_uuid(),
  inicio_semana date not null,
  dias jsonb not null default '[]'::jsonb,
  fila_espera jsonb not null default '[]'::jsonb,
  bloqueados jsonb not null default '[]'::jsonb,
  motivo text not null default 'geracao',
  arquivado_em timestamptz not null default now()
);

create index if not exists idx_escala_historico_inicio
  on public.escala_historico (inicio_semana desc);

create index if not exists idx_escala_historico_arquivado
  on public.escala_historico (arquivado_em desc);

alter table public.escala_historico enable row level security;

drop policy if exists "escala_historico_select_anon" on public.escala_historico;
create policy "escala_historico_select_anon"
  on public.escala_historico for select to anon, authenticated using (true);

drop policy if exists "escala_historico_write_authenticated" on public.escala_historico;
create policy "escala_historico_write_authenticated"
  on public.escala_historico for all to authenticated using (true) with check (true);

comment on table public.escala_historico is 'Snapshots de escalas semanais arquivadas';
