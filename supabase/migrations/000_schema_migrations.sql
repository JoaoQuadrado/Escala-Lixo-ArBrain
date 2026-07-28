-- Controlo de migrations aplicadas pela API
create table if not exists public.schema_migrations (
  id text primary key,
  applied_at timestamptz not null default now()
);

comment on table public.schema_migrations is 'Migrations SQL já executadas pelo EscalaLixo.Api';
