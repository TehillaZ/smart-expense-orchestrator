const gatewayBaseUrl = import.meta.env.VITE_API_GATEWAY_URL ?? ''

export const API_ENDPOINTS = {
  submissions: `${gatewayBaseUrl}/submissions`,
  submissionById: (trackingId: string) => `${gatewayBaseUrl}/submissions/${trackingId}`,
  approvalsQueue: `${gatewayBaseUrl}/approvals/queue`,
  approvalDecision: (trackingId: string) =>
    `${gatewayBaseUrl}/approvals/${trackingId}/decision`,
  policy: `${gatewayBaseUrl}/policy`,
  notificationById: (trackingId: string) =>
    `${import.meta.env.VITE_NOTIFICATION_SERVICE_URL ?? ''}/notifications/${trackingId}`,
} as const
