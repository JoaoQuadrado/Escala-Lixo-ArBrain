import * as DropdownMenu from '@radix-ui/react-dropdown-menu'
import { cn } from '@/utils/cn'

interface DropdownProps {
  trigger: React.ReactNode
  items: { label: string; icon?: React.ReactNode; onClick: () => void; danger?: boolean }[]
  align?: 'start' | 'center' | 'end'
}

export function Dropdown({ trigger, items, align = 'end' }: DropdownProps) {
  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>{trigger}</DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content
          align={align}
          sideOffset={6}
          className="z-50 min-w-[160px] rounded-lg border border-border bg-bg-panel p-1 shadow-xl animate-in fade-in-0 zoom-in-95"
        >
          {items.map((item) => (
            <DropdownMenu.Item
              key={item.label}
              onClick={item.onClick}
              className={cn(
                'flex items-center gap-2 rounded-md px-3 py-2 text-sm cursor-pointer outline-none transition-colors',
                item.danger
                  ? 'text-danger hover:bg-danger/10 focus:bg-danger/10'
                  : 'text-text-primary hover:bg-white/5 focus:bg-white/5',
              )}
            >
              {item.icon}
              {item.label}
            </DropdownMenu.Item>
          ))}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  )
}
