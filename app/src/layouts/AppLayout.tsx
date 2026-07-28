import { Outlet } from 'react-router-dom'
import { Sidebar } from '@/components/layout/Sidebar'
import { TopBar } from '@/components/layout/TopBar'
import { EmployeePanel } from '@/components/layout/EmployeePanel'
import { ToastContainer } from '@/components/ui/Toast'
import { useState } from 'react'
import { EmployeeFormDialog } from '@/features/employees/components/EmployeeFormDialog'
import { useApp, useToast } from '@/contexts/AppContext'
import type { Employee } from '@/types'

export function AppLayout() {
  const { state, addEmployee, updateEmployee } = useApp()
  const { showToast } = useToast()
  const [editEmployee, setEditEmployee] = useState<Employee | null>(null)
  const [formOpen, setFormOpen] = useState(false)

  const handleEditFromPanel = (id: string) => {
    const emp = state.employees.find((e) => e.id === id)
    if (emp) {
      setEditEmployee(emp)
      setFormOpen(true)
    }
  }

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
    <div className="gradient-bg flex h-full">
      <Sidebar />
      <div className="flex flex-1 flex-col min-w-0">
        <TopBar />
        <main className="scrollbar-discreet flex-1 min-h-0 overflow-y-auto p-4">
          <Outlet context={{ openEmployeeForm: () => { setEditEmployee(null); setFormOpen(true) } }} />
        </main>
      </div>
      <EmployeePanel onEdit={handleEditFromPanel} />
      <ToastContainer />

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
