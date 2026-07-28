# Supabase — Escala Lixo

## 1. Criar projeto

1. Acesse [supabase.com](https://supabase.com) e crie um projeto
2. Anote a senha em **Settings → Database → Database password**

## 2. Configurar a API

Copie `EscalaLixo.Api/appsettings.Development.json.example` para `appsettings.Development.json`
e coloque a connection string do **Session pooler** (porta 5432):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.SEU_PROJETO;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

> **Nunca commite a senha.** `appsettings.Development.json` está no `.gitignore`.

### React (`app/.env`)

```env
VITE_API_URL=
```

O frontend fala só com a API C#.

## 3. Migrations (criar tabelas)

### Opção A — Automática (recomendado)

Ao iniciar a API, as migrations em `supabase/migrations/` são aplicadas automaticamente.

Também podes:
- Clicar **Aplicar migrations Supabase** em **Configurações** no app
- Chamar `POST http://localhost:5000/api/db/migrate`

### Opção B — SQL Editor manual

No **SQL Editor** do Supabase, execute na ordem os ficheiros em `supabase/migrations/`:

| Ficheiro | Conteúdo |
|----------|----------|
| `000_schema_migrations.sql` | Controlo de migrations |
| `001_colaboradores.sql` | Tabela `colaboradores` |
| `002_seed_colaboradores.sql` | Dados iniciais (opcional) |
| `003_escala_ativa.sql` | Escala semanal |
| `004_historico_pares.sql` | Histórico de duplas |
| `005_configuracao_app.sql` | Config Discord/GIFs |
| `006_escala_posicoes.sql` | Posições normalizadas |
| `007_mover_colaborador_atomico.sql` | Movimento atómico |
| `008_fila_espera_enum.sql` | Enum `fila_espera` (ficheiro separado — exigência Postgres) |
| `009_fila_espera_explicita.sql` | Slot `fila_espera` explícito + funções |

## 4. Verificar ligação

```bash
GET http://localhost:5000/api/db/status
GET http://localhost:5000/api/health
```

## 5. Rodar

```bash
cd app && npm run dev:full
```

## Comportamento

Todos os dados (colaboradores, escala, histórico, configurações Discord) ficam em **PostgreSQL**. A API não arranca sem connection string válida.

Configurações Discord/GIFs ficam na tabela `public.configuracao_app`.
