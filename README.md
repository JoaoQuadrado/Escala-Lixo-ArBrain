# Escala Lixo

Gestão de escala de limpeza — React + API C# + PostgreSQL (Supabase).

## Arquitetura

```
app/ (React + Vite)  →  EscalaLixo.Api  →  PostgreSQL (Supabase)
                              ↓
                         data/ (config Discord local)
```

- **Frontend:** `app/` — interface web/desktop (Electron opcional)
- **API:** `EscalaLixo.Api` — REST na porta 5000
- **Core:** `EscalaLixo.Core` — domínio, repositórios Postgres, validação
- **BD:** `supabase/migrations/` — schema e seeds SQL

## Pré-requisitos

- Node.js 20+
- .NET 10 SDK
- Projeto Supabase com PostgreSQL

## Configuração

1. Copie `EscalaLixo.Api/appsettings.Development.json.example` para `appsettings.Development.json` com a connection string do pooler (porta 5432).
2. Opcional: copie `app/.env.example` para `app/.env` se quiser URL explícita da API.

Ver detalhes em [supabase/README.md](supabase/README.md).

## Desenvolvimento

```bash
cd app
npm install
npm run dev:full
```

Isto arranca a API e o Vite em paralelo. Abra http://localhost:5173.

Comandos separados:

```bash
npm run api    # só API
npm run dev    # só frontend
```

## Build instalador (outra máquina)

Gera um `.exe` que instala o app **com a API embutida** — ao abrir o Escala Lixo, a API sobe sozinha.

```powershell
.\scripts\build-installer.ps1
```

O instalador fica em `app/release/`. Copie para a outra máquina e instale.

**Configuração na máquina instalada:** na primeira execução é criado:

`%APPDATA%\EscalaLixo\appsettings.json`

Edite a connection string do Supabase nesse ficheiro se o instalador foi gerado sem `appsettings.Development.json` local.

### Atalho rápido (desenvolvimento)

Duplo-clique em `Iniciar-EscalaLixo.bat` na raiz do projeto (sobe API + web).

Para janela desktop em dev:

```bash
cd app && npm run electron:full
```

## Build

```bash
dotnet build EsacalaLixo.sln
cd app && npm run build
```

## Dados

| O quê | Onde |
|-------|------|
| Colaboradores, escala, histórico | PostgreSQL (Supabase) |
| Config Discord/GIFs | `public.configuracao_app` |
| Migrations | `supabase/migrations/` |

A pasta `data/` só serve para importação única de `configuracao_app.json` legado, se existir.
