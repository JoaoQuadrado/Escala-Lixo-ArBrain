import { motion } from 'framer-motion'
import { CalendarDays, Palmtree, SendHorizontal, UserX } from 'lucide-react'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import {
  DiscordSendConfirmDialog,
  useDiscordSendConfirm,
} from '@/components/discord/DiscordSendConfirmDialog'
import { useApp, useToast } from '@/contexts/AppContext'
import { useTodaySchedule } from '@/features/schedule/hooks/useSchedule'

export function TodaySchedulePanel() {
  const { dayLabel, dateLabel, employees, schedule, isWeekend } = useTodaySchedule()
  const { apiConnected, enviarDiscordDia } = useApp()
  const { showToast } = useToast()
  const { pendingTipo, requestSend, closeConfirm } = useDiscordSendConfirm()

  const canSend = apiConnected && !isWeekend && !!schedule && employees.length > 0

  return (
    <>
      <motion.aside
        initial={{ opacity: 0, x: -24 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ delay: 0.25, duration: 0.6 }}
        className="flex w-full flex-col rounded-2xl border border-white/10 bg-black/45 p-5 shadow-2xl shadow-black/35 backdrop-blur-xl"
        aria-label="Escala de hoje"
      >
        <div className="mb-4 flex items-start gap-3">
          <div className="flex min-w-0 flex-1 items-start gap-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-accent/15 ring-1 ring-accent/30">
              <CalendarDays className="h-5 w-5 text-accent" />
            </div>
            <div className="min-w-0">
              <h2 className="text-lg font-semibold text-white">Escala de hoje</h2>
              <p className="text-sm capitalize text-white/70">{dateLabel}</p>
            </div>
          </div>

          {canSend && (
            <Button
              type="button"
              variant="secondary"
              size="sm"
              className="shrink-0 h-9 px-2.5 border-white/15 bg-white/10 hover:bg-white/15"
              title="Enviar escala de hoje"
              onClick={() => requestSend('dia')}
            >
              <SendHorizontal className="h-4 w-4" />
            </Button>
          )}
        </div>

        {isWeekend ? (
          <p className="rounded-xl border border-white/10 bg-white/5 px-4 py-6 text-center text-sm text-white/55">
            Fim de semana — sem escala de limpeza.
          </p>
        ) : !schedule ? (
          <p className="rounded-xl border border-white/10 bg-white/5 px-4 py-6 text-center text-sm text-white/55">
            Nenhuma escala para esta semana.
          </p>
        ) : employees.length === 0 ? (
          <p className="rounded-xl border border-white/10 bg-white/5 px-4 py-6 text-center text-sm text-white/55">
            Ninguém escalado para {dayLabel?.toLowerCase() ?? 'hoje'}.
          </p>
        ) : (
          <ul className="flex max-h-52 flex-col gap-2.5 overflow-y-auto pr-0.5">
            {employees.map((employee, index) => (
              <li
                key={employee.id}
                className="flex items-center gap-3 rounded-xl border border-white/10 bg-white/5 px-3 py-2.5"
                style={{ borderLeftColor: employee.color, borderLeftWidth: 3 }}
              >
                <span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-accent/15 text-[11px] font-bold text-accent">
                  {index + 1}
                </span>
                <Avatar name={employee.name} src={employee.photoUrl} color={employee.color} size="sm" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-white">{employee.name}</p>
                </div>
                <div className="flex shrink-0 flex-col gap-1">
                  {employee.onVacation && (
                    <Badge variant="warning">
                      <Palmtree className="mr-0.5 h-2.5 w-2.5" />
                      Férias
                    </Badge>
                  )}
                  {employee.absent && (
                    <Badge variant="danger">
                      <UserX className="mr-0.5 h-2.5 w-2.5" />
                      Ausente
                    </Badge>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </motion.aside>

      <DiscordSendConfirmDialog
        tipo={pendingTipo}
        onOpenChange={(open) => {
          if (!open) closeConfirm()
        }}
        onConfirm={async () => enviarDiscordDia()}
        onSuccess={(msg) => showToast('success', msg)}
        onError={(msg) => showToast('error', msg)}
      />
    </>
  )
}
