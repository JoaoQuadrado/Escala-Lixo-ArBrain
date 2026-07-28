import type { AppState, Employee, ScheduleColumn, WeekDay } from '@/types'
import type { AppConfig, AppConfigSave, GifLibraryItem } from '@/types/config'

const API_BASE = import.meta.env.VITE_API_URL ?? ''

export interface ApiValidation {
  valid: boolean
  errors: string[]
  warnings: string[]
}

export class ApiError extends Error {
  validation?: ApiValidation

  constructor(message: string, validation?: ApiValidation) {
    super(message)
    this.name = 'ApiError'
    this.validation = validation
  }
}

interface ApiEstadoRaw {
  employees: Employee[]
  schedule: {
    id: string
    title: string
    weekStart: string
    days: { day: string; employeeIds: string[] }[]
    waitingQueue?: string[]
    blockedQueue?: string[]
    updatedAt?: string
  } | null
  validation: ApiValidation | null
  scheduleHash: string | null
}

interface ApiErrorResponse {
  message: string
  validation?: ApiValidation
}

let scheduleHash: string | null = null

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  })

  if (!res.ok) {
    const body = (await res.json().catch(() => ({}))) as ApiErrorResponse
    throw new ApiError(body.message ?? `Erro ${res.status}`, body.validation)
  }

  return res.json() as Promise<T>
}

function mapSchedule(raw: ApiEstadoRaw['schedule']) {
  if (!raw) return null
  return {
    id: raw.id,
    title: raw.title,
    weekStart: raw.weekStart,
    days: raw.days
      .filter((d) => d.day !== 'saturday' && d.day !== 'sunday' && d.day !== 'waiting')
      .map((d) => ({
        day: d.day as WeekDay,
        employeeIds: d.employeeIds,
      })),
    waitingQueue: raw.waitingQueue ?? [],
    blockedQueue: raw.blockedQueue ?? [],
    createdAt: raw.updatedAt ?? new Date().toISOString(),
    updatedAt: raw.updatedAt ?? new Date().toISOString(),
  }
}

function mapEstado(raw: ApiEstadoRaw): AppState {
  scheduleHash = raw.scheduleHash
  const schedule = mapSchedule(raw.schedule)
  return {
    employees: raw.employees,
    schedules: schedule ? [schedule] : [],
    activeScheduleId: schedule?.id ?? null,
  }
}

export async function fetchEstado(): Promise<AppState> {
  const raw = await request<ApiEstadoRaw>('/api/estado')
  return mapEstado(raw)
}

export async function moveEmployeeApi(
  employeeId: string,
  fromDay: ScheduleColumn,
  toDay: ScheduleColumn,
  toIndex?: number,
): Promise<AppState> {
  const raw = await request<ApiEstadoRaw>('/api/escala/mover', {
    method: 'POST',
    body: JSON.stringify({
      employeeId,
      fromDay,
      toDay,
      toIndex,
      expectedHash: scheduleHash,
    }),
  })
  return mapEstado(raw)
}

export async function swapEmployeesApi(
  employeeIdA: string,
  fromDayA: ScheduleColumn,
  employeeIdB: string,
  fromDayB: ScheduleColumn,
): Promise<AppState> {
  const raw = await request<ApiEstadoRaw>('/api/escala/trocar', {
    method: 'POST',
    body: JSON.stringify({
      employeeIdA,
      fromDayA,
      employeeIdB,
      fromDayB,
      expectedHash: scheduleHash,
    }),
  })
  return mapEstado(raw)
}

export async function createColaboradorApi(data: Omit<Employee, 'id'>): Promise<AppState> {
  const raw = await request<ApiEstadoRaw>('/api/colaboradores', {
    method: 'POST',
    body: JSON.stringify(employeeToApi(data)),
  })
  return mapEstado(raw)
}

export async function updateColaboradorApi(employee: Employee): Promise<AppState> {
  const raw = await request<ApiEstadoRaw>(`/api/colaboradores/${employee.id}`, {
    method: 'PUT',
    body: JSON.stringify(employeeToApi(employee)),
  })
  return mapEstado(raw)
}

export async function deleteColaboradorApi(id: string): Promise<AppState> {
  const raw = await request<ApiEstadoRaw>(`/api/colaboradores/${id}`, {
    method: 'DELETE',
  })
  return mapEstado(raw)
}

function employeeToApi(e: Omit<Employee, 'id'> | Employee) {
  return {
    name: e.name,
    discordUser: e.discordUser,
    role: e.role,
    color: e.color,
    photoUrl: e.photoUrl,
    onVacation: e.onVacation,
    absent: e.absent,
    notes: e.notes,
  }
}

