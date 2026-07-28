import { RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import { Button } from '@/components/ui/Button'
import { DiscordIcon } from '@/components/ui/DiscordIcon'
import {
  DiscordSendConfirmDialog,
  useDiscordSendConfirm,
} from '@/components/discord/DiscordSendConfirmDialog'
import { GerarEscalaConfirmDialog } from '@/features/schedule/components/GerarEscalaConfirmDialog'
import { useApp, useToast, ApiError } from '@/contexts/AppContext'
import { getBlockedEmployeeIds } from '@/features/schedule/utils/schedulePools'
import { formatWeekRange } from '@/utils/weekDays'

interface TopBarProps {
  title?: string
  showActions?: boolean
}

export function TopBar({ title, showActions = true }: TopBarProps) {
  const {
    activeSchedule,
    apiConnected,
    gerarEscala,
    enviarDiscordDia,
    enviarDiscordPrevia,
  } = useApp()
  const { showToast } = useToast()
  const { pathname } = useLocation()
  const isHistorico = pathname === '/historico'
  const isRotacao = pathname === '/rotacao'
  const isTutorial = pathname === '/tutorial'
  const { pendingTipo, requestSend, closeConfirm } = useDiscordSendConfirm()
  const [gerarConfirmOpen, setGerarConfirmOpen] = useState(false)

  const displayTitle = isHistorico
    ? 'Histórico de escalas'
    : isRotacao
      ? 'Rotação de escalas'
      : isTutorial
        ? 'Tutorial'
        : (title ?? 'Escala Lixo - ArBrain')

  const blockedCount = activeSchedule ? getBlockedEmployeeIds(activeSchedule).length : 0

  const handleGerarEscala = async () => {
    await gerarEscala()
    showToast(
      'success',
      blockedCount > 0
        ? `Escala gerada — ${blockedCount} em Missão cumprida mantidos de fora`
        : 'Escala gerada',
    )
  }

  const handleDiscordConfirm = async (tipo: 'dia' | 'previa') =>
    tipo === 'dia' ? enviarDiscordDia() : enviarDiscordPrevia()

  return (
    <>
    <header className="flex h-14 shrink-0 items-center gap-4 border-b border-border bg-bg-panel/60 backdrop-blur-xl px-6 pr-20 [-webkit-app-region:drag]">
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <h1 className="text-base font-semibold text-text-primary truncate">{displayTitle}</h1>
        </div>
        {!isHistorico && !isRotacao && !isTutorial && activeSchedule && (
          <p className="text-xs text-text-secondary">
            Semana de {formatWeekRange(activeSchedule.weekStart)}
          </p>
        )}
        {isHistorico && (
          <p className="text-xs text-text-secondary">Escalas arquivadas ao gerar uma nova semana</p>
        )}
        {isRotacao && (
          <p className="text-xs text-text-secondary">Visualização do fluxo de novos, repetidos e limites</p>
        )}
        {isTutorial && (
          <p className="text-xs text-text-secondary">Guia de uso do programa</p>
        )}
      </div>

      {showActions && apiConnected && !isHistorico && !isRotacao && !isTutorial && (
        <div className="flex items-center gap-2 [-webkit-app-region:no-drag]">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setGerarConfirmOpen(true)}
            title="Gerar escala"
            className="px-2.5"
          >
            <RefreshCw className="h-5 w-5" />
          </Button>
          <Button variant="secondary" size="sm" onClick={() => requestSend('dia')}>
            <DiscordIcon className="h-5 w-5" />
            Escala hoje
          </Button>
          <Button variant="secondary" size="sm" onClick={() => requestSend('previa')}>
            <DiscordIcon className="h-5 w-5" />
            Escala semana
          </Button>
        </div>
      )}
    </header>

    <GerarEscalaConfirmDialog
      open={gerarConfirmOpen}
      onOpenChange={setGerarConfirmOpen}
      hasActiveSchedule={!!activeSchedule}
      onConfirm={async () => {
        try {
          await handleGerarEscala()
        } catch (err) {
          showToast('error', err instanceof ApiError ? err.message : 'Erro ao gerar escala')
          throw err
        }
      }}
    />

    <DiscordSendConfirmDialog
      tipo={pendingTipo}
      onOpenChange={(open) => {
        if (!open) closeConfirm()
      }}
      onConfirm={handleDiscordConfirm}
      onSuccess={(msg) => showToast('success', msg)}
      onError={(msg) => showToast('error', msg)}
    />
    </>
  )
}
