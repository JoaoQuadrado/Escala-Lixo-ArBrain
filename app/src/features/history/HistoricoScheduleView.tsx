import { useMemo } from 'react'
import { CheckCircle2, Clock } from 'lucide-react'
import { Avatar } from '@/components/ui/Avatar'
import { WEEK_DAYS, WAITING_QUEUE, BLOCKED_LIST } from '@/utils/weekDays'
import type { Employee, Schedule, WeekDay } from '@/types'
import { cn } from '@/utils/cn'

interface HistoricoScheduleViewProps {
  schedule: Schedule
  employees: Employee[]
}

function getEmployeeMap(employees: Employee[]) {
  return new Map(employees.map((e) => [e.id, e]))
}

export function HistoricoScheduleView({ schedule, employees }: HistoricoScheduleViewProps) {
  const byId = useMemo(() => getEmployeeMap(employees), [employees])

  const dayIds = useMemo(() => {
    const map = new Map<WeekDay, string[]>()
    for (const day of schedule.days) {
      map.set(day.day, day.employeeIds)
    }
    return map
  }, [schedule.days])

  const resolve = (ids: string[]) =>
    ids.map((id) => byId.get(id)).filter(Boolean) as Employee[]

  return (
    <div className="flex h-full min-h-0 flex-col gap-3">
      <div className="flex min-h-0 flex-1 gap-2">
        {WEEK_DAYS.map((day) => {
          const ids = dayIds.get(day.key) ?? []
          return (
            <ReadOnlyColumn
              key={day.key}
              label={day.label}
              employees={resolve(ids)}
              variant="day"
            />
          )
        })}
      </div>

      <div className="flex min-h-0 flex-1 gap-3">
        <ReadOnlyColumn
          label={WAITING_QUEUE.label}
          subtitle="Colaboradores fora da escala"
          employees={resolve(schedule.waitingQueue)}
          variant="waiting"
          pool
        />
        <ReadOnlyColumn
          label={BLOCKED_LIST.label}
          subtitle="Quem já cumpriu a missão da semana"
          employees={resolve(schedule.blockedQueue)}
          variant="blocked"
          pool
        />
      </div>
    </div>
  )
}

function HistoricoEmployeeChip({
  employee,
  queuePosition,
  pool,
}: {
  employee: Employee
  queuePosition?: number
  pool?: boolean
}) {
  return (
    <div
      className={cn(
        'rounded-lg border border-border bg-bg-panel/90 shadow-sm',
        pool ? 'w-[148px] shrink-0 p-2' : 'p-2',
      )}
      style={{ borderLeftColor: employee.color, borderLeftWidth: 3 }}
    >
      <div className="flex items-center gap-1.5">
        {queuePosition !== undefined && (
          <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-amber-500/15 text-[10px] font-bold text-amber-300">
            {queuePosition}
          </span>
        )}
        <Avatar name={employee.name} src={employee.photoUrl} color={employee.color} size="sm" className="!h-7 !w-7 !text-[10px]" />
        <p className="min-w-0 flex-1 truncate text-xs font-medium text-text-primary">{employee.name}</p>
      </div>
    </div>
  )
}

function ReadOnlyColumn({
  label,
  subtitle,
  employees,
  variant,
  pool = false,
}: {
  label: string
  subtitle?: string
  employees: Employee[]
  variant: 'day' | 'waiting' | 'blocked'
  pool?: boolean
}) {
  const isWaiting = variant === 'waiting'
  const isBlocked = variant === 'blocked'

  return (
    <div
      className={cn(
        'flex min-h-0 min-w-0 flex-1 flex-col rounded-xl border border-border bg-bg-panel/40 backdrop-blur-sm',
        isWaiting && 'ring-1 ring-amber-500/20 border-amber-500/15',
        isBlocked && 'ring-1 ring-emerald-500/20 border-emerald-500/15',
      )}
    >
      <div
        className={cn(
          'flex shrink-0 items-center justify-between border-b border-border px-2.5 py-2 rounded-t-xl',
          isWaiting ? 'bg-amber-500/5' : isBlocked ? 'bg-emerald-500/5' : 'bg-bg-panel/60',
        )}
      >
        <div className="min-w-0">
          <div className="flex items-center gap-1.5">
            {isWaiting && <Clock className="h-3 w-3 shrink-0 text-amber-400/80" />}
            {isBlocked && <CheckCircle2 className="h-3 w-3 shrink-0 text-emerald-400/80" />}
            <h3
              className={cn(
                'truncate text-xs font-semibold text-text-primary',
                isWaiting && 'text-amber-300',
                isBlocked && 'text-emerald-300',
              )}
            >
              {label}
            </h3>
          </div>
          {subtitle && (
            <span className="block truncate text-[9px] text-text-secondary/70">{subtitle}</span>
          )}
        </div>
        <span className="ml-1 flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full bg-white/5 px-1 text-[10px] text-text-secondary">
          {employees.length}
        </span>
      </div>

      <div
        className={cn(
          'min-h-0 flex-1 gap-1.5 overflow-y-auto p-2',
          pool ? 'flex flex-wrap content-start' : 'flex flex-col',
        )}
      >
        {employees.length === 0 ? (
          <div className="flex flex-1 items-center justify-center rounded-lg border border-dashed border-border/50 p-3">
            <p className="text-[10px] text-text-secondary/60">
              {isBlocked ? 'Ninguém por aqui' : isWaiting ? 'Todos escalados' : '—'}
            </p>
          </div>
        ) : (
          employees.map((emp, idx) => (
            <HistoricoEmployeeChip
              key={emp.id}
              employee={emp}
              queuePosition={isWaiting ? idx + 1 : undefined}
              pool={pool}
            />
          ))
        )}
      </div>
    </div>
  )
}
