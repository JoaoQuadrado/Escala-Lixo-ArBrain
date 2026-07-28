import { useState } from 'react'
import { Loader2, RefreshCw } from 'lucide-react'
import { Dialog } from '@/components/ui/Dialog'
import { Button } from '@/components/ui/Button'

interface GerarEscalaConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: () => Promise<void>
  hasActiveSchedule: boolean
}

export function GerarEscalaConfirmDialog({
  open,
  onOpenChange,
  onConfirm,
  hasActiveSchedule,
}: GerarEscalaConfirmDialogProps) {
  const [generating, setGenerating] = useState(false)

  const handleClose = () => {
    if (!generating) onOpenChange(false)
  }

  const handleConfirm = async () => {
    if (generating) return
    setGenerating(true)
    try {
      await onConfirm()
      onOpenChange(false)
    } finally {
      setGenerating(false)
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) handleClose()
      }}
      title="Gerar nova escala?"
      description={
        hasActiveSchedule
          ? 'A escala actual será arquivada no histórico e substituída por uma nova geração automática.'
          : 'Será criada uma nova escala automática para a semana corrente.'
      }
    >
      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" disabled={generating} onClick={handleClose}>
          Cancelar
        </Button>
        <Button type="button" disabled={generating} onClick={() => void handleConfirm()}>
          {generating ? (
            <Loader2 className="h-4 w-4 animate-spin mr-1.5" />
          ) : (
            <RefreshCw className="h-4 w-4 mr-1.5" />
          )}
          Gerar escala
        </Button>
      </div>
    </Dialog>
  )
}
