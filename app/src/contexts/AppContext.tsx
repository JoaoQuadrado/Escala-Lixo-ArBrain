import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import type { AppState } from '@/types'
import {
  createColaboradorApi,
  deleteColaboradorApi,
  discordDiaApi,
  discordPreviaApi,
  fetchEstado,
  gerarEscalaApi,
  moveEmployeeApi,
  swapEmployeesApi,
  updateColaboradorApi,
  waitForApiHealth,
} from '@/services/api'
import type { Employee, ScheduleColumn } from '@/types'

export { ApiError } from '@/services/api'
export { ToastProvider, useToast } from '@/contexts/ToastContext'
export { UIProvider, useUI } from '@/contexts/UIContext'

const EMPTY_STATE: AppState = {
  employees: [],
  schedules: [],
  activeScheduleId: null,
}

interface AppContextValue {
  state: AppState
  activeSchedule: AppState['schedules'][number] | null
  apiConnected: boolean
  loading: boolean
  addEmployee: (data: Omit<Employee, 'id'>) => Promise<void>
  updateEmployee: (employee: Employee) => Promise<void>
  deleteEmployee: (id: string) => Promise<void>
  moveEmployee: (employeeId: string, fromDay: ScheduleColumn, toDay: ScheduleColumn, toIndex?: number) => Promise<AppState>
  swapEmployees: (employeeIdA: string, fromDayA: ScheduleColumn, employeeIdB: string, fromDayB: ScheduleColumn) => Promise<void>
  getEmployeeById: (id: string) => Employee | undefined
  gerarEscala: () => Promise<void>
  enviarDiscordDia: () => Promise<string>
  enviarDiscordPrevia: () => Promise<string>
  refreshFromApi: () => Promise<void>
}

const AppContext = createContext<AppContextValue | null>(null)

export function AppProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AppState>(EMPTY_STATE)
  const [apiConnected, setApiConnected] = useState(false)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    async function init() {
      const online = await waitForApiHealth()
      if (cancelled) return

      setApiConnected(online)

      if (online) {
        try {
          const data = await fetchEstado()
          if (!cancelled) setState(data)
        } catch {
          if (!cancelled) setState(EMPTY_STATE)
        }
      }

      if (!cancelled) setLoading(false)
    }

    init()
    return () => { cancelled = true }
  }, [])

  const addEmployee = useCallback(async (data: Omit<Employee, 'id'>) => {
    const next = await createColaboradorApi(data)
    setState(next)
  }, [])

  const updateEmployee = useCallback(async (employee: Employee) => {
    const next = await updateColaboradorApi(employee)
    setState(next)
  }, [])

  const deleteEmployee = useCallback(async (id: string) => {
    const next = await deleteColaboradorApi(id)
    setState(next)
  }, [])

  const activeSchedule = useMemo(
    () => state.schedules.find((s) => s.id === state.activeScheduleId) ?? null,
    [state.schedules, state.activeScheduleId],
  )

  const moveEmployee = useCallback(
    async (employeeId: string, fromDay: ScheduleColumn, toDay: ScheduleColumn, toIndex?: number) => {
      const next = await moveEmployeeApi(employeeId, fromDay, toDay, toIndex)
      setState(next)
      return next
    },
    [],
  )

  const swapEmployees = useCallback(
    async (
      employeeIdA: string,
      fromDayA: ScheduleColumn,
      employeeIdB: string,
      fromDayB: ScheduleColumn,
    ) => {
      const next = await swapEmployeesApi(employeeIdA, fromDayA, employeeIdB, fromDayB)
      setState(next)
    },
    [],
  )

  const gerarEscala = useCallback(async () => {
    const next = await gerarEscalaApi()
    setState(next)
  }, [])

  const enviarDiscordDia = useCallback(async () => discordDiaApi(), [])
  const enviarDiscordPrevia = useCallback(async () => discordPreviaApi(), [])

  const refreshFromApi = useCallback(async () => {
    const next = await fetchEstado()
    setState(next)
  }, [])

  const getEmployeeById = useCallback(
    (id: string) => state.employees.find((e) => e.id === id),
    [state.employees],
  )

  const value = useMemo(
    () => ({
      state,
      activeSchedule,
      apiConnected,
      loading,
      addEmployee,
      updateEmployee,
      deleteEmployee,
      moveEmployee,
      swapEmployees,
      getEmployeeById,
      gerarEscala,
      enviarDiscordDia,
      enviarDiscordPrevia,
      refreshFromApi,
    }),
    [
      state,
      activeSchedule,
      apiConnected,
      loading,
      addEmployee,
      updateEmployee,
      deleteEmployee,
      moveEmployee,
      swapEmployees,
      getEmployeeById,
      gerarEscala,
      enviarDiscordDia,
      enviarDiscordPrevia,
      refreshFromApi,
    ],
  )

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>
}

export function useApp() {
  const ctx = useContext(AppContext)
  if (!ctx) throw new Error('useApp must be used within AppProvider')
  return ctx
}
