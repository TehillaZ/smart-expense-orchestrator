export type WorkflowStepId = 'received' | 'processing' | 'decision' | 'payment'

export type WorkflowStepState = 'complete' | 'current' | 'upcoming' | 'failed' | 'compensated'

export type WorkflowStep = {
  id: WorkflowStepId
  label: string
  state: WorkflowStepState
}

const RECEIVED = new Set(['Received'])
const PROCESSING = new Set(['Processing'])
const DECISION = new Set(['AutoApproved', 'Escalated', 'HumanApproved', 'Rejected'])
const PAYMENT = new Set(['PaymentPending', 'Paid', 'PaymentFailed', 'Compensated'])

function phaseIndex(status: string): number {
  if (RECEIVED.has(status)) {
    return 0
  }

  if (PROCESSING.has(status)) {
    return 1
  }

  if (DECISION.has(status)) {
    return 2
  }

  if (PAYMENT.has(status)) {
    return 3
  }

  return 1
}

function paymentTerminalState(
  backendStatus: string,
): Pick<WorkflowStep, 'label' | 'state'> | null {
  switch (backendStatus) {
    case 'Paid':
      return { label: 'Paid', state: 'complete' }
    case 'PaymentFailed':
      return { label: 'Payment failed', state: 'failed' }
    case 'Compensated':
      return { label: 'Compensated', state: 'compensated' }
    default:
      return null
  }
}

export function getWorkflowSteps(backendStatus: string): WorkflowStep[] {
  const currentPhase = phaseIndex(backendStatus)
  const paymentTerminal = paymentTerminalState(backendStatus)

  const steps: WorkflowStep[] = [
    { id: 'received', label: 'Received', state: 'upcoming' },
    { id: 'processing', label: 'Processing', state: 'upcoming' },
    { id: 'decision', label: 'Decision', state: 'upcoming' },
    { id: 'payment', label: 'Payment', state: 'upcoming' },
  ]

  return steps.map((step, index) => {
    if (index < currentPhase) {
      return { ...step, state: 'complete' }
    }

    if (index === currentPhase) {
      if (step.id === 'payment' && paymentTerminal) {
        return { ...step, ...paymentTerminal }
      }

      return { ...step, state: 'current' }
    }

    return step
  })
}

export function formatBackendStatusLabel(status: string): string {
  return status.replace(/([a-z])([A-Z])/g, '$1 $2')
}

export function isPaymentCompensationStatus(status: string): boolean {
  return status === 'Compensated' || status === 'PaymentFailed'
}

export function isAuditFailureLabel(text: string): boolean {
  const normalized = text.toLowerCase()
  return (
    normalized.includes('failed') ||
    normalized.includes('compensat') ||
    normalized.includes('payment.failed')
  )
}
