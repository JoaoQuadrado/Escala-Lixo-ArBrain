import type { Schedule, ScheduleColumn, WeekDay } from '@/types'
import { WEEK_DAYS } from '@/utils/weekDays'
import { getWaitingEmployeeIds } from '@/features/schedule/utils/schedulePools'

export const DUPLA_SIZE = 2

export function isWeekday(column: ScheduleColumn): column is WeekDay {
  return WEEK_DAYS.some((d) => d.key === column)
}

export function countOnWeekday(schedule: Schedule, day: WeekDay): number {
  return schedule.days.find((d) => d.day === day)?.employeeIds.length ?? 0
}

/** Completa dupla (2 pessoas) puxando da fila de espera, se necessário. */
export async function balanceWeekdayDupla(
  schedule: Schedule,
  day: WeekDay,
  move: (employeeId: string, from: ScheduleColumn, to: ScheduleColumn) => Promise<Schedule>,
): Promise<{ schedule: Schedule; filled: number; missing: number }> {
  let current = schedule
  let filled = 0

  while (countOnWeekday(current, day) < DUPLA_SIZE) {
    const waiting = getWaitingEmployeeIds(current)
    if (waiting.length === 0) break

    current = await move(waiting[0], 'waiting', day)
    filled += 1
  }

  const missing = Math.max(0, DUPLA_SIZE - countOnWeekday(current, day))
  return { schedule: current, filled, missing }
}
