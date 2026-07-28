-- GIFs armazenados como arquivo (bytea) em vez de URL externa
alter table public.configuracao_app
  add column if not exists gif_previa_semanal bytea,
  add column if not exists gif_previa_mime text not null default '',
  add column if not exists gif_diario bytea,
  add column if not exists gif_diario_mime text not null default '';

comment on column public.configuracao_app.gif_previa_semanal is 'GIF da prévia semanal (binário)';
comment on column public.configuracao_app.gif_diario is 'GIF do lembrete diário (binário)';
