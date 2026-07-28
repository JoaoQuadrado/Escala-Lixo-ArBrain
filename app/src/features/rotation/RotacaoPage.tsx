import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import {
  ArrowRight,
  Loader2,
  RefreshCw,
  Repeat,
  UserCheck,
  UserMinus,
  Users,
  Workflow,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { Avatar } from '@/components/ui/Avatar'
import { Button } from '@/components/ui/Button'
import { fetchRotacaoPainel, type RotacaoPainel } from '@/services/api'
import { formatWeekRange } from '@/utils/weekDays'
import { cn } from '@/utils/cn'

const STATUS_LABEL: Record<string, string> = {
  escalado: 'Na escala',
  fila: 'Fila de espera',
  missao: 'Missão cumprida',
  fora: 'De fora',
  sem_escala: 'Sem escala',
}

const STATUS_VARIANT: Record<string, 'accent' | 'default' | 'secondary' | 'warning' | 'success'> = {
  escalado: 'accent',
  fila: 'warning',
  missao: 'success',
  fora: 'secondary',
  sem_escala: 'default',
}

function NameChip({ name, variant = 'default' }: { name: string; variant?: 'new' | 'repeat' | 'default' }) {
  return (
    <span
      className={cn(
        'inline-flex rounded-md border px-2 py-0.5 text-[11px] font-medium',
        variant === 'new' && 'border-sky-500/30 bg-sky-500/10 text-sky-200',
        variant === 'repeat' && 'border-amber-500/30 bg-amber-500/10 text-amber-200',
        variant === 'default' && 'border-border bg-bg-panel/60 text-text-primary',
      )}
    >
      {name}
    </span>
  )
}

export function RotacaoPage() {
  const [data, setData] = useState<RotacaoPainel | null>(null)
  const [loading, setLoading] = useState(true)
  const [erro, setErro] = useState<string | null>(null)

  const load = async () => {
    setLoading(true)
    setErro(null)
    try {
      setData(await fetchRotacaoPainel(4))
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao carregar rotação')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center text-text-secondary">
        <Loader2 className="mr-2 h-5 w-5 animate-spin" />
        Carregando rotação…
      </div>
    )
  }

  if (erro || !data) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-3 text-text-secondary">
        <p>{erro ?? 'Dados indisponíveis'}</p>
        <Button variant="secondary" size="sm" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4 mr-1" />
          Tentar novamente
        </Button>
      </div>
    )
  }

  const { summary, nextWeek, employees, simulation } = data

  return (
    <div className="mx-auto flex h-full min-h-0 w-full max-w-6xl flex-col gap-5 overflow-y-auto pb-8">
      <div className="flex shrink-0 items-start justify-between gap-4">
        <div>
          <h2 className="flex items-center gap-2 text-lg font-semibold">
            <Workflow className="h-5 w-5 text-accent" />
            Rotação de escalas
          </h2>
          <p className="text-sm text-text-secondary">
            Como o sistema distribui {summary.vagasPorSemana} vagas entre {summary.totalColaboradores} colaboradores
          </p>
        </div>
        <Button variant="ghost" size="sm" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4" />
        </Button>
      </div>

      <div className="grid gap-3 md:grid-cols-3">
        <Card glass className="p-4">
          <div className="flex items-center gap-2 text-accent">
            <Users className="h-4 w-4" />
            <span className="text-xs font-semibold uppercase tracking-wide">1. Prioridade</span>
          </div>
          <p className="mt-2 text-sm text-text-primary">
            Quem <strong>não</strong> foi na semana anterior entra primeiro ({summary.deForaProxima} de fora agora).
          </p>
        </Card>
        <Card glass className="p-4">
          <div className="flex items-center gap-2 text-amber-300">
            <Repeat className="h-4 w-4" />
            <span className="text-xs font-semibold uppercase tracking-wide">2. Repetição</span>
          </div>
          <p className="mt-2 text-sm text-text-primary">
            Se faltar gente, completa com repetidos — hoje precisaria de{' '}
            <strong>{summary.repetidosNecessarios}</strong> repetido(s).
          </p>
        </Card>
        <Card glass className="p-4">
          <div className="flex items-center gap-2 text-emerald-300">
            <UserMinus className="h-4 w-4" />
            <span className="text-xs font-semibold uppercase tracking-wide">3. Limite</span>
          </div>
          <p className="mt-2 text-sm text-text-primary">
            Quem já tem {summary.limiteSemanasConsecutivas} semanas seguidas{' '}
            <strong>não repete</strong> na 3ª, se houver alternativa.
          </p>
        </Card>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {[
          { label: 'Colaboradores', value: summary.totalColaboradores, icon: Users },
          { label: 'Vagas / semana', value: summary.vagasPorSemana, icon: UserCheck },
          { label: 'De fora (próx.)', value: summary.deForaProxima, icon: UserMinus },
          { label: 'Repetidos (próx.)', value: summary.repetidosNecessarios, icon: Repeat },
        ].map((item) => (
          <Card key={item.label} glass className="p-4">
            <div className="flex items-center justify-between">
              <span className="text-xs text-text-secondary">{item.label}</span>
              <item.icon className="h-4 w-4 text-text-secondary/60" />
            </div>
            <p className="mt-1 text-2xl font-semibold text-text-primary">{item.value}</p>
          </Card>
        ))}
      </div>

      <Card glass>
        <CardHeader>
          <CardTitle className="text-base">Próxima geração (análise)</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap items-center gap-2 text-sm text-text-secondary">
            <span className="rounded-lg bg-white/5 px-2 py-1">{nextWeek.waitingOutside.length} de fora</span>
            <ArrowRight className="h-4 w-4" />
            <span className="rounded-lg bg-sky-500/10 px-2 py-1 text-sky-200">
              {Math.min(nextWeek.newEntries.length, summary.vagasPorSemana)} entram como novos
            </span>
            {nextWeek.repeatNeeded > 0 && (
              <>
                <span>+</span>
                <span className="rounded-lg bg-amber-500/10 px-2 py-1 text-amber-200">
                  {nextWeek.repeatNeeded} repetido(s)
                </span>
              </>
            )}
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <div>
              <p className="mb-2 text-xs font-medium uppercase tracking-wide text-text-secondary">
                Novos (de fora)
              </p>
              <div className="flex flex-wrap gap-1.5">
                {nextWeek.newEntries.length === 0 ? (
                  <span className="text-sm text-text-secondary/60">—</span>
                ) : (
                  nextWeek.newEntries.map((n) => <NameChip key={n} name={n} variant="new" />)
                )}
              </div>
            </div>
            <div>
              <p className="mb-2 text-xs font-medium uppercase tracking-wide text-text-secondary">
                Podem repetir (sequência &lt; {summary.limiteSemanasConsecutivas})
              </p>
              <div className="flex flex-wrap gap-1.5">
                {nextWeek.canRepeat.length === 0 ? (
                  <span className="text-sm text-text-secondary/60">—</span>
                ) : (
                  nextWeek.canRepeat.map((n) => <NameChip key={n} name={n} variant="repeat" />)
                )}
              </div>
            </div>
          </div>

          {nextWeek.blockedRepeat.length > 0 && (
            <div className="rounded-lg border border-red-500/20 bg-red-500/5 p-3">
              <p className="mb-2 text-xs font-medium uppercase tracking-wide text-red-300">
                Bloqueados para repetir (3ª semana seguida)
              </p>
              <div className="flex flex-wrap gap-1.5">
                {nextWeek.blockedRepeat.map((n) => (
                  <NameChip key={n} name={n} variant="default" />
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Card glass>
        <CardHeader>
          <CardTitle className="text-base">Colaboradores agora</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
            {employees.map((emp, i) => (
              <motion.div
                key={emp.id}
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.02 }}
                className="flex items-center gap-2 rounded-lg border border-border bg-bg-panel/40 px-3 py-2"
              >
                <Avatar name={emp.name} color={emp.color} size="sm" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">{emp.name}</p>
                  <div className="mt-0.5 flex flex-wrap gap-1">
                    <Badge variant={STATUS_VARIANT[emp.status] ?? 'default'} className="text-[9px]">
                      {STATUS_LABEL[emp.status] ?? emp.status}
                    </Badge>
                    {emp.streak > 0 && (
                      <Badge variant="secondary" className="text-[9px]">
                        {emp.streak} sem. seguida{emp.streak > 1 ? 's' : ''}
                      </Badge>
                    )}
                  </div>
                </div>
              </motion.div>
            ))}
          </div>
        </CardContent>
      </Card>

      <Card glass>
        <CardHeader>
          <CardTitle className="text-base">Simulação — próximas {simulation.length} semanas</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-4 text-xs text-text-secondary/80">
            Projeção com o algoritmo actual (ordem fixa para visualização). A escala real pode variar no sorteio de duplas.
          </p>
          <div className="flex gap-3 overflow-x-auto pb-2">
            {simulation.map((week) => (
              <div
                key={week.index}
                className="w-[260px] shrink-0 rounded-xl border border-border bg-bg-panel/50 p-3"
              >
                <p className="text-sm font-semibold">Semana {week.index}</p>
                <p className="text-[11px] text-text-secondary">
                  {formatWeekRange(week.weekStart)}
                </p>
                <div className="mt-2 flex flex-wrap gap-1 text-[10px] text-text-secondary">
                  <span>{week.outsideCount} de fora</span>
                  <span>·</span>
                  <span>{week.repeatNeeded} repet.</span>
                </div>
                <div className="mt-3 space-y-2">
                  <div>
                    <p className="mb-1 text-[10px] uppercase tracking-wide text-sky-300/80">Novos</p>
                    <div className="flex flex-wrap gap-1">
                      {week.newNames.map((n) => (
                        <NameChip key={n} name={n} variant="new" />
                      ))}
                    </div>
                  </div>
                  <div>
                    <p className="mb-1 text-[10px] uppercase tracking-wide text-amber-300/80">Repetidos</p>
                    <div className="flex flex-wrap gap-1">
                      {week.repeatNames.length === 0 ? (
                        <span className="text-[11px] text-text-secondary/50">—</span>
                      ) : (
                        week.repeatNames.map((n) => (
                          <NameChip key={n} name={n} variant="repeat" />
                        ))
                      )}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
