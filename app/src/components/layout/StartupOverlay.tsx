import { Loader2 } from 'lucide-react'
import { useApp } from '@/contexts/AppContext'

export function StartupOverlay() {
  const { loading } = useApp()
  if (!loading) return null

  return (
    <div className="fixed inset-0 z-[500] flex items-center justify-center bg-[#111827]/95 backdrop-blur-sm">
      <div className="flex flex-col items-center gap-3 text-center">
        <Loader2 className="h-9 w-9 animate-spin text-accent" aria-hidden />
        <div>
          <p className="text-sm font-medium text-text-primary">A iniciar Escala Lixo</p>
          <p className="mt-1 text-xs text-text-secondary">A ligar à API e ao Supabase…</p>
        </div>
      </div>
    </div>
  )
}
