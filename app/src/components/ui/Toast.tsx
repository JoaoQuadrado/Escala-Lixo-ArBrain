import { motion, AnimatePresence } from 'framer-motion'
import { CheckCircle, AlertCircle, Info, X } from 'lucide-react'
import { useToast } from '@/contexts/AppContext'
import { cn } from '@/utils/cn'

const icons = {
  success: CheckCircle,
  error: AlertCircle,
  info: Info,
}

const colors = {
  success: 'border-success/30 bg-success/10 text-success',
  error: 'border-danger/30 bg-danger/10 text-danger',
  info: 'border-accent/30 bg-accent/10 text-accent',
}

export function ToastContainer() {
  const { toasts, dismissToast } = useToast()

  return (
    <div className="fixed bottom-4 right-4 z-[100] flex flex-col gap-2">
      <AnimatePresence>
        {toasts.map((toast) => {
          const Icon = icons[toast.type]
          return (
            <motion.div
              key={toast.id}
              initial={{ opacity: 0, x: 50, scale: 0.95 }}
              animate={{ opacity: 1, x: 0, scale: 1 }}
              exit={{ opacity: 0, x: 50, scale: 0.95 }}
              className={cn(
                'flex items-center gap-3 rounded-lg border px-4 py-3 shadow-lg backdrop-blur-md min-w-[280px]',
                colors[toast.type],
              )}
            >
              <Icon className="h-4 w-4 shrink-0" />
              <span className="flex-1 text-sm text-text-primary">{toast.message}</span>
              <button
                onClick={() => dismissToast(toast.id)}
                className="text-text-secondary hover:text-text-primary cursor-pointer"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            </motion.div>
          )
        })}
      </AnimatePresence>
    </div>
  )
}
