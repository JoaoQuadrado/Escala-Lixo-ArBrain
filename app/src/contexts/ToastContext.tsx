import { createContext, useCallback, useContext, useReducer, type ReactNode } from 'react'
import type { ToastMessage } from '@/types'
import { generateId } from '@/utils/colors'

interface ToastContextValue {
  toasts: ToastMessage[]
  showToast: (type: ToastMessage['type'], message: string) => void
  dismissToast: (id: string) => void
}

const ToastContext = createContext<ToastContextValue | null>(null)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useReducer(
    (
      state: ToastMessage[],
      action: { type: 'add'; toast: ToastMessage } | { type: 'remove'; id: string },
    ) => {
      if (action.type === 'add') return [...state, action.toast]
      return state.filter((t) => t.id !== action.id)
    },
    [],
  )

  const showToast = useCallback((type: ToastMessage['type'], message: string) => {
    const toast: ToastMessage = { id: generateId(), type, message }
    setToasts({ type: 'add', toast })
    setTimeout(() => setToasts({ type: 'remove', id: toast.id }), 5000)
  }, [])

  const dismissToast = useCallback((id: string) => {
    setToasts({ type: 'remove', id })
  }, [])

  return (
    <ToastContext.Provider value={{ toasts, showToast, dismissToast }}>
      {children}
    </ToastContext.Provider>
  )
}

export function useToast() {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within ToastProvider')
  return ctx
}
