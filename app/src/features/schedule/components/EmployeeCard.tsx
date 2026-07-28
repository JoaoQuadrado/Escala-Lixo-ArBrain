import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { motion } from 'framer-motion'
import { GripVertical, Palmtree, UserX } from 'lucide-react'
import type { Employee } from '@/types'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { cn } from '@/utils/cn'
import { useUI } from '@/contexts/UIContext'

interface EmployeeCardProps {
  employee: Employee
  isDragging?: boolean
  isOverlay?: boolean
  queuePosition?: number
  compact?: boolean
}

export function EmployeeCard({ employee, isDragging, isOverlay, queuePosition, compact }: EmployeeCardProps) {
  const { setSelectedEmployeeId, setEmployeePanelOpen } = useUI()

  const handleClick = () => {
    setSelectedEmployeeId(employee.id)
    setEmployeePanelOpen(true)
  }

  return (
    <motion.div
      layout
      whileHover={!isDragging ? { scale: 1.02, y: -2 } : undefined}
      transition={{ type: 'spring', stiffness: 400, damping: 25 }}
      onClick={handleClick}
      className={cn(
        'group relative cursor-grab rounded-lg border border-border bg-bg-panel/90 shadow-sm transition-shadow duration-200',
        compact ? 'p-2' : 'p-3',
        'hover:border-white/15 hover:shadow-md hover:shadow-black/20',
        isDragging && 'opacity-40',
        isOverlay && 'drag-overlay cursor-grabbing border-accent/30 bg-bg-panel',
      )}
      style={{ borderLeftColor: employee.color, borderLeftWidth: 3 }}
    >
      <div className={cn('flex items-start', compact ? 'gap-1.5' : 'gap-2.5')}>
        {queuePosition !== undefined ? (
          <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-amber-500/15 text-[10px] font-bold text-amber-300">
            {queuePosition}
          </span>
        ) : (
          !compact && (
            <GripVertical className="mt-1 h-4 w-4 shrink-0 text-text-secondary/40 opacity-0 transition-opacity group-hover:opacity-100" />
          )
        )}
        <Avatar
          name={employee.name}
          src={employee.photoUrl}
          color={employee.color}
          size="sm"
          className={compact ? '!h-7 !w-7 !text-[10px]' : undefined}
        />
        <div className="min-w-0 flex-1">
          <p className={cn('truncate font-medium text-text-primary', compact ? 'text-xs' : 'text-sm')}>
            {employee.name}
          </p>
          {!compact && (
            <div className="mt-1.5 flex gap-1">
              {employee.onVacation && (
                <Badge variant="warning">
                  <Palmtree className="mr-0.5 h-2.5 w-2.5" />
                  Férias
                </Badge>
              )}
              {employee.absent && (
                <Badge variant="danger">
                  <UserX className="mr-0.5 h-2.5 w-2.5" />
                  Ausente
                </Badge>
              )}
            </div>
          )}
        </div>
      </div>
    </motion.div>
  )
}

interface SortableEmployeeCardProps {
  employee: Employee
  id: string
  queuePosition?: number
  compact?: boolean
  className?: string
}

export function SortableEmployeeCard({ employee, id, queuePosition, compact, className }: SortableEmployeeCardProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id,
    data: { employee, type: 'employee' },
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners} className={className}>
      <EmployeeCard employee={employee} isDragging={isDragging} queuePosition={queuePosition} compact={compact} />
    </div>
  )
}
