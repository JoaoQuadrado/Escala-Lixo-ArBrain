import { NavLink, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Users, Settings, Trash2, History, Workflow, Home, BookOpen } from 'lucide-react'
import { cn } from '@/utils/cn'
import { Tooltip, TooltipProvider } from '@/components/ui/Tooltip'
import { useUI } from '@/contexts/UIContext'

const navItems = [
  { to: '/', icon: Trash2, label: 'Escalas' },
  { to: '/historico', icon: History, label: 'Histórico' },
  { to: '/rotacao', icon: Workflow, label: 'Rotação' },
  { to: '/funcionarios', icon: Users, label: 'Funcionários' },
  { to: '/tutorial', icon: BookOpen, label: 'Tutorial' },
  { to: '/configuracoes', icon: Settings, label: 'Configurações' },
]

export function Sidebar() {
  const navigate = useNavigate()
  const { openIntroScreen, setEmployeePanelOpen, setSelectedEmployeeId } = useUI()

  const handleInicio = () => {
    setEmployeePanelOpen(false)
    setSelectedEmployeeId(null)
    navigate('/')
    openIntroScreen()
  }

  return (
    <TooltipProvider>
      <aside className="flex h-full w-[68px] flex-col items-center border-r border-border bg-bg-panel/80 backdrop-blur-xl py-4">
        <nav className="flex flex-1 flex-col items-center gap-1">
          <Tooltip content="Início" side="right">
            <motion.button
              type="button"
              whileHover={{ scale: 1.08 }}
              whileTap={{ scale: 0.95 }}
              onClick={handleInicio}
              className="flex h-11 w-11 items-center justify-center rounded-xl text-text-secondary transition-colors duration-200 hover:bg-white/5 hover:text-text-primary cursor-pointer"
              aria-label="Voltar ao início"
            >
              <Home className="h-5 w-5" />
            </motion.button>
          </Tooltip>

          {navItems.map((item) => (
            <Tooltip key={item.to} content={item.label} side="right">
              <NavLink to={item.to} end={item.to === '/'}>
                {({ isActive }) => (
                  <motion.div
                    whileHover={{ scale: 1.08 }}
                    whileTap={{ scale: 0.95 }}
                    className={cn(
                      'relative flex h-11 w-11 items-center justify-center rounded-xl transition-colors duration-200',
                      isActive
                        ? 'bg-accent/15 text-accent'
                        : 'text-text-secondary hover:bg-white/5 hover:text-text-primary',
                    )}
                  >
                    {isActive && (
                      <motion.div
                        layoutId="sidebar-active"
                        className="absolute inset-0 rounded-xl bg-accent/10 ring-1 ring-accent/25"
                        transition={{ type: 'spring', stiffness: 350, damping: 30 }}
                      />
                    )}
                    <item.icon className="relative h-5 w-5" />
                  </motion.div>
                )}
              </NavLink>
            </Tooltip>
          ))}
        </nav>

        <div className="mt-auto text-[9px] text-text-secondary/50 font-medium tracking-wider">
          v1.0
        </div>
      </aside>
    </TooltipProvider>
  )
}
