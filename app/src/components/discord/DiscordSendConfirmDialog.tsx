import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import { Dialog } from '@/components/ui/Dialog'
import { Button } from '@/components/ui/Button'
import { DiscordIcon } from '@/components/ui/DiscordIcon'
import { ApiError } from '@/contexts/AppContext'

export type DiscordSendTipo = 'dia' | 'previa'

const MENSAGENS: Record<
  DiscordSendTipo,
  { title: string; description: string; confirmLabel: string }
> = {
  dia: {
    title: 'Enviar escala de hoje?',
    description: 'A mensagem com quem está na escala de hoje será publicada no canal do Discord.',
    confirmLabel: 'Enviar escala hoje',
  },
  previa: {
    title: 'Enviar escala da semana?',
    description: 'A prévia da escala semanal será publicada no canal do Discord.',
    confirmLabel: 'Enviar escala semana',
  },
}

interface DiscordSendConfirmDialogProps {
  tipo: DiscordSendTipo | null
  onOpenChange: (open: boolean) => void
  onConfirm: (tipo: DiscordSendTipo) => Promise<string>
  onSuccess: (message: string) => void
  onError: (message: string) => void
}

export function DiscordSendConfirmDialog({
  tipo,
  onOpenChange,
  onConfirm,
  onSuccess,
  onError,
}: DiscordSendConfirmDialogProps) {
  const [sending, setSending] = useState(false)
  const open = tipo !== null
  const config = tipo ? MENSAGENS[tipo] : null

  const handleClose = () => {
    if (!sending) onOpenChange(false)
  }

  const handleConfirm = async () => {
    if (!tipo || sending) return
    setSending(true)
    try {
      const msg = await onConfirm(tipo)
      onSuccess(msg)
      onOpenChange(false)
    } catch (err) {
      onError(err instanceof ApiError ? err.message : 'Erro ao enviar Discord')
    } finally {
      setSending(false)
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) handleClose()
      }}
      title={config?.title ?? ''}
      description={config?.description}
    >
      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" disabled={sending} onClick={handleClose}>
          Cancelar
        </Button>
        <Button type="button" disabled={sending || !tipo} onClick={() => void handleConfirm()}>
          {sending ? (
            <Loader2 className="h-4 w-4 animate-spin mr-1.5" />
          ) : (
            <DiscordIcon className="h-4 w-4 mr-1.5" />
          )}
          {config?.confirmLabel ?? 'Enviar'}
        </Button>
      </div>
    </Dialog>
  )
}

export function useDiscordSendConfirm() {
  const [pendingTipo, setPendingTipo] = useState<DiscordSendTipo | null>(null)

  return {
    pendingTipo,
    requestSend: (tipo: DiscordSendTipo) => setPendingTipo(tipo),
    closeConfirm: () => setPendingTipo(null),
  }
}
