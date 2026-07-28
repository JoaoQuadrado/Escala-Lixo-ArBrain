import { useCallback, useEffect, useRef } from 'react'
import { motion } from 'framer-motion'
import { Button } from '@/components/ui/Button'
import { SpherePortrait } from '@/components/ui/SpherePortrait'
import { TodaySchedulePanel } from '@/features/intro/TodaySchedulePanel'

interface IntroScreenProps {
  onEnter: () => void
}

export function IntroScreen({ onEnter }: IntroScreenProps) {
  const videoRef = useRef<HTMLVideoElement>(null)

  const handleEnter = useCallback(() => {
    videoRef.current?.pause()
    onEnter()
  }, [onEnter])

  useEffect(() => {
    const video = videoRef.current
    if (!video) return
    video.currentTime = 0
    void video.play().catch(() => {})
  }, [])

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault()
        handleEnter()
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [handleEnter])

  return (
    <motion.div
      initial={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.6, ease: 'easeInOut' }}
      className="fixed inset-0 z-[200] overflow-hidden bg-black [-webkit-app-region:drag]"
      role="dialog"
      aria-label="Tela inicial"
    >
      <video
        ref={videoRef}
        autoPlay
        muted
        loop
        playsInline
        className="absolute inset-0 h-full w-full object-cover brightness-[1.06] contrast-[1.03] saturate-[1.05]"
        src="/media/intro.mp4"
      />

      <div className="absolute inset-0 bg-gradient-to-t from-black/55 via-black/10 to-transparent" />
      <div className="absolute inset-0 bg-gradient-to-r from-black/25 via-transparent to-black/15" />

      <div className="absolute left-6 top-6 z-10 lg:left-10 lg:top-8 [-webkit-app-region:no-drag]">
        <img
          src="/media/arbrain-logo.png?v=2"
          alt="ArBrain"
          className="h-8 w-auto opacity-95 drop-shadow-md sm:h-9"
          draggable={false}
        />
      </div>

      <div className="relative flex h-full items-end px-6 pb-12 pt-6 lg:px-10 [-webkit-app-region:no-drag]">
        <div className="flex w-full max-w-sm flex-col gap-5">
          <motion.div
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3, duration: 0.7 }}
            className="flex flex-col gap-4"
          >
            <div className="flex items-start gap-4">
              <SpherePortrait src="/media/backoffice.png" size="lg" className="shrink-0" />
              <div className="pt-8">
                <h1 className="text-3xl font-bold tracking-tight text-white drop-shadow-lg">
                  Escala Lixo
                </h1>
                <p className="mt-1 text-sm text-white/65">
                  Gestão de escala de limpeza
                </p>
              </div>
            </div>

            <Button size="lg" onClick={handleEnter} className="w-full">
              Entrar
            </Button>
          </motion.div>

          <TodaySchedulePanel />
        </div>
      </div>
    </motion.div>
  )
}
