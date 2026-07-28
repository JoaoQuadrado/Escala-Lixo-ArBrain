-- Biblioteca de GIFs reutilizáveis
create table if not exists public.biblioteca_gifs (
  id uuid primary key default gen_random_uuid(),
  nome text not null default 'GIF',
  dados bytea not null,
  mime text not null default 'image/gif',
  created_at timestamptz not null default now()
);

alter table public.configuracao_app
  add column if not exists gif_previa_id uuid references public.biblioteca_gifs(id) on delete set null,
  add column if not exists gif_diario_id uuid references public.biblioteca_gifs(id) on delete set null;

-- Migrar GIFs inline (010) para a biblioteca
do $$
declare
  v_previa_id uuid;
  v_diario_id uuid;
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'configuracao_app' and column_name = 'gif_previa_semanal'
  ) then
    insert into public.biblioteca_gifs (nome, dados, mime)
    select 'Prévia semanal', gif_previa_semanal, coalesce(nullif(gif_previa_mime, ''), 'image/gif')
    from public.configuracao_app
    where id = 1
      and gif_previa_semanal is not null
      and octet_length(gif_previa_semanal) > 0
      and gif_previa_id is null
    returning id into v_previa_id;

    if v_previa_id is not null then
      update public.configuracao_app set gif_previa_id = v_previa_id where id = 1;
    end if;

    insert into public.biblioteca_gifs (nome, dados, mime)
    select 'Lembrete diário', gif_diario, coalesce(nullif(gif_diario_mime, ''), 'image/gif')
    from public.configuracao_app
    where id = 1
      and gif_diario is not null
      and octet_length(gif_diario) > 0
      and gif_diario_id is null
    returning id into v_diario_id;

    if v_diario_id is not null then
      update public.configuracao_app set gif_diario_id = v_diario_id where id = 1;
    end if;
  end if;
end $$;

alter table public.biblioteca_gifs enable row level security;

drop policy if exists "biblioteca_gifs_select_anon" on public.biblioteca_gifs;
create policy "biblioteca_gifs_select_anon"
  on public.biblioteca_gifs for select to anon, authenticated using (true);

drop policy if exists "biblioteca_gifs_write_authenticated" on public.biblioteca_gifs;
create policy "biblioteca_gifs_write_authenticated"
  on public.biblioteca_gifs for all to authenticated using (true) with check (true);

comment on table public.biblioteca_gifs is 'Biblioteca de GIFs/imagens para mensagens Discord';
