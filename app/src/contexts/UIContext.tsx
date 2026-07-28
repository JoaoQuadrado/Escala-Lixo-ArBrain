import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useReducer,
  useState,
  type ReactNode,
} from 'react'

interface UIContextValue {
  selectedEmployeeId: string | null
  setSelectedEmployeeId: (id: string | null) => void
  employeePanelOpen: boolean
  setEmployeePanelOpen: (open: boolean) => void
  showIntro: boolean
  openIntroScreen: () => void
  closeIntroScreen: () => void
}

const UIContext = createContext<UIContextValue | null>(null)

export function UIProvider({ children }: { children: ReactNode }) {
  const [showIntro, setShowIntro] = useState(true)
  const [selectedEmployeeId, setSelectedEmployeeId] = useReducer(
    (_: string | null, action: string | null) => action,
    null,
  )
  const [employeePanelOpen, setEmployeePanelOpen] = useReducer(
    (_: boolean, action: boolean) => action,
    false,
  )

  const openIntroScreen = useCallback(() => setShowIntro(true), [])
  const closeIntroScreen = useCallback(() => setShowIntro(false), [])

  const value = useMemo(
    () => ({
      selectedEmployeeId,
      setSelectedEmployeeId,
      employeePanelOpen,
      setEmployeePanelOpen,
      showIntro,
      openIntroScreen,
      closeIntroScreen,
    }),
    [selectedEmployeeId, employeePanelOpen, showIntro, openIntroScreen, closeIntroScreen],
  )

  return <UIContext.Provider value={value}>{children}</UIContext.Provider>
}

export function useUI() {
  const ctx = useContext(UIContext)
  if (!ctx) throw new Error('useUI must be used within UIProvider')
  return ctx
}
