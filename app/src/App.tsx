import { BrowserRouter, Navigate, Routes, Route } from 'react-router-dom'
import { AppProvider, ToastProvider, UIProvider } from '@/contexts/AppContext'
import { AppLayout } from '@/layouts/AppLayout'
import { IntroOverlay } from '@/features/intro/IntroOverlay'
import { WindowControls } from '@/components/layout/WindowControls'
import { EscalasPage } from '@/features/schedule/EscalasPage'
import { FuncionariosPage } from '@/features/employees/FuncionariosPage'
import { ConfiguracoesPage } from '@/features/settings/ConfiguracoesPage'
import { HistoricoPage } from '@/features/history/HistoricoPage'
import { RotacaoPage } from '@/features/rotation/RotacaoPage'
import { TutorialPage } from '@/features/tutorial/TutorialPage'

export default function App() {
  return (
    <AppProvider>
      <ToastProvider>
        <UIProvider>
          <BrowserRouter>
            <Routes>
              <Route element={<AppLayout />}>
                <Route index element={<EscalasPage />} />
                <Route path="escalas" element={<Navigate to="/" replace />} />
                <Route path="funcionarios" element={<FuncionariosPage />} />
                <Route path="historico" element={<HistoricoPage />} />
                <Route path="rotacao" element={<RotacaoPage />} />
                <Route path="tutorial" element={<TutorialPage />} />
                <Route path="configuracoes" element={<ConfiguracoesPage />} />
              </Route>
            </Routes>
          </BrowserRouter>
          <IntroOverlay />
          <WindowControls />
        </UIProvider>
      </ToastProvider>
    </AppProvider>
  )
}
