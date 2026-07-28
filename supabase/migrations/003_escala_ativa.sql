-- Escala semanal ativa (substitui escala_semana.json)
create table if not exists public.escala_ativa (
  id int primary key default 1 check (id = 1),
  inicio_semana date not null,
  dias jsonb not null default '[]'::jsonb,
  fila_espera jsonb not null default '[]'::jsonb,
  conteudo_hash text not null default '',
  updated_at timestamptz not null default now()
);

drop trigger if exists escala_ativa_updated_at on public.escala_ativa;
create trigger escala_ativa_updated_at
  before update on public.escala_ativa
  for each row execute function public.set_updated_at();

alter table public.escala_ativa enable row level security;

create policy "escala_ativa_select_anon"
  on public.escala_ativa for select to anon, authenticated using (true);

create policy "escala_ativa_write_authenticated"
  on public.escala_ativa for all to authenticated using (true) with check (true);

comment on table public.escala_ativa is 'Escala da semana corrente (singleton)';
