import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import { CalendarDays, ChevronRight, History, Loader2 } from 'lucide-react'
import { Card } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { HistoricoScheduleView } from '@/features/history/HistoricoScheduleView'
import {
  fetchHistoricoDetalhe,
  fetchHistoricoLista,
  type EscalaHistoricoDetalhe,
  type EscalaHistoricoResumo,
} from '@/services/api'
import { formatWeekRange } from '@/utils/weekDays'
import { cn } from '@/utils/cn'
import type { Schedule } from '@/types'

const MOTIVO_LABEL: Record<string, string> = {
  nova_semana: 'Nova semana',
  regeneracao: 'Regeneração',
}

function formatArchivedAt(iso: string) {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function mapDetalheToSchedule(detalhe: EscalaHistoricoDetalhe): Schedule {
  const s = detalhe.schedule!
  return {
    id: s.id,
    title: s.title,
    weekStart: s.weekStart,
    days: s.days.map((d) => ({ day: d.day, employeeIds: d.employeeIds })),
    waitingQueue: s.waitingQueue,
    blockedQueue: s.blockedQueue,
    createdAt: detalhe.archivedAt,
    updatedAt: detalhe.archivedAt,
  }
}

export function HistoricoPage() {
  const [lista, setLista] = useState<EscalaHistoricoResumo[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [detalhe, setDetalhe] = useState<EscalaHistoricoDetalhe | null>(null)
  const [loadingLista, setLoadingLista] = useState(true)
  const [loadingDetalhe, setLoadingDetalhe] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoadingLista(true)
    setErro(null)

    fetchHistoricoLista()
      .then((items) => {
        if (cancelled) return
        setLista(items)
        if (items.length > 0) {
          setSelectedId((prev) => prev ?? items[0].id)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setErro(err instanceof Error ? err.message : 'Erro ao carregar histórico')
        }
      })
      .finally(() => {
        if (!cancelled) setLoadingLista(false)
      })

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!selectedId) {
      setDetalhe(null)
      return
    }

    let cancelled = false
    setLoadingDetalhe(true)
    setErro(null)

    fetchHistoricoDetalhe(selectedId)
      .then((d) => {
        if (!cancelled) setDetalhe(d)
      })
      .catch((err) => {
        if (!cancelled) {
          setErro(err instanceof Error ? err.message : 'Erro ao carregar escala')
          setDetalhe(null)
        }
      })
      .finally(() => {
        if (!cancelled) setLoadingDetalhe(false)
      })

    return () => {
      cancelled = true
    }
  }, [selectedId])

  return (
    <div className="flex h-full min-h-0 flex-col gap-4 overflow-hidden">
      <div className="flex shrink-0 items-center justify-between">
        <div>
          <h2 className="flex items-center gap-2 text-lg font-semibold">
            <History className="h-5 w-5 text-accent" />
            Histórico de escalas
          </h2>
          <p className="text-sm text-text-secondary">
            Escalas arquivadas ao gerar uma nova semana
          </p>
        </div>
        {!loadingLista && (
          <span className="text-sm text-text-secondary">{lista.length} registros</span>
        )}
      </div>

      {erro && (
        <div className="rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {erro}
        </div>
      )}

      {loadingLista ? (
        <div className="flex flex-1 items-center justify-center text-text-secondary">
          <Loader2 className="mr-2 h-5 w-5 animate-spin" />
          Carregando histórico…
        </div>
      ) : lista.length === 0 ? (
        <div className="flex flex-1 flex-col items-center justify-center gap-2 text-text-secondary">
          <CalendarDays className="h-10 w-10 opacity-40" />
          <p className="text-sm">Nenhuma escala arquivada ainda.</p>
          <p className="text-xs opacity-70">
            Ao gerar uma nova escala, a anterior é salva aqui automaticamente.
          </p>
        </div>
      ) : (
        <div className="grid min-h-0 flex-1 grid-cols-1 gap-4 overflow-hidden lg:grid-cols-[280px_minmax(0,1fr)]">
          <div className="flex min-h-0 flex-col gap-2 overflow-y-auto pb-2 lg:max-h-full">
            {lista.map((item, i) => {
              const active = item.id === selectedId
              return (
                <motion.button
                  key={item.id}
                  type="button"
                  initial={{ opacity: 0, x: -8 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: i * 0.03 }}
                  onClick={() => setSelectedId(item.id)}
                  className={cn(
                    'w-full rounded-xl border px-3 py-3 text-left transition-colors cursor-pointer',
                    active
                      ? 'border-accent/40 bg-accent/10'
                      : 'border-border bg-bg-panel/40 hover:border-white/10 hover:bg-bg-panel/60',
                  )}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold text-text-primary">
                        Semana de {formatWeekRange(item.weekStart, 'long')}
                      </p>
                      <p className="mt-0.5 text-[11px] text-text-secondary">
                        {formatArchivedAt(item.archivedAt)}
                      </p>
                    </div>
                    <ChevronRight
                      className={cn(
                        'h-4 w-4 shrink-0 transition-transform',
                        active ? 'text-accent rotate-90 lg:rotate-0' : 'text-text-secondary/50',
                      )}
                    />
                  </div>
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    <Badge variant="default">{item.assignedCount} escalados</Badge>
                    <Badge variant="secondary">
                      {MOTIVO_LABEL[item.motivo] ?? item.motivo}
                    </Badge>
                  </div>
                </motion.button>
              )
            })}
          </div>

          <Card glass className="flex min-h-0 min-w-0 flex-col overflow-hidden p-4 lg:min-h-[420px]">
            {loadingDetalhe ? (
              <div className="flex flex-1 items-center justify-center text-text-secondary">
                <Loader2 className="mr-2 h-5 w-5 animate-spin" />
                Carregando escala…
              </div>
            ) : detalhe?.schedule ? (
              <>
                <div className="mb-3 shrink-0 border-b border-border pb-3">
                  <h3 className="text-base font-semibold">
                    Semana de {formatWeekRange(detalhe.weekStart, 'long')}
                  </h3>
                  <p className="text-xs text-text-secondary">
                    Arquivada em {formatArchivedAt(detalhe.archivedAt)} ·{' '}
                    {MOTIVO_LABEL[detalhe.motivo] ?? detalhe.motivo}
                  </p>
                </div>
                <div className="min-h-0 flex-1 overflow-hidden">
                  <HistoricoScheduleView
                    schedule={mapDetalheToSchedule(detalhe)}
                    employees={detalhe.employees}
                  />
                </div>
              </>
            ) : (
              <div className="flex flex-1 items-center justify-center text-sm text-text-secondary">
                Selecione um registro para ver a escala
              </div>
            )}
          </Card>
        </div>
      )}
    </div>
  )
}
