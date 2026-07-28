import { Minus, X } from 'lucide-react'
import { cn } from '@/utils/cn'

interface WindowControlsProps {
  className?: string
}

export function WindowControls({ className }: WindowControlsProps) {
  const api = window.electronAPI
  if (!api?.isElectron) return null

  return (
    <div className={cn('fixed top-2 right-2 z-[300] flex items-center gap-1 [-webkit-app-region:no-drag]', className)}>
      <button
        type="button"
        onClick={() => api.minimizeWindow?.()}
        className="flex h-7 w-8 items-center justify-center rounded-md text-white/70 transition-colors hover:bg-white/10 hover:text-white"
        aria-label="Minimizar"
      >
        <Minus className="h-4 w-4" />
      </button>
      <button
        type="button"
        onClick={() => api.closeWindow?.()}
        className="flex h-7 w-8 items-center justify-center rounded-md text-white/70 transition-colors hover:bg-red-500/80 hover:text-white"
        aria-label="Fechar"
      >
        <X className="h-4 w-4" />
      </button>
    </div>
  )
}
