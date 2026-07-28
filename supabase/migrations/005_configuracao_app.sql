-- Configurações da app (Discord, GIFs, horários)
create table if not exists public.configuracao_app (
  id int primary key default 1 check (id = 1),
  webhook_discord text not null default '',
  token_bot_discord text not null default '',
  id_servidor_discord text not null default '',
  url_gif_previa_semanal text not null default '',
  url_gif_diario text not null default '',
  modelo_mensagem_diaria text not null default '',
  intervalo_verificacao_minutos int not null default 60,
  hora_notificacao_padrao int not null default 8 check (hora_notificacao_padrao between 0 and 23),
  hora_previa_semanal int not null default 8 check (hora_previa_semanal between 0 and 23),
  hora_lembrete_diario int not null default 17 check (hora_lembrete_diario between 0 and 23),
  updated_at timestamptz not null default now()
);

drop trigger if exists configuracao_app_updated_at on public.configuracao_app;
create trigger configuracao_app_updated_at
  before update on public.configuracao_app
  for each row execute function public.set_updated_at();

alter table public.configuracao_app enable row level security;

create policy "configuracao_app_select_anon"
  on public.configuracao_app for select to anon, authenticated using (true);

create policy "configuracao_app_write_authenticated"
  on public.configuracao_app for all to authenticated using (true) with check (true);

comment on table public.configuracao_app is 'Configurações Discord, mensagens e agendamento';
