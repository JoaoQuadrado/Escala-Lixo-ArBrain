-- Escala Lixo — tabela de colaboradores
-- Executar no SQL Editor do Supabase: https://supabase.com/dashboard/project/_/sql

create extension if not exists "pgcrypto";

create table if not exists public.colaboradores (
  id uuid primary key default gen_random_uuid(),
  nome text not null,
  usuario_discord text not null default '',
  cargo text not null default 'Auxiliar',
  cor text not null default '#FFC300',
  foto_url text,
  de_ferias boolean not null default false,
  ausente boolean not null default false,
  observacoes text,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint colaboradores_nome_unique unique (nome)
);

create index if not exists colaboradores_nome_idx on public.colaboradores (nome);

-- Trigger updated_at
create or replace function public.set_updated_at()
returns trigger as $$
begin
  new.updated_at = now();
  return new;
end;
$$ language plpgsql;

drop trigger if exists colaboradores_updated_at on public.colaboradores;
create trigger colaboradores_updated_at
  before update on public.colaboradores
  for each row execute function public.set_updated_at();

-- RLS
alter table public.colaboradores enable row level security;

-- Leitura pública (anon key — app desktop autenticado via anon)
create policy "colaboradores_select_anon"
  on public.colaboradores for select
  to anon, authenticated
  using (true);

-- Escrita via authenticated ou service role
create policy "colaboradores_insert_authenticated"
  on public.colaboradores for insert
  to authenticated
  with check (true);

create policy "colaboradores_update_authenticated"
  on public.colaboradores for update
  to authenticated
  using (true)
  with check (true);

create policy "colaboradores_delete_authenticated"
  on public.colaboradores for delete
  to authenticated
  using (true);

-- Service role bypassa RLS automaticamente

comment on table public.colaboradores is 'Colaboradores da escala de limpeza';
