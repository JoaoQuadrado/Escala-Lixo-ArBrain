import { useMemo } from 'react'
import { useApp } from '@/contexts/AppContext'
import type { Employee } from '@/types'
import { getCurrentWeekStart, getTodayWeekDay, WEEK_DAY_MAP } from '@/utils/weekDays'

export function useEmployeeWorkDays(employeeId: string) {
  const { activeSchedule } = useApp()

  return useMemo(() => {
    if (!activeSchedule) return []
    return activeSchedule.days
      .filter((d) => d.employeeIds.includes(employeeId))
      .map((d) => d.day)
  }, [activeSchedule, employeeId])
}

export function useTodaySchedule() {
  const { state, activeSchedule } = useApp()

  return useMemo(() => {
    const today = getTodayWeekDay()
    const weekStart = getCurrentWeekStart()
    const schedule =
      state.schedules.find((s) => s.weekStart === weekStart) ?? activeSchedule

    const dateLabel = new Date().toLocaleDateString('pt-BR', {
      weekday: 'long',
      day: '2-digit',
      month: 'long',
    })

    if (!today) {
      return {
        today: null,
        dayLabel: null,
        dateLabel,
        employees: [] as Employee[],
        schedule,
        isWeekend: true,
      }
    }

    const dayLabel = WEEK_DAY_MAP[today]

    if (!schedule) {
      return {
        today,
        dayLabel,
        dateLabel,
        employees: [] as Employee[],
        schedule: null,
        isWeekend: false,
      }
    }

    const employeeIds =
      schedule.days.find((d) => d.day === today)?.employeeIds ?? []

    const employees = employeeIds
      .map((id) => state.employees.find((e) => e.id === id))
      .filter((e): e is Employee => e != null)

    return {
      today,
      dayLabel,
      dateLabel,
      employees,
      schedule,
      isWeekend: false,
    }
  }, [state.schedules, state.employees, activeSchedule])
}
