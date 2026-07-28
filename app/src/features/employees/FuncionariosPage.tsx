import { useState } from 'react'
import { motion } from 'framer-motion'
import { Plus, MoreVertical, Pencil, Trash2 } from 'lucide-react'
import { useApp, useToast, ApiError } from '@/contexts/AppContext'
import { Button } from '@/components/ui/Button'
import { Card } from '@/components/ui/Card'
import { Avatar } from '@/components/ui/Avatar'
import { Badge } from '@/components/ui/Badge'
import { Dropdown } from '@/components/ui/Dropdown'
import { EmployeeFormDialog } from '@/features/employees/components/EmployeeFormDialog'
import type { Employee } from '@/types'

export function FuncionariosPage() {
  const { state, addEmployee, updateEmployee, deleteEmployee } = useApp()
  const { showToast } = useToast()

  const [formOpen, setFormOpen] = useState(false)
  const [editEmployee, setEditEmployee] = useState<Employee | null>(null)

  const handleSave = async (data: Omit<Employee, 'id'> | Employee) => {
    try {
      if ('id' in data) {
        await updateEmployee(data)
        showToast('success', 'Funcionário atualizado')
      } else {
        await addEmployee(data)
        showToast('success', 'Funcionário adicionado')
      }
    } catch (err) {
      showToast('error', err instanceof Error ? err.message : 'Erro ao salvar')
    }
  }

  return (
    <div className="flex h-full flex-col gap-4">
      <div className="flex items-center justify-between shrink-0">
        <div>
          <h2 className="text-lg font-semibold">Funcionários</h2>
          <p className="text-sm text-text-secondary">{state.employees.length} funcionários</p>
        </div>
        <Button
          onClick={() => {
            setEditEmployee(null)
            setFormOpen(true)
          }}
        >
          <Plus className="h-4 w-4" />
          Adicionar
        </Button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3 overflow-y-auto pb-4">
        {state.employees.map((emp, i) => (
          <motion.div
            key={emp.id}
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: i * 0.03 }}
          >
            <Card hover glass className="relative">
              <div className="p-4">
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <Avatar name={emp.name} src={emp.photoUrl} color={emp.color} />
                    <div>
                      <p className="text-sm font-semibold">{emp.name}</p>
                      {emp.discordUser && (
                        <p className="text-xs text-text-secondary truncate">Discord: {emp.discordUser}</p>
                      )}
                    </div>
                  </div>
                  <Dropdown
                    trigger={
                      <button className="rounded-lg p-1 text-text-secondary hover:bg-white/5 cursor-pointer">
                        <MoreVertical className="h-4 w-4" />
                      </button>
                    }
                    items={[
                      {
                        label: 'Editar',
                        icon: <Pencil className="h-3.5 w-3.5" />,
                        onClick: () => {
                          setEditEmployee(emp)
                          setFormOpen(true)
                        },
                      },
                      {
                        label: 'Excluir',
                        icon: <Trash2 className="h-3.5 w-3.5" />,
                        danger: true,
                        onClick: async () => {
                          try {
                            await deleteEmployee(emp.id)
                            showToast('info', `${emp.name} removido`)
                          } catch (err) {
                            showToast('error', err instanceof ApiError ? err.message : 'Erro ao excluir')
                          }
                        },
                      },
                    ]}
                  />
                </div>
                <div className="mt-3 flex gap-1.5">
                  {emp.onVacation && <Badge variant="warning">Férias</Badge>}
                  {emp.absent && <Badge variant="danger">Ausente</Badge>}
                </div>
              </div>
              <div
                className="absolute bottom-0 left-0 right-0 h-0.5 rounded-b-xl"
                style={{ backgroundColor: emp.color }}
              />
            </Card>
          </motion.div>
        ))}
      </div>

      <EmployeeFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        employee={editEmployee}
        employeeCount={state.employees.length}
        onSave={handleSave}
      />
    </div>
  )
}
