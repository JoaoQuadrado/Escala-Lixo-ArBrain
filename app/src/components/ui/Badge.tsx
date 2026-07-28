import { cn } from '@/utils/cn'

interface BadgeProps {
  children: React.ReactNode
  variant?: 'default' | 'accent' | 'success' | 'warning' | 'danger' | 'secondary'
  className?: string
}

const variants = {
  default: 'bg-white/10 text-text-primary',
  accent: 'bg-accent/15 text-accent border border-accent/25',
  success: 'bg-success/15 text-success border border-success/25',
  warning: 'bg-warning/15 text-warning border border-warning/25',
  danger: 'bg-danger/15 text-danger border border-danger/25',
  secondary: 'bg-bg-panel-hover text-text-secondary',
}

export function Badge({ children, variant = 'default', className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide',
        variants[variant],
        className,
      )}
    >
      {children}
    </span>
  )
}
