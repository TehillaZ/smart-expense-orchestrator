import { getWorkflowSteps, type WorkflowStepState } from '../../utils/workflowSteps'

type WorkflowStepperProps = {
  backendStatus: string
}

const STATE_STYLES: Record<WorkflowStepState, string> = {
  complete: 'border-emerald-500 bg-emerald-50 text-emerald-800',
  current: 'border-blue-500 bg-blue-50 text-blue-800 ring-2 ring-blue-200',
  upcoming: 'border-slate-200 bg-white text-slate-400',
  failed: 'border-rose-500 bg-rose-50 text-rose-800 ring-2 ring-rose-200',
  compensated: 'border-amber-500 bg-amber-50 text-amber-900 ring-2 ring-amber-200',
}

const STATE_CAPTIONS: Record<WorkflowStepState, string> = {
  complete: 'complete',
  current: 'current',
  upcoming: 'upcoming',
  failed: 'failed',
  compensated: 'compensated',
}

export function WorkflowStepper({ backendStatus }: WorkflowStepperProps) {
  const steps = getWorkflowSteps(backendStatus)

  return (
    <ol className="flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-2">
      {steps.map((step, index) => (
        <li key={step.id} className="flex flex-1 items-center gap-2">
          <div
            className={`flex flex-1 flex-col rounded-lg border px-3 py-2 text-center text-xs font-semibold ${STATE_STYLES[step.state]}`}
          >
            <span>{step.label}</span>
            <span className="mt-0.5 text-[10px] font-normal uppercase tracking-wide opacity-80">
              {STATE_CAPTIONS[step.state]}
            </span>
          </div>
          {index < steps.length - 1 ? (
            <span className="hidden text-slate-300 sm:inline" aria-hidden="true">
              →
            </span>
          ) : null}
        </li>
      ))}
    </ol>
  )
}
