import { useState, useEffect } from 'react'
import type { Employee } from '@/types'
import { Dialog } from '@/components/ui/Dialog'
import { Button } from '@/components/ui/Button'
import { Input, Label } from '@/components/ui/Input'
import { getEmployeeColor } from '@/utils/colors'

interface EmployeeFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  employee?: Employee | null
  employeeCount: number
  onSave: (data: Omit<Employee, 'id'> | Employee) => void
}

export function EmployeeFormDialog({
  open,
  onOpenChange,
  employee,
  employeeCount,
  onSave,
}: EmployeeFormDialogProps) {
  const [name, setName] = useState('')
  const [photoUrl, setPhotoUrl] = useState('')
  const [discordUser, setDiscordUser] = useState('')

  useEffect(() => {
    if (employee) {
      setName(employee.name)
      setPhotoUrl(employee.photoUrl ?? '')
      setDiscordUser(employee.discordUser ?? '')
    } else {
      setName('')
      setPhotoUrl('')
      setDiscordUser('')
    }
  }, [employee, open])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!name.trim()) return

    const data = {
      name: name.trim(),
      role: employee?.role ?? '',
      color: employee?.color ?? getEmployeeColor(employeeCount),
      photoUrl: photoUrl.trim() || undefined,
      discordUser: discordUser.trim() || undefined,
      onVacation: employee?.onVacation ?? false,
      absent: employee?.absent ?? false,
      notes: employee?.notes,
    }

    if (employee) {
      onSave({ ...data, id: employee.id })
    } else {
      onSave(data)
    }
    onOpenChange(false)
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title={employee ? 'Editar colaborador' : 'Adicionar colaborador'}
      description={
        employee
          ? 'Atualize os dados do colaborador'
          : 'Nome e foto. O Discord é usado nas menções quando entra na escala.'
      }
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="name">Nome</Label>
          <Input
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Nome completo"
            required
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="photo">Foto (opcional)</Label>
          <Input
            id="photo"
            value={photoUrl}
            onChange={(e) => setPhotoUrl(e.target.value)}
            placeholder="https://..."
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="discord">ID Discord (opcional)</Label>
          <Input
            id="discord"
            value={discordUser}
            onChange={(e) => setDiscordUser(e.target.value)}
            placeholder="Ex.: 361476217209225218"
          />
          <p className="text-xs text-text-secondary">
            Se vazio, o bot tenta resolver pelo nome quando o colaborador estiver na escala.
          </p>
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
            Cancelar
          </Button>
          <Button type="submit">{employee ? 'Salvar' : 'Adicionar'}</Button>
        </div>
      </form>
    </Dialog>
  )
}
