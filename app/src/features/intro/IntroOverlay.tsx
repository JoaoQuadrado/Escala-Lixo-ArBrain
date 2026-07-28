import { AnimatePresence } from 'framer-motion'
import { useUI } from '@/contexts/UIContext'
import { IntroScreen } from '@/features/intro/IntroScreen'

export function IntroOverlay() {
  const { showIntro, closeIntroScreen } = useUI()

  return (
    <AnimatePresence>
      {showIntro && (
        <IntroScreen key="intro" onEnter={closeIntroScreen} />
      )}
    </AnimatePresence>
  )
}
