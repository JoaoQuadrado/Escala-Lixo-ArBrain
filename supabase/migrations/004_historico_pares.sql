-- Histórico de duplas e sequências (substitui historico_pares.json)
create table if not exists public.historico_pares (
  id int primary key default 1 check (id = 1),
  contagens_pares jsonb not null default '{}'::jsonb,
  sequencias_consecutivas jsonb not null default '{}'::jsonb,
  updated_at timestamptz not null default now()
);

drop trigger if exists historico_pares_updated_at on public.historico_pares;
create trigger historico_pares_updated_at
  before update on public.historico_pares
  for each row execute function public.set_updated_at();

alter table public.historico_pares enable row level security;

create policy "historico_pares_select_anon"
  on public.historico_pares for select to anon, authenticated using (true);

create policy "historico_pares_write_authenticated"
  on public.historico_pares for all to authenticated using (true) with check (true);

comment on table public.historico_pares is 'Contagem de pares e sequências consecutivas por colaborador';
