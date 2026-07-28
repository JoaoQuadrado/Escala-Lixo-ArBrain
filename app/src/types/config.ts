export interface AppConfig {
  webhookDiscord: string
  webhookConfigured: boolean
  tokenBotDiscord: string
  tokenBotConfigured: boolean
  idServidorDiscord: string
  gifPreviaConfigured: boolean
  gifDiarioConfigured: boolean
  gifPreviaId: string | null
  gifDiarioId: string | null
  modeloMensagemDiaria: string
  intervaloVerificacaoMinutos: number
  horaNotificacaoPadrao: number
  horaPreviaSemanal: number
  horaLembreteDiario: number
  pastaDados: string
  colaboradoresFonte: string
  postgresConfigured: boolean
  caminhoConfiguracao: string
}

export interface GifLibraryItem {
  id: string
  nome: string
  mime: string
  criadoEm: string
}

export type AppConfigSave = Pick<
  AppConfig,
  | 'webhookDiscord'
  | 'tokenBotDiscord'
  | 'idServidorDiscord'
  | 'modeloMensagemDiaria'
  | 'intervaloVerificacaoMinutos'
  | 'horaNotificacaoPadrao'
  | 'horaPreviaSemanal'
  | 'horaLembreteDiario'
>

export const DEFAULT_DAILY_MESSAGE = `📌 **Lembrete do dia — equipe do lixo**

🗑️ Coleta após 17h30 (banheiros, cozinha orgânico/reciclável, descarte -1).
☕ Pia, escorredor e cafeteiras na cozinha.

👥 **Dupla de hoje:** {dupla}

_Obrigado por manter o espaço em ordem!_`
