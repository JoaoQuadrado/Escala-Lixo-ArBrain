import type { ScheduleColumn, WeekDay } from '@/types'

export const WEEK_DAYS: { key: WeekDay; label: string; short: string }[] = [
  { key: 'monday', label: 'Segunda', short: 'Seg' },
  { key: 'tuesday', label: 'Terça', short: 'Ter' },
  { key: 'wednesday', label: 'Quarta', short: 'Qua' },
  { key: 'thursday', label: 'Quinta', short: 'Qui' },
  { key: 'friday', label: 'Sexta', short: 'Sex' },
]

export const BLOCKED_LIST: { key: ScheduleColumn; label: string; short: string } = {
  key: 'blocked',
  label: 'Missão cumprida',
  short: 'Missão',
}

export const WAITING_QUEUE: { key: ScheduleColumn; label: string; short: string } = {
  key: 'waiting',
  label: 'Fila de espera',
  short: 'Fila',
}

export const WEEK_DAY_MAP: Record<WeekDay, string> = {
  monday: 'Segunda-feira',
  tuesday: 'Terça-feira',
  wednesday: 'Quarta-feira',
  thursday: 'Quinta-feira',
  friday: 'Sexta-feira',
}

export function getTodayWeekDay(): WeekDay | null {
  const days: (WeekDay | null)[] = [
    null,
    'monday',
    'tuesday',
    'wednesday',
    'thursday',
    'friday',
    null,
  ]
  return days[new Date().getDay()]
}

export function formatWeekStart(date: string): string {
  const d = new Date(date + 'T12:00:00')
  return d.toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  })
}

function parseWeekStart(date: string): Date {
  return new Date(date + 'T12:00:00')
}

function toDateString(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

/** Sexta-feira da semana útil (seg–sex). */
export function getWeekEndDate(weekStart: string): Date {
  const end = parseWeekStart(weekStart)
  end.setDate(end.getDate() + 4)
  return end
}

export function formatWeekRange(weekStart: string, style: 'short' | 'long' = 'short'): string {
  const start = parseWeekStart(weekStart)
  const end = getWeekEndDate(weekStart)

  if (style === 'short') {
    const fmt = (d: Date) => d.toLocaleDateString('pt-BR')
    return `${fmt(start)} a ${fmt(end)}`
  }

  const startDay = start.toLocaleDateString('pt-BR', { day: '2-digit' })
  const endDay = end.toLocaleDateString('pt-BR', { day: '2-digit' })
  const startMonth = start.toLocaleDateString('pt-BR', { month: 'long' })
  const endMonth = end.toLocaleDateString('pt-BR', { month: 'long' })
  const startYear = start.getFullYear()
  const endYear = end.getFullYear()

  if (startMonth === endMonth && startYear === endYear) {
    return `${startDay} a ${endDay} de ${startMonth} de ${startYear}`
  }

  if (startYear === endYear) {
    return `${startDay} de ${startMonth} a ${endDay} de ${endMonth} de ${startYear}`
  }

  return `${formatWeekStart(weekStart)} a ${formatWeekStart(toDateString(end))}`
}

export function getCurrentWeekStart(): string {
  const now = new Date()
  const day = now.getDay()
  const diff = day === 0 ? -6 : 1 - day
  const monday = new Date(now)
  monday.setDate(now.getDate() + diff)
  return monday.toISOString().split('T')[0]
}
