import { forwardRef, type InputHTMLAttributes } from 'react'
import { ChevronDown, Search } from 'lucide-react'
import { cn } from '@/utils/cn'

const fieldBase =
  'flex h-9 w-full min-w-0 rounded-lg border border-border bg-bg-panel/80 px-3 text-sm text-text-primary shadow-sm transition-colors focus:border-accent/50 focus:outline-none focus:ring-1 focus:ring-accent/25'

const numberInputStyles =
  '[appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none'

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  icon?: boolean
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, icon, type, ...props }, ref) => (
    <div className="relative min-w-0">
      {icon && (
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-text-secondary" />
      )}
      <input
        ref={ref}
        type={type}
        className={cn(
          fieldBase,
          'placeholder:text-text-secondary/70',
          type === 'number' && numberInputStyles,
          icon && 'pl-9',
          className,
        )}
        {...props}
      />
    </div>
  ),
)
Input.displayName = 'Input'

export const Textarea = forwardRef<
  HTMLTextAreaElement,
  React.TextareaHTMLAttributes<HTMLTextAreaElement>
>(({ className, ...props }, ref) => (
  <textarea
    ref={ref}
    className={cn(
      fieldBase,
      'min-h-[80px] resize-none py-2 placeholder:text-text-secondary/70',
      className,
    )}
    {...props}
  />
))
Textarea.displayName = 'Textarea'

export function Label({ className, ...props }: React.LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label
      className={cn('text-xs font-medium text-text-secondary uppercase tracking-wide', className)}
      {...props}
    />
  )
}

export function Select({
  className,
  children,
  ...props
}: React.SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <div className="relative min-w-0">
      <select
        className={cn(
          fieldBase,
          'cursor-pointer appearance-none pr-9',
          className,
        )}
        {...props}
      >
        {children}
      </select>
      <ChevronDown
        className="pointer-events-none absolute right-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-text-secondary/80"
        aria-hidden
      />
    </div>
  )
}
