const PALETTE = [
  '#FFC300',
  '#3B82F6',
  '#10B981',
  '#8B5CF6',
  '#EC4899',
  '#F97316',
  '#06B6D4',
  '#EF4444',
  '#84CC16',
  '#6366F1',
]

export function getEmployeeColor(index: number): string {
  return PALETTE[index % PALETTE.length]
}

export function generateId(): string {
  return crypto.randomUUID()
}
