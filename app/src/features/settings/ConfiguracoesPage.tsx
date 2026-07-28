import { useCallback, useEffect, useRef, useState } from 'react'
import { motion } from 'framer-motion'
import {
  Clock,
  Loader2,
  MessageSquare,
  Save,
  Trash2,
  Upload,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { DiscordIcon } from '@/components/ui/DiscordIcon'
import { Input, Label, Select, Textarea } from '@/components/ui/Input'
import { Badge } from '@/components/ui/Badge'
import {
  DiscordSendConfirmDialog,
  useDiscordSendConfirm,
  type DiscordSendTipo,
} from '@/components/discord/DiscordSendConfirmDialog'
import { useApp, useToast, ApiError } from '@/contexts/AppContext'
import {
  deleteGifFromLibrary,
  fetchConfig,
  fetchGifLibrary,
  gifLibraryUrl,
  saveConfigApi,
  selectGifApi,
  uploadGifToLibrary,
} from '@/services/api'
import type { AppConfig, AppConfigSave, GifLibraryItem } from '@/types/config'
import { DEFAULT_DAILY_MESSAGE } from '@/types/config'

const HOURS = Array.from({ length: 24 }, (_, i) => i)
const MAX_GIF_MB = 8

function GifLibraryPanel({
  gifs,
  gifPreviaId,
  gifDiarioId,
  cacheKey,
  uploading,
  selecting,
  onUpload,
  onDelete,
  onSelectPrevia,
  onSelectDiario,
}: {
  gifs: GifLibraryItem[]
  gifPreviaId: string | null
  gifDiarioId: string | null
  cacheKey: number
  uploading: boolean
  selecting: string | null
  onUpload: (file: File) => void
  onDelete: (id: string) => void
  onSelectPrevia: (id: string) => void
  onSelectDiario: (id: string) => void
}) {
  const inputRef = useRef<HTMLInputElement>(null)

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) onUpload(file)
    e.target.value = ''
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <input
          ref={inputRef}
          type="file"
          accept="image/gif,image/png,image/jpeg,image/webp"
          className="hidden"
          onChange={handleChange}
        />
        <Button
          type="button"
          variant="secondary"
          size="sm"
          disabled={uploading}
          onClick={() => inputRef.current?.click()}
        >
          {uploading ? (
            <Loader2 className="h-3.5 w-3.5 animate-spin mr-1" />
          ) : (
            <Upload className="h-3.5 w-3.5 mr-1" />
          )}
          Adicionar à biblioteca
        </Button>
        <span className="text-[11px] text-text-secondary/70">
          Até {MAX_GIF_MB} MB · GIF, PNG, JPEG ou WebP
        </span>
      </div>

      {gifs.length === 0 ? (
        <p className="rounded-lg border border-dashed border-border/80 py-8 text-center text-sm text-text-secondary">
          Nenhum GIF na biblioteca. Adicione arquivos para usar nas mensagens Discord.
        </p>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2">
          {gifs.map((gif) => {
            const isSemana = gifPreviaId === gif.id
            const isHoje = gifDiarioId === gif.id
            const busy = selecting === gif.id

            return (
              <div
                key={gif.id}
                className={`rounded-xl border p-3 transition-colors ${
                  isSemana || isHoje
                    ? 'border-accent/40 bg-accent/5'
                    : 'border-border bg-bg-panel/40'
                }`}
              >
                <img
                  src={gifLibraryUrl(gif.id, cacheKey)}
                  alt={gif.nome}
                  className="mb-2 h-28 w-full rounded-lg border border-border/60 object-contain bg-black/20"
                />
                <p className="truncate text-sm font-medium text-text-primary">{gif.nome}</p>
                <div className="mt-1 flex flex-wrap gap-1">
                  {isSemana && <Badge variant="accent" className="text-[10px]">Escala semana</Badge>}
                  {isHoje && <Badge variant="secondary" className="text-[10px]">Escala hoje</Badge>}
                </div>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  <Button
                    type="button"
                    variant={isSemana ? 'default' : 'secondary'}
                    size="sm"
                    className="h-7 text-[11px] px-2"
                    disabled={busy || uploading}
                    onClick={() => onSelectPrevia(gif.id)}
                  >
                    {busy ? <Loader2 className="h-3 w-3 animate-spin" /> : 'Semana'}
                  </Button>
                  <Button
                    type="button"
                    variant={isHoje ? 'default' : 'secondary'}
                    size="sm"
                    className="h-7 text-[11px] px-2"
                    disabled={busy || uploading}
                    onClick={() => onSelectDiario(gif.id)}
                  >
                    {busy ? <Loader2 className="h-3 w-3 animate-spin" /> : 'Hoje'}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 text-[11px] px-2 text-danger hover:text-danger ml-auto"
                    disabled={busy || uploading}
                    onClick={() => onDelete(gif.id)}
                  >
                    <Trash2 className="h-3 w-3" />
                  </Button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

function configToForm(cfg: AppConfig): AppConfigSave {
  return {
    webhookDiscord: '',
    tokenBotDiscord: '',
    idServidorDiscord: cfg.idServidorDiscord,
    modeloMensagemDiaria: cfg.modeloMensagemDiaria,
    intervaloVerificacaoMinutos: cfg.intervaloVerificacaoMinutos,
    horaNotificacaoPadrao: cfg.horaNotificacaoPadrao,
    horaPreviaSemanal: cfg.horaPreviaSemanal,
    horaLembreteDiario: cfg.horaLembreteDiario,
  }
}

export function ConfiguracoesPage() {
  const { apiConnected, enviarDiscordDia, enviarDiscordPrevia } = useApp()
  const { showToast } = useToast()

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [selecting, setSelecting] = useState<string | null>(null)
  const { pendingTipo, requestSend, closeConfirm } = useDiscordSendConfirm()
  const [gifCache, setGifCache] = useState(0)
  const [gifs, setGifs] = useState<GifLibraryItem[]>([])
  const [meta, setMeta] = useState<Pick<AppConfig, 'webhookConfigured' | 'gifPreviaConfigured' | 'gifDiarioConfigured' | 'gifPreviaId' | 'gifDiarioId'> | null>(null)
  const [form, setForm] = useState<AppConfigSave>({
    webhookDiscord: '',
    tokenBotDiscord: '',
    idServidorDiscord: '',
    modeloMensagemDiaria: '',
    intervaloVerificacaoMinutos: 60,
    horaNotificacaoPadrao: 8,
    horaPreviaSemanal: 8,
    horaLembreteDiario: 17,
  })

  const applyConfig = useCallback((cfg: AppConfig) => {
    setForm(configToForm(cfg))
    setMeta({
      webhookConfigured: cfg.webhookConfigured,
      gifPreviaConfigured: cfg.gifPreviaConfigured,
      gifDiarioConfigured: cfg.gifDiarioConfigured,
      gifPreviaId: cfg.gifPreviaId,
      gifDiarioId: cfg.gifDiarioId,
    })
  }, [])

  const loadGifs = useCallback(async () => {
    const lista = await fetchGifLibrary()
    setGifs(lista)
  }, [])

  const load = useCallback(async () => {
    if (!apiConnected) {
      setLoading(false)
      return
    }
    setLoading(true)
    try {
      const [cfg] = await Promise.all([fetchConfig(), loadGifs()])
      applyConfig(cfg)
    } catch (err) {
      showToast('error', err instanceof ApiError ? err.message : 'Erro ao carregar configurações')
    } finally {
      setLoading(false)
    }
  }, [apiConnected, applyConfig, loadGifs, showToast])

  useEffect(() => {
    load()
  }, [load])

  const update = <K extends keyof AppConfigSave>(key: K, value: AppConfigSave[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  const handleSave = async () => {
    if (!apiConnected) return
    setSaving(true)
    try {
      const saved = await saveConfigApi(form)
      applyConfig(saved)
      showToast('success', 'Configurações salvas')
    } catch (err) {
      showToast('error', err instanceof ApiError ? err.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  const handleUpload = async (file: File) => {
    if (file.size > MAX_GIF_MB * 1024 * 1024) {
      showToast('error', `Arquivo muito grande (máximo ${MAX_GIF_MB} MB)`)
      return
    }
    setUploading(true)
    try {
      const saved = await uploadGifToLibrary(file, file.name.replace(/\.[^.]+$/, ''))
      applyConfig(saved)
      await loadGifs()
      setGifCache(Date.now())
      showToast('success', 'GIF adicionado à biblioteca')
    } catch (err) {
      showToast('error', err instanceof ApiError ? err.message : 'Erro ao enviar GIF')
    } finally {
      setUploading(false)
    }
  }

  const handleDelete = async (id: string) => {
    setSelecting(id)
    try {
      const saved = await deleteGifFromLibrary(id)
      applyConfig(saved)
      await loadGifs()
      setGifCache(Date.now())
      showToast('success', 'GIF removido da biblioteca')
    } catch (err) {
      showToast('error', err instanceof ApiError ? err.message : 'Erro ao remover GIF')
    } finally {
      setSelecting(null)
    }
  }

  const handleSelectPrevia = async (id: string) => {
    setSelecting(id)
    try {
      const saved = await selectGifApi({ gifPreviaId: id })
      applyConfig(saved)
      setGifCache(Date.now())
      showToast('success', 'GIF selecionado para escala semana')
    } catch (err) {
      showToast('error', err instanceof ApiError ? err.message : 'Erro ao selecionar GIF')
    } finally {
      setSelecting(null)
    }
  }

  const handleSelectDiario = async (id: string) => {
    setSelecting(id)
    try {
      const saved = await selectGifApi({ gifDiarioId: id })
      applyConfig(saved)
      setGifCache(Date.now())
      showToast('success', 'GIF selecionado para escala hoje')
    } catch (err) {
      showToast('error', err instanceof ApiError ? err.message : 'Erro ao selecionar GIF')
    } finally {
      setSelecting(null)
    }
  }

  const handleDiscordConfirm = async (tipo: DiscordSendTipo) =>
    tipo === 'dia' ? enviarDiscordDia() : enviarDiscordPrevia()

  if (!apiConnected) {
    return (
      <div className="mx-auto flex h-full w-full max-w-3xl flex-col gap-4">
        <div>
          <h2 className="text-lg font-semibold">Configurações</h2>
          <p className="text-sm text-text-secondary">Discord, GIFs e agendamento</p>
        </div>
        <Card glass>
          <CardContent className="py-8 text-center text-sm text-text-secondary">
            A API não está conectada. Inicie o backend com{' '}
            <code className="text-accent/80">npm run dev:full</code> para editar as configurações.
          </CardContent>
        </Card>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center text-text-secondary">
        <Loader2 className="h-5 w-5 animate-spin mr-2" />
        Carregando configurações…
      </div>
    )
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-5 pb-8">
      <div className="flex items-start justify-between gap-4 border-b border-border/60 pb-4">
        <div className="min-w-0">
          <h2 className="text-lg font-semibold">Configurações</h2>
          <p className="text-sm text-text-secondary">
            Discord, biblioteca de GIFs e horários
          </p>
        </div>
        <Button onClick={handleSave} disabled={saving} className="shrink-0">
          {saving ? <Loader2 className="h-4 w-4 animate-spin mr-1.5" /> : <Save className="h-4 w-4 mr-1.5" />}
          Salvar
        </Button>
      </div>

      <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
        <Card glass>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <DiscordIcon className="h-4 w-4" />
              Discord
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="webhook">Webhook URL</Label>
              <Input
                id="webhook"
                type="password"
                placeholder={meta?.webhookConfigured ? 'Configurado — deixe vazio para manter' : 'https://discord.com/api/webhooks/...'}
                value={form.webhookDiscord}
                onChange={(e) => update('webhookDiscord', e.target.value)}
              />
              {meta?.webhookConfigured && !form.webhookDiscord && (
                <Badge variant="accent" className="text-[10px]">Webhook ativo</Badge>
              )}
            </div>

            <p className="text-xs text-text-secondary/70">
              Menções usam o ID Discord cadastrado em cada colaborador.
            </p>

            <div className="flex gap-2 pt-1">
              <Button
                variant="secondary"
                size="sm"
                disabled={!meta?.webhookConfigured}
                onClick={() => requestSend('previa')}
              >
                <DiscordIcon className="h-3.5 w-3.5 mr-1" />
                Testar escala semana
              </Button>
              <Button
                variant="secondary"
                size="sm"
                disabled={!meta?.webhookConfigured}
                onClick={() => requestSend('dia')}
              >
                <DiscordIcon className="h-3.5 w-3.5 mr-1" />
                Testar escala hoje
              </Button>
            </div>
          </CardContent>
        </Card>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.05 }}>
        <Card glass>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MessageSquare className="h-4 w-4 text-accent" />
              Mensagens e GIFs
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-5">
            <GifLibraryPanel
              gifs={gifs}
              gifPreviaId={meta?.gifPreviaId ?? null}
              gifDiarioId={meta?.gifDiarioId ?? null}
              cacheKey={gifCache}
              uploading={uploading}
              selecting={selecting}
              onUpload={handleUpload}
              onDelete={handleDelete}
              onSelectPrevia={handleSelectPrevia}
              onSelectDiario={handleSelectDiario}
            />

            <div className="space-y-1.5 border-t border-border/60 pt-4">
              <div className="flex items-center justify-between">
                <Label htmlFor="modelo">Modelo da mensagem diária</Label>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-6 text-[10px] px-2"
                  onClick={() => update('modeloMensagemDiaria', DEFAULT_DAILY_MESSAGE)}
                >
                  Restaurar padrão
                </Button>
              </div>
              <Textarea
                id="modelo"
                rows={8}
                placeholder={DEFAULT_DAILY_MESSAGE}
                value={form.modeloMensagemDiaria}
                onChange={(e) => update('modeloMensagemDiaria', e.target.value)}
                className="font-mono text-xs leading-relaxed"
              />
            </div>
          </CardContent>
        </Card>
      </motion.div>

      <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: 0.1 }}>
        <Card glass>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Clock className="h-4 w-4 text-accent" />
              Agendamento automático
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-xs text-text-secondary/70">
              A API verifica a cada intervalo configurado. Lembrete diário (seg–sex) usa{' '}
              <strong className="text-text-secondary">Lembrete diário</strong>; prévia semanal usa{' '}
              <strong className="text-text-secondary">Prévia semanal — segunda</strong>. Requer webhook
              Discord activo e escala válida.
            </p>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="min-w-0 space-y-1.5">
                <Label htmlFor="intervalo">Intervalo de verificação (min)</Label>
                <Input
                  id="intervalo"
                  type="number"
                  min={1}
                  max={1440}
                  value={form.intervaloVerificacaoMinutos}
                  onChange={(e) => update('intervaloVerificacaoMinutos', Number(e.target.value))}
                />
              </div>
              <div className="min-w-0 space-y-1.5">
                <Label htmlFor="hora-padrao">Hora padrão (reserva)</Label>
                <Select
                  id="hora-padrao"
                  value={form.horaNotificacaoPadrao}
                  onChange={(e) => update('horaNotificacaoPadrao', Number(e.target.value))}
                >
                  {HOURS.map((h) => (
                    <option key={h} value={h}>{String(h).padStart(2, '0')}:00</option>
                  ))}
                </Select>
              </div>
              <div className="min-w-0 space-y-1.5">
                <Label htmlFor="hora-previa">Prévia semanal — segunda</Label>
                <Select
                  id="hora-previa"
                  value={form.horaPreviaSemanal}
                  onChange={(e) => update('horaPreviaSemanal', Number(e.target.value))}
                >
                  {HOURS.map((h) => (
                    <option key={h} value={h}>{String(h).padStart(2, '0')}:00</option>
                  ))}
                </Select>
              </div>
              <div className="min-w-0 space-y-1.5">
                <Label htmlFor="hora-diario">Lembrete diário — seg a sex</Label>
                <Select
                  id="hora-diario"
                  value={form.horaLembreteDiario}
                  onChange={(e) => update('horaLembreteDiario', Number(e.target.value))}
                >
                  {HOURS.map((h) => (
                    <option key={h} value={h}>{String(h).padStart(2, '0')}:00</option>
                  ))}
                </Select>
              </div>
            </div>
          </CardContent>
        </Card>
      </motion.div>

      <DiscordSendConfirmDialog
        tipo={pendingTipo}
        onOpenChange={(open) => {
          if (!open) closeConfirm()
        }}
        onConfirm={handleDiscordConfirm}
        onSuccess={(msg) => showToast('success', msg)}
        onError={(msg) => showToast('error', msg)}
      />
    </div>
  )
}
