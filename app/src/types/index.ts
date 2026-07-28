export type WeekDay =
  | 'monday'
  | 'tuesday'
  | 'wednesday'
  | 'thursday'
  | 'friday'

export type ScheduleColumn = WeekDay | 'waiting' | 'blocked'

export interface Employee {
  id: string
  name: string
  role: string
  photoUrl?: string
  color: string
  discordUser?: string
  onVacation: boolean
  absent: boolean
  notes?: string
}

export interface DayAssignment {
  day: WeekDay
  employeeIds: string[]
}

export interface Schedule {
  id: string
  title: string
  weekStart: string
  days: DayAssignment[]
  waitingQueue: string[]
  blockedQueue: string[]
  createdAt: string
  updatedAt: string
}

export interface AppState {
  employees: Employee[]
  schedules: Schedule[]
  activeScheduleId: string | null
}

export interface ToastMessage {
  id: string
  type: 'success' | 'error' | 'info'
  message: string
}
