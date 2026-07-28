import { useMemo, useState } from 'react'
import {
  DndContext,
  DragOverlay,
  closestCorners,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import { useApp, useToast, ApiError } from '@/contexts/AppContext'
import {
  getBlockedEmployeeIds,
  getWaitingEmployeeIds,
  resolveMoveOrigin,
} from '@/features/schedule/utils/schedulePools'
import {
  DUPLA_SIZE,
  balanceWeekdayDupla,
  countOnWeekday,
  isWeekday,
} from '@/features/schedule/utils/scheduleMove'
import { WEEK_DAYS, WAITING_QUEUE, BLOCKED_LIST, getTodayWeekDay } from '@/utils/weekDays'
import type { ScheduleColumn } from '@/types'
import { EmployeeCard } from './EmployeeCard'
import { DayColumn } from './DayColumn'

function parseDragId(id: string): { column: ScheduleColumn; employeeId: string } | null {
  const parts = id.split('::')
  if (parts.length !== 2) return null
  return { column: parts[0] as ScheduleColumn, employeeId: parts[1] }
}

function makeDragId(column: ScheduleColumn, employeeId: string) {
  return `${column}::${employeeId}`
}

export function KanbanBoard() {
  const { activeSchedule, moveEmployee, swapEmployees, getEmployeeById } = useApp()
  const { showToast } = useToast()

  const [activeDragId, setActiveDragId] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor),
  )

  const today = getTodayWeekDay()

  const activeEmployee = useMemo(() => {
    if (!activeDragId) return null
    const parsed = parseDragId(activeDragId)
    return parsed ? getEmployeeById(parsed.employeeId) : null
  }, [activeDragId, getEmployeeById])

  const waitingIds = useMemo(() => {
    if (!activeSchedule) return []
    return getWaitingEmployeeIds(activeSchedule)
  }, [activeSchedule])

  const blockedIds = useMemo(() => {
    if (!activeSchedule) return []
    return getBlockedEmployeeIds(activeSchedule)
  }, [activeSchedule])

  if (!activeSchedule) {
    return (
      <div className="flex h-full items-center justify-center text-text-secondary">
        Nenhuma escala ativa. Crie uma nova escala para começar.
      </div>
    )
  }

  const waitingEmployees = waitingIds
    .map((id) => getEmployeeById(id))
    .filter(Boolean) as NonNullable<ReturnType<typeof getEmployeeById>>[]

  const blockedEmployees = blockedIds
    .map((id) => getEmployeeById(id))
    .filter(Boolean) as NonNullable<ReturnType<typeof getEmployeeById>>[]

  const handleDragStart = (event: DragStartEvent) => {
    setActiveDragId(String(event.active.id))
  }

  const handleMutationError = (err: unknown) => {
    if (err instanceof ApiError) {
      showToast('error', err.message)
      if (err.validation?.errors.length) {
        err.validation.errors.forEach((e) => showToast('error', e))
      }
    } else {
      showToast('error', 'Erro ao atualizar escala')
    }
  }

  const moveAndBalanceOrigin = async (
    employeeId: string,
    fromDay: ScheduleColumn,
    toDay: ScheduleColumn,
    toIndex?: number,
  ) => {
    let next = await moveEmployee(employeeId, fromDay, toDay, toIndex)

    if (isWeekday(fromDay)) {
      await balanceWeekdayDupla(next, fromDay, async (id, from, to) => {
        next = await moveEmployee(id, from, to)
        return next
      })
    }

    return next
  }

  const handleDragEnd = async (event: DragEndEvent) => {
    setActiveDragId(null)
    const { active, over } = event
    if (!over || isSaving) return

    const from = parseDragId(String(active.id))
    if (!from) return

    const actualFrom = resolveMoveOrigin(from.employeeId, activeSchedule, from.column)
    const overId = String(over.id)

    setIsSaving(true)
    try {
      // Soltar sobre outro colaborador
      if (!overId.startsWith('column-')) {
        const to = parseDragId(overId)
        if (!to || to.employeeId === from.employeeId) return

        const actualTo = resolveMoveOrigin(to.employeeId, activeSchedule, to.column)

        // Dupla cheia → troca com quem foi escolhido
        if (isWeekday(actualTo) && countOnWeekday(activeSchedule, actualTo) >= DUPLA_SIZE) {
          await swapEmployees(from.employeeId, actualFrom, to.employeeId, actualTo)
          showToast('success', 'Troca feita')
          return
        }

        // Dia incompleto → entra na dupla (sem tirar o outro)
        const toIndex = isWeekday(actualTo)
          ? countOnWeekday(activeSchedule, actualTo)
          : actualTo === 'blocked'
            ? blockedIds.indexOf(to.employeeId)
            : undefined

        await moveAndBalanceOrigin(
          from.employeeId,
          actualFrom,
          actualTo,
          toIndex,
        )
        showToast('success', 'Escala ajustada')
        return
      }

      // Soltar na área da coluna
      const toColumn = overId.replace('column-', '') as ScheduleColumn
      if (actualFrom === toColumn) return

      if (isWeekday(toColumn) && countOnWeekday(activeSchedule, toColumn) >= DUPLA_SIZE) {
        showToast('error', 'Este dia já tem dupla completa. Solte em cima de quem quer trocar.')
        return
      }

      let toIndex: number | undefined
      if (toColumn === 'waiting') {
        toIndex = undefined
      } else if (toColumn === 'blocked') {
        toIndex = blockedIds.length
      } else if (isWeekday(toColumn)) {
        toIndex = countOnWeekday(activeSchedule, toColumn)
      }

      await moveAndBalanceOrigin(
        from.employeeId,
        actualFrom,
        toColumn,
        toIndex,
      )
      showToast('success', 'Escala ajustada')
    } catch (err) {
      handleMutationError(err)
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div className="flex h-full min-h-0 flex-col gap-3">
        <div className="flex min-h-0 flex-1 gap-2">
          {WEEK_DAYS.map((dayInfo, colIndex) => {
            const dayData = activeSchedule.days.find((d) => d.day === dayInfo.key)
            const employeeIds = dayData?.employeeIds ?? []
            const employees = employeeIds
              .map((id) => getEmployeeById(id))
              .filter(Boolean) as NonNullable<ReturnType<typeof getEmployeeById>>[]

            return (
              <DayColumn
                key={dayInfo.key}
                column={dayInfo.key}
                label={dayInfo.label}
                isToday={dayInfo.key === today}
                employeeIds={employeeIds}
                employees={employees}
                colIndex={colIndex}
                makeDragId={makeDragId}
                fill
              />
            )
          })}
        </div>

        <div className="flex min-h-0 flex-1 gap-3">
          <DayColumn
            key={WAITING_QUEUE.key}
            column={WAITING_QUEUE.key}
            label={WAITING_QUEUE.label}
            subtitle="Colaboradores fora da escala"
            isToday={false}
            employeeIds={waitingIds}
            employees={waitingEmployees}
            colIndex={WEEK_DAYS.length}
            makeDragId={makeDragId}
            variant="waiting"
            fill
            pool
          />

          <DayColumn
            key={BLOCKED_LIST.key}
            column={BLOCKED_LIST.key}
            label={BLOCKED_LIST.label}
            subtitle="Quem já cumpriu a missão da semana"
            isToday={false}
            employeeIds={blockedIds}
            employees={blockedEmployees}
            colIndex={WEEK_DAYS.length + 1}
            makeDragId={makeDragId}
            variant="blocked"
            fill
            pool
          />
        </div>
      </div>

      <DragOverlay dropAnimation={{ duration: 250, easing: 'cubic-bezier(0.18, 0.67, 0.6, 1.22)' }}>
        {activeEmployee && <EmployeeCard employee={activeEmployee} isOverlay />}
      </DragOverlay>
    </DndContext>
  )
}
