import { motion } from 'framer-motion'
import {
  BookOpen,
  CalendarDays,
  Clock,
  History,
  Home,
  MessageSquare,
  RefreshCw,
  SendHorizontal,
  Settings,
  Trash2,
  Users,
  Workflow,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { cn } from '@/utils/cn'

interface TutorialSection {
  id: string
  icon: typeof Home
  title: string
  summary: string
  steps: string[]
  tips?: string[]
}

const SECTIONS: TutorialSection[] = [
  {
    id: 'inicio',
    icon: Home,
    title: 'Tela inicial',
    summary: 'Ao abrir o app, a tela de boas-vindas mostra a escala de hoje e um atalho para entrar.',
    steps: [
      'Veja quem está escalado hoje no painel «Escala de hoje».',
      'Use o botão de envio (ícone →) para publicar o dia no Discord, se necessário.',
      'Clique em «Entrar» ou pressione Enter para ir ao quadro de escalas.',
      'Volte a qualquer momento pelo ícone de casa na barra lateral.',
    ],
  },
  {
    id: 'escalas',
    icon: Trash2,
    title: 'Escalas da semana',
    summary: 'O quadro principal organiza segunda a sexta, fila de espera e quem está de fora.',
    steps: [
      'Cada coluna representa um dia útil; arraste funcionários entre dias ou para a fila.',
      'A fila de espera guarda quem aguarda entrar na escala quando houver vaga.',
      '«Missão cumprida» (bloqueados) são quem já cumpriu o limite da semana e não entra de novo.',
      'Clique num funcionário para ver detalhes, férias ou ausência no painel lateral.',
      'Use o botão ↻ na barra superior para gerar uma nova escala (a anterior vai para o histórico).',
    ],
    tips: [
      'Só é possível enviar ao Discord com escala válida e API conectada.',
      'Fins de semana não entram na escala de limpeza.',
    ],
  },
  {
    id: 'funcionarios',
    icon: Users,
    title: 'Funcionários',
    summary: 'Cadastro e manutenção da equipe que participa da rotação.',
    steps: [
      'Adicione colaboradores com nome, cor e foto opcional.',
      'Marque férias ou ausência quando alguém não puder entrar na escala.',
      'Edite ou remova registros pela lista ou pelo painel lateral na tela de escalas.',
    ],
  },
  {
    id: 'historico',
    icon: History,
    title: 'Histórico',
    summary: 'Consulte escalas passadas arquivadas automaticamente.',
    steps: [
      'Cada vez que uma nova semana é gerada, a escala anterior é salva aqui.',
      'Selecione um item na lista à esquerda para ver o quadro completo daquela semana.',
      'Útil para conferir quem estava escalado em semanas anteriores.',
    ],
  },
  {
    id: 'rotacao',
    icon: Workflow,
    title: 'Rotação',
    summary: 'Entenda como o sistema escolhe quem entra, repete ou fica de fora.',
    steps: [
      'Veja o fluxo visual de novos escalados, repetidos e limites da semana.',
      'A análise mostra a tendência para a próxima semana antes de gerar.',
      'A simulação projeta até 4 semanas com base nas regras actuais.',
      'Use esta aba para validar se a rotação está justa antes de confirmar.',
    ],
  },
  {
    id: 'discord',
    icon: MessageSquare,
    title: 'Envios ao Discord',
    summary: 'Notificações manuais e automáticas no canal configurado.',
    steps: [
      '«Escala hoje» — publica quem limpa no dia corrente (seg–sex).',
      '«Escala semana» — envia a prévia da semana inteira (ideal na segunda).',
      'Na tela inicial, o botão → envia só o dia de hoje, com confirmação.',
      'Em Configurações, defina o webhook, GIFs e textos das mensagens.',
    ],
    tips: [
      'Lembrete diário automático: seg–sex, no horário «Lembrete diário».',
      'Prévia semanal automática: segunda-feira, no horário «Prévia semanal».',
      'Requer webhook Discord activo, escala válida e API a correr.',
    ],
  },
  {
    id: 'instalacao',
    icon: BookOpen,
    title: 'Instalação noutra máquina',
    summary: 'Instalador Windows com API embutida — abrir o app basta, sem terminal.',
    steps: [
      'Na máquina de build, execute .\\scripts\\build-installer.ps1 para gerar o instalador em app/release/.',
      'Copie o .exe para a outra máquina e instale (atalho no Desktop é criado automaticamente).',
      'Na primeira abertura, o app cria %APPDATA%\\EscalaLixo\\appsettings.json com a connection string do Supabase.',
      'Se precisar, edite esse ficheiro e reabra o app — a API sobe sozinha em segundo plano.',
    ],
  },
  {
    id: 'configuracoes',
    icon: Settings,
    title: 'Configurações',
    summary: 'Webhook, mensagens, GIFs e agendamento dos envios.',
    steps: [
      'Cole o URL do webhook do canal Discord onde as mensagens devem aparecer.',
      'Personalize o texto do lembrete diário (placeholders como {dia} e {nomes}).',
      'Faça upload de GIFs e escolha qual usar na prévia e no lembrete diário.',
      'Ajuste os horários e o intervalo de verificação do agendamento automático.',
      'Teste envios manuais pelos botões na própria página de configurações.',
    ],
  },
]

const QUICK_REF = [
  { icon: RefreshCw, label: 'Gerar escala', where: 'Barra superior — Escalas' },
  { icon: SendHorizontal, label: 'Enviar hoje', where: 'Barra superior ou tela inicial' },
  { icon: MessageSquare, label: 'Enviar semana', where: 'Barra superior — Escalas' },
  { icon: CalendarDays, label: 'Semana actual', where: 'Subtítulo da barra superior' },
  { icon: Clock, label: 'Envio automático', where: 'Configurações → Agendamento' },
]

export function TutorialPage() {
  return (
    <div className="mx-auto max-w-3xl space-y-6 pb-8">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        className="space-y-2"
      >
        <div className="flex items-center gap-2">
          <BookOpen className="h-5 w-5 text-accent" />
          <h2 className="text-lg font-semibold text-text-primary">Como usar o Escala Lixo</h2>
        </div>
        <p className="text-sm text-text-secondary leading-relaxed">
          Guia rápido das funcionalidades do programa. Use a barra lateral para navegar entre as
          secções descritas abaixo.
        </p>
      </motion.div>

      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: 0.05 }}
      >
        <Card glass>
          <CardHeader>
            <CardTitle className="text-xs uppercase tracking-wider text-text-secondary">
              Referência rápida
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="grid gap-2 sm:grid-cols-2">
              {QUICK_REF.map(({ icon: Icon, label, where }) => (
                <li
                  key={label}
                  className="flex items-start gap-2.5 rounded-lg border border-border/60 bg-bg-panel/40 px-3 py-2.5"
                >
                  <Icon className="mt-0.5 h-4 w-4 shrink-0 text-accent" />
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-text-primary">{label}</p>
                    <p className="text-xs text-text-secondary">{where}</p>
                  </div>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      </motion.div>

      <div className="space-y-4">
        {SECTIONS.map((section, index) => (
          <motion.div
            key={section.id}
            initial={{ opacity: 0, y: 16 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.08 + index * 0.04 }}
          >
            <Card hover className="overflow-hidden">
              <CardHeader className="flex-row items-start gap-3 space-y-0 pb-3">
                <div
                  className={cn(
                    'flex h-9 w-9 shrink-0 items-center justify-center rounded-lg',
                    'bg-accent/15 text-accent ring-1 ring-accent/25',
                  )}
                >
                  <section.icon className="h-4 w-4" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <CardTitle className="text-base">{section.title}</CardTitle>
                    <Badge variant="secondary">{index + 1}</Badge>
                  </div>
                  <p className="mt-1 text-sm text-text-secondary leading-relaxed">
                    {section.summary}
                  </p>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                <ol className="space-y-2">
                  {section.steps.map((step, stepIndex) => (
                    <li key={stepIndex} className="flex gap-3 text-sm text-text-primary">
                      <span className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-white/5 text-[10px] font-bold text-accent">
                        {stepIndex + 1}
                      </span>
                      <span className="pt-0.5 leading-relaxed text-text-secondary">{step}</span>
                    </li>
                  ))}
                </ol>
                {section.tips && section.tips.length > 0 && (
                  <div className="rounded-lg border border-accent/20 bg-accent/5 px-3 py-2.5">
                    <p className="mb-1.5 text-[11px] font-semibold uppercase tracking-wide text-accent">
                      Dicas
                    </p>
                    <ul className="space-y-1">
                      {section.tips.map((tip) => (
                        <li key={tip} className="text-xs text-text-secondary leading-relaxed">
                          • {tip}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </CardContent>
            </Card>
          </motion.div>
        ))}
      </div>
    </div>
  )
}
