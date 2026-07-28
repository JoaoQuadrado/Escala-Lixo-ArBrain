import { motion, AnimatePresence } from 'framer-motion'
import { X, Edit, Calendar } from 'lucide-react'
import { useApp } from '@/contexts/AppContext'
import { useUI } from '@/contexts/UIContext'
import { useEmployeeWorkDays } from '@/features/schedule/hooks/useSchedule'
import { isEmployeeInWaitingPool } from '@/features/schedule/utils/schedulePools'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { WEEK_DAY_MAP } from '@/utils/weekDays'

interface EmployeePanelProps {
  onEdit: (id: string) => void
}

export function EmployeePanel({ onEdit }: EmployeePanelProps) {
  const { selectedEmployeeId, employeePanelOpen, setEmployeePanelOpen, setSelectedEmployeeId } = useUI()
  const { getEmployeeById, activeSchedule } = useApp()

  const employee = selectedEmployeeId ? getEmployeeById(selectedEmployeeId) : null
  const workDays = useEmployeeWorkDays(selectedEmployeeId ?? '')
  const queuePosition = selectedEmployeeId
    ? (activeSchedule?.blockedQueue?.includes(selectedEmployeeId)
        ? -2
        : activeSchedule?.waitingQueue?.indexOf(selectedEmployeeId) ?? -1)
    : -1

  const close = () => {
    setEmployeePanelOpen(false)
    setSelectedEmployeeId(null)
  }

  return (
    <AnimatePresence>
      {employeePanelOpen && employee && (
        <>
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-40 bg-black/30"
            onClick={close}
          />
          <motion.aside
            initial={{ x: 400, opacity: 0 }}
            animate={{ x: 0, opacity: 1 }}
            exit={{ x: 400, opacity: 0 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className="fixed right-0 top-0 z-50 flex h-full w-[360px] flex-col border-l border-border bg-bg-panel/95 backdrop-blur-xl shadow-2xl"
          >
            <div className="flex items-center justify-between border-b border-border p-4">
              <h2 className="text-sm font-semibold">Detalhes</h2>
              <button onClick={close} className="rounded-lg p-1.5 text-text-secondary hover:bg-white/5 cursor-pointer">
                <X className="h-4 w-4" />
              </button>
            </div>

            <div className="flex-1 overflow-y-auto p-4 space-y-6">
              <div className="flex flex-col items-center gap-3 pt-2">
                <Avatar name={employee.name} src={employee.photoUrl} color={employee.color} size="lg" />
                <div className="text-center">
                  <h3 className="text-lg font-semibold">{employee.name}</h3>
                  {employee.discordUser && (
                    <p className="text-sm text-text-secondary">Discord: {employee.discordUser}</p>
                  )}
                </div>
                <div className="flex gap-2">
                  {employee.onVacation && <Badge variant="warning">Férias</Badge>}
                  {employee.absent && <Badge variant="danger">Ausente</Badge>}
                </div>
              </div>

              <div>
                <h4 className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-wide text-text-secondary">
                  <Calendar className="h-3.5 w-3.5" />
                  Dias trabalhados
                </h4>
                {workDays.length > 0 ? (
                  <div className="flex flex-wrap gap-1.5">
                    {workDays.map((day) => (
                      <Badge key={day} variant="accent">
                        {WEEK_DAY_MAP[day]}
                      </Badge>
                    ))}
                  </div>
                ) : queuePosition === -2 ? (
                  <Badge variant="success">Missão cumprida</Badge>
                ) : selectedEmployeeId && activeSchedule && isEmployeeInWaitingPool(selectedEmployeeId, activeSchedule) ? (
                  <Badge variant="warning">Fila de espera</Badge>
                ) : (
                  <p className="text-sm text-text-secondary">Nenhum dia atribuído nesta escala</p>
                )}
              </div>

              {employee.notes && (
                <div>
                  <h4 className="mb-2 text-xs font-medium uppercase tracking-wide text-text-secondary">
                    Observações
                  </h4>
                  <p className="text-sm text-text-primary/80 rounded-lg bg-bg-primary/50 p-3 border border-border">
                    {employee.notes}
                  </p>
                </div>
              )}
            </div>

            <div className="border-t border-border p-4">
              <Button className="w-full" onClick={() => onEdit(employee.id)}>
                <Edit className="h-4 w-4" />
                Editar funcionário
              </Button>
            </div>
          </motion.aside>
        </>
      )}
    </AnimatePresence>
  )
}
