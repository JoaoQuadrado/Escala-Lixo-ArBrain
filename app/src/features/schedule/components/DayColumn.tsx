import { useDroppable } from '@dnd-kit/core'
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable'
import { motion } from 'framer-motion'
import { CheckCircle2, Clock, TriangleAlert } from 'lucide-react'
import { DUPLA_SIZE } from '@/features/schedule/utils/scheduleMove'
import type { ScheduleColumn, Employee } from '@/types'
import { SortableEmployeeCard } from './EmployeeCard'
import { cn } from '@/utils/cn'

interface DayColumnProps {
  column: ScheduleColumn
  label: string
  subtitle?: string
  isToday: boolean
  employeeIds: string[]
  employees: Employee[]
  colIndex: number
  makeDragId: (column: ScheduleColumn, employeeId: string) => string
  variant?: 'day' | 'waiting' | 'blocked'
  compact?: boolean
  fill?: boolean
  pool?: boolean
  className?: string
}

export function DayColumn({
  column,
  label,
  subtitle,
  isToday,
  employeeIds,
  employees,
  colIndex,
  makeDragId,
  variant = 'day',
  compact = false,
  fill = false,
  pool = false,
  className,
}: DayColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: `column-${column}` })
  const sortableIds = employeeIds.map((eid) => makeDragId(column, eid))
  const isWaiting = variant === 'waiting'
  const isBlocked = variant === 'blocked'
  const isDay = variant === 'day'
  const duplaIncomplete = isDay && employeeIds.length < DUPLA_SIZE

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: colIndex * 0.05 }}
      className={cn(
        'flex h-full min-h-0 flex-col rounded-xl border border-border bg-bg-panel/40 backdrop-blur-sm transition-colors duration-200',
        fill ? 'min-w-0 flex-1' : compact ? 'w-[128px] min-w-[128px] shrink-0' : 'w-full min-w-0',
        isToday && 'border-t-2 border-t-accent/70',
        isWaiting && 'ring-1 ring-amber-500/20 border-amber-500/15',
        isBlocked && 'ring-1 ring-emerald-500/20 border-emerald-500/15',
        isOver &&
          (isBlocked
            ? 'border-emerald-500/40 bg-emerald-500/5'
            : isWaiting
              ? 'border-amber-500/40 bg-amber-500/5'
              : 'border-accent/40 bg-accent/5'),
        className,
      )}
    >
      <div
        className={cn(
          'flex items-center justify-between border-b border-border px-2.5 py-2 rounded-t-xl shrink-0',
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
            {isToday && (
              <span className="shrink-0 rounded px-1 py-px text-[8px] font-semibold uppercase tracking-wide text-accent/90 bg-accent/10">
                Hoje
              </span>
            )}
          </div>
          {subtitle && (
            <span className="block truncate text-[9px] text-text-secondary/70">{subtitle}</span>
          )}
        </div>
        <span
          className={cn(
            'ml-1 flex h-5 min-w-5 shrink-0 items-center justify-center rounded-full px-1 text-[10px]',
            isBlocked
              ? 'bg-emerald-500/10 text-emerald-300/80'
              : isWaiting
                ? 'bg-amber-500/10 text-amber-300/80'
                : duplaIncomplete
                  ? 'bg-amber-500/15 text-amber-300'
                  : 'bg-white/5 text-text-secondary',
          )}
        >
          {employeeIds.length}
        </span>
      </div>

      <SortableContext items={sortableIds} strategy={verticalListSortingStrategy}>
        <div
          ref={setNodeRef}
          className={cn(
            'min-h-0 flex-1 gap-1.5 overflow-y-auto p-2',
            pool ? 'flex flex-wrap content-start' : 'flex flex-col',
            !pool && (compact ? 'min-h-[120px]' : 'min-h-[80px]'),
          )}
        >
          {employees.map((employee) => (
            <SortableEmployeeCard
              key={employee.id}
              id={makeDragId(column, employee.id)}
              employee={employee}
              compact={compact || pool}
              className={pool ? 'w-[148px] shrink-0' : undefined}
            />
          ))}
          {duplaIncomplete && isDay && employees.length > 0 && (
            <div className="flex flex-1 items-center justify-center min-h-[44px]">
              <TriangleAlert
                className="h-5 w-5 text-amber-400/75"
                aria-label="Dupla incompleta"
              />
            </div>
          )}
          {employees.length === 0 && (
            <div className="flex flex-1 items-center justify-center rounded-lg border border-dashed border-border/50 p-3">
              {isDay ? (
                <TriangleAlert
                  className="h-5 w-5 text-amber-400/75"
                  aria-label="Dupla incompleta"
                />
              ) : (
                <p className="text-[10px] text-text-secondary/60">
                  {isBlocked ? 'Ninguém por aqui ainda' : isWaiting ? 'Todos escalados' : 'Arraste aqui'}
                </p>
              )}
            </div>
          )}
        </div>
      </SortableContext>
    </motion.div>
  )
}