export async function gerarEscalaApi(): Promise<AppState> {
  const raw = await request<ApiEstadoRaw>('/api/escala/gerar', { method: 'POST' })
  return mapEstado(raw)
}

export async function discordDiaApi(): Promise<string> {
  const res = await request<{ message: string }>('/api/discord/dia', { method: 'POST' })
  return res.message
}

export async function discordPreviaApi(): Promise<string> {
  const res = await request<{ message: string }>('/api/discord/previa', { method: 'POST' })
  return res.message
}

export async function checkApiHealth(): Promise<boolean> {
  try {
    const res = await fetch(`${API_BASE}/api/health`)
    return res.ok
  } catch {
    return false
  }
}

interface ApiConfigRaw {
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

function mapConfig(raw: ApiConfigRaw): AppConfig {
  return {
    webhookDiscord: raw.webhookDiscord,
    webhookConfigured: raw.webhookConfigured,
    tokenBotDiscord: raw.tokenBotDiscord,
    tokenBotConfigured: raw.tokenBotConfigured,
    idServidorDiscord: raw.idServidorDiscord,
    gifPreviaConfigured: raw.gifPreviaConfigured,
    gifDiarioConfigured: raw.gifDiarioConfigured,
    gifPreviaId: raw.gifPreviaId ?? null,
    gifDiarioId: raw.gifDiarioId ?? null,
    modeloMensagemDiaria: raw.modeloMensagemDiaria,
    intervaloVerificacaoMinutos: raw.intervaloVerificacaoMinutos,
    horaNotificacaoPadrao: raw.horaNotificacaoPadrao,
    horaPreviaSemanal: raw.horaPreviaSemanal,
    horaLembreteDiario: raw.horaLembreteDiario,
    pastaDados: raw.pastaDados,
    colaboradoresFonte: raw.colaboradoresFonte,
    postgresConfigured: raw.postgresConfigured,
    caminhoConfiguracao: raw.caminhoConfiguracao,
  }
}

function configToApi(body: AppConfigSave) {
  return {
    webhookDiscord: body.webhookDiscord,
    tokenBotDiscord: body.tokenBotDiscord,
    idServidorDiscord: body.idServidorDiscord,
    urlGifPreviaSemanal: '',
    urlGifDiario: '',
    modeloMensagemDiaria: body.modeloMensagemDiaria,
    intervaloVerificacaoMinutos: body.intervaloVerificacaoMinutos,
    horaNotificacaoPadrao: body.horaNotificacaoPadrao,
    horaPreviaSemanal: body.horaPreviaSemanal,
    horaLembreteDiario: body.horaLembreteDiario,
  }
}

export function gifLibraryUrl(id: string, cacheKey?: number): string {
  const base = `${API_BASE}/api/gifs/${id}`
  return cacheKey ? `${base}?v=${cacheKey}` : base
}

export function gifPreviaUrl(cacheKey?: number): string {
  const base = `${API_BASE}/api/config/gif/previa`
  return cacheKey ? `${base}?v=${cacheKey}` : base
}

export function gifDiarioUrl(cacheKey?: number): string {
  const base = `${API_BASE}/api/config/gif/dia`
  return cacheKey ? `${base}?v=${cacheKey}` : base
}

export async function fetchGifLibrary(): Promise<GifLibraryItem[]> {
  return request<GifLibraryItem[]>('/api/gifs')
}

export async function uploadGifToLibrary(file: File, nome?: string): Promise<AppConfig> {
  const form = new FormData()
  form.append('file', file)
  if (nome?.trim()) form.append('nome', nome.trim())
  const res = await fetch(`${API_BASE}/api/gifs`, {
    method: 'POST',
    body: form,
  })
  if (!res.ok) {
    const body = (await res.json().catch(() => ({}))) as ApiErrorResponse
    const msg = res.status === 404
      ? 'Endpoint de GIFs não encontrado — reinicie a API (npm run dev:full).'
      : (body.message ?? `Erro ${res.status}`)
    throw new ApiError(msg, body.validation)
  }
  const raw = (await res.json()) as ApiConfigRaw
  return mapConfig(raw)
}

export async function deleteGifFromLibrary(id: string): Promise<AppConfig> {
  const raw = await request<ApiConfigRaw>(`/api/gifs/${id}`, { method: 'DELETE' })
  return mapConfig(raw)
}

export async function selectGifApi(body: {
  gifPreviaId?: string | null
  gifDiarioId?: string | null
}): Promise<AppConfig> {
  const payload: Record<string, string | null> = {}
  if (body.gifPreviaId !== undefined) payload.gifPreviaId = body.gifPreviaId
  if (body.gifDiarioId !== undefined) payload.gifDiarioId = body.gifDiarioId
  const raw = await request<ApiConfigRaw>('/api/config/gif-selecao', {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
  return mapConfig(raw)
}

export async function fetchConfig(): Promise<AppConfig> {
  const raw = await request<ApiConfigRaw>('/api/config')
  return mapConfig(raw)
}

export async function saveConfigApi(body: AppConfigSave): Promise<AppConfig> {
  const raw = await request<ApiConfigRaw>('/api/config', {
    method: 'PUT',
    body: JSON.stringify(configToApi(body)),
  })
  return mapConfig(raw)
}

export interface DbStatus {
  connected: boolean
  erro?: string
  colaboradores: number
  migrationsAplicadas: string[]
  tabelas: string[]
  pronto: boolean
}

export async function fetchDbStatus(): Promise<DbStatus> {
  const raw = await request<{
    connected: boolean
    erro?: string
    colaboradores: number
    migrationsAplicadas: string[]
    tabelas: string[]
    pronto: boolean
  }>('/api/db/status')
  return {
    connected: raw.connected,
    erro: raw.erro,
    colaboradores: raw.colaboradores,
    migrationsAplicadas: raw.migrationsAplicadas ?? [],
    tabelas: raw.tabelas ?? [],
    pronto: raw.pronto,
  }
}

export async function runMigrationsApi(): Promise<{ message: string; aplicadas: string[] }> {
  return request('/api/db/migrate', { method: 'POST' })
}

export interface EscalaHistoricoResumo {
  id: string
  weekStart: string
  archivedAt: string
  motivo: string
  assignedCount: number
}

export interface EscalaHistoricoDetalhe {
  id: string
  weekStart: string
  archivedAt: string
  motivo: string
  employees: Employee[]
  schedule: {
    id: string
    title: string
    weekStart: string
    days: { day: WeekDay; employeeIds: string[] }[]
    waitingQueue: string[]
    blockedQueue: string[]
  } | null
}

export async function fetchHistoricoLista(limit = 50): Promise<EscalaHistoricoResumo[]> {
  try {
    return await request<EscalaHistoricoResumo[]>(`/api/escala/historico?limit=${limit}`)
  } catch (err) {
    if (err instanceof ApiError && err.message === 'Erro 404') {
      throw new ApiError(
        'Endpoint de histórico não encontrado — reinicie a API (npm run dev:full ou dotnet run).',
      )
    }
    throw err
  }
}

export async function fetchHistoricoDetalhe(id: string): Promise<EscalaHistoricoDetalhe> {
  const raw = await request<{
    id: string
    weekStart: string
    archivedAt: string
    motivo: string
    employees: Employee[]
    schedule: {
      id: string
      title: string
      weekStart: string
      days: { day: string; employeeIds: string[] }[]
      waitingQueue: string[]
      blockedQueue: string[]
    } | null
  }>(`/api/escala/historico/${id}`)

  return {
    ...raw,
    schedule: raw.schedule
      ? {
          ...raw.schedule,
          days: raw.schedule.days
            .filter((d) => d.day !== 'waiting' && d.day !== 'blocked')
            .map((d) => ({
              day: d.day as WeekDay,
              employeeIds: d.employeeIds,
            })),
        }
      : null,
  }
}

export interface RotacaoPainel {
  summary: {
    totalColaboradores: number
    vagasPorSemana: number
    deForaProxima: number
    repetidosNecessarios: number
    limiteSemanasConsecutivas: number
  }
  employees: {
    id: string
    name: string
    color: string
    streak: number
    status: string
    canRepeatNext: boolean
    blockedRepeat: boolean
  }[]
  nextWeek: {
    newEntries: string[]
    waitingOutside: string[]
    repeatNeeded: number
    canRepeat: string[]
    blockedRepeat: string[]
  }
  simulation: {
    index: number
    weekStart: string
    outsideCount: number
    repeatNeeded: number
    newNames: string[]
    repeatNames: string[]
    assignedNames: string[]
    streaksAfter: Record<string, number>
  }[]
}

export async function fetchRotacaoPainel(semanas = 4): Promise<RotacaoPainel> {
  try {
    return await request<RotacaoPainel>(`/api/escala/rotacao?semanas=${semanas}`)
  } catch (err) {
    if (err instanceof ApiError && err.message === 'Erro 404') {
      throw new ApiError(
        'Endpoint de rotação não encontrado — reinicie a API (npm run dev:full ou dotnet run).',
      )
    }
    throw err
  }
}
