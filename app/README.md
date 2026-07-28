# Escala Lixo — Desktop App

Aplicação desktop para gerenciamento de escala de limpeza, inspirada visualmente em clientes modernos (League Client, Discord, Steam).

## Stack

- React 19 + TypeScript
- Vite
- TailwindCSS 4
- shadcn/ui (componentes customizados)
- Framer Motion
- @dnd-kit (drag and drop)
- Lucide Icons
- Electron (estrutura preparada para Tauri)

## Desenvolvimento

```bash
cd app
npm install
npm run dev          # Apenas web (http://localhost:5173)
npm run electron:dev # Electron + hot reload
```

## Build

```bash
npm run build
npm run electron:preview  # Testar build no Electron
npm run electron:build    # Gerar instalador
```

## Estrutura

```
src/
├── components/     # UI reutilizável + features
├── pages/          # Dashboard, Escalas, Funcionários...
├── layouts/        # AppLayout
├── hooks/          # useDashboardStats, etc.
├── contexts/       # Estado global + toasts + UI
├── services/       # Persistência (localStorage)
├── types/          # Tipagens TypeScript
├── utils/          # Helpers
└── assets/         # Imagens e recursos
```

## Funcionalidades

- Quadro Kanban com drag-and-drop por dia da semana
- CRUD de funcionários
- Dashboard com indicadores
- Painel lateral de detalhes
- Salvamento automático
- Duplicar escala
- Pesquisa e filtros
- Histórico de alterações

## Paleta

| Token | Cor |
|-------|-----|
| Fundo | `#111827` |
| Painéis | `#1F2937` |
| Destaque | `#FFC300` |
| Texto | Branco / Cinza claro |
