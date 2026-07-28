import type { Schedule, ScheduleColumn } from '@/types'

/** Fila de espera — slot `fila_espera` no banco. */
export function getWaitingEmployeeIds(schedule: Schedule): string[] {
  return [...(schedule.waitingQueue ?? [])]
}

/** Missão cumprida — slot `bloqueados` no banco. */
export function getBlockedEmployeeIds(schedule: Schedule): string[] {
  return [...(schedule.blockedQueue ?? [])]
}

export function isEmployeeInWaitingPool(employeeId: string, schedule: Schedule): boolean {
  return getWaitingEmployeeIds(schedule).includes(employeeId)
}

export function resolveMoveOrigin(
  employeeId: string,
  schedule: Schedule,
  fromDay: ScheduleColumn,
): ScheduleColumn {
  if (fromDay === 'waiting' || fromDay === 'blocked') return fromDay
  if (getBlockedEmployeeIds(schedule).includes(employeeId)) return 'blocked'
  if (isEmployeeInWaitingPool(employeeId, schedule)) return 'waiting'
  return fromDay
}
