import { cn } from '@/utils/cn'
import discordIcon from '@/assets/discord-icon.png'

interface DiscordIconProps {
  className?: string
}

export function DiscordIcon({ className }: DiscordIconProps) {
  return (
    <img
      src={discordIcon}
      alt=""
      aria-hidden
      className={cn('h-4 w-4 shrink-0 object-contain', className)}
    />
  )
}
