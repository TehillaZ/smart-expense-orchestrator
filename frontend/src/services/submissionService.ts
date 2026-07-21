import { API_ENDPOINTS } from '../config/api'
import type {
  NotificationResponse,
  SubmissionAcceptedResponse,
  SubmissionRequestPayload,
  SubmissionStatusResponse,
} from '../types/api'
import {
  toBackendCategory,
  type ExpenseCategory,
  type SubmissionDetail,
  type SubmissionRecord,
} from '../types/submission'
import { mapBackendStatusToUi } from '../utils/mapSubmissionStatus'
import { ApiRequestError, requestJson } from './apiClient'

export type SubmitExpenseInput = {
  vendorName: string
  invoiceNumber: string
  amount: number
  category: ExpenseCategory
}

function hasDuplicateResubmits(intake: SubmissionStatusResponse): boolean {
  return (intake.duplicateResubmitCount ?? 0) > 0
}

export async function submitExpense(
  input: SubmitExpenseInput,
): Promise<SubmissionAcceptedResponse> {
  const payload: SubmissionRequestPayload = {
    vendor: input.vendorName,
    invoiceNumber: input.invoiceNumber,
    amount: input.amount,
    category: toBackendCategory(input.category),
  }

  return requestJson<SubmissionAcceptedResponse>(API_ENDPOINTS.submissions, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  })
}

export async function getSubmissionStatus(
  trackingId: string,
): Promise<SubmissionStatusResponse | null> {
  try {
    return await requestJson<SubmissionStatusResponse>(
      API_ENDPOINTS.submissionById(trackingId),
    )
  } catch (error) {
    if (error instanceof ApiRequestError && error.status === 404) {
      return null
    }

    throw error
  }
}

async function getNotificationOutcome(
  trackingId: string,
): Promise<NotificationResponse | null> {
  try {
    return await requestJson<NotificationResponse>(API_ENDPOINTS.notificationById(trackingId))
  } catch (error) {
    if (error instanceof ApiRequestError && (error.status === 404 || error.status === 0)) {
      return null
    }

    throw error
  }
}

function toSubmissionRecord(
  intake: SubmissionStatusResponse,
  notification: NotificationResponse | null,
): SubmissionRecord {
  const backendStatus = notification?.status ?? intake.status
  const duplicateResubmitCount = intake.duplicateResubmitCount ?? 0

  return {
    trackingId: intake.trackingId,
    vendor: intake.vendor,
    amount: notification?.amount ?? intake.amount,
    status: mapBackendStatusToUi(backendStatus),
    invoiceNumber: intake.invoiceNumber,
    category: (intake.category as ExpenseCategory | undefined) ?? 'Other',
    backendStatus,
    reason: notification?.reason ?? intake.reason,
    isDuplicateResubmit: duplicateResubmitCount > 0,
    duplicateResubmitCount,
    humanDecision: intake.humanDecision ?? null,
    humanDecisionComment: intake.humanDecisionComment ?? null,
    humanDecisionActor: intake.humanDecisionActor ?? null,
  }
}

function toSubmissionDetail(
  intake: SubmissionStatusResponse,
  notification: NotificationResponse | null,
): SubmissionDetail {
  const record = toSubmissionRecord(intake, notification)

  return {
    ...record,
    correlationId: intake.correlationId,
    reason: intake.reason,
    notificationReason: notification?.reason ?? null,
    aiRecommendation: intake.aiRecommendation,
    aiConfidence: intake.aiConfidence,
    citedRules: intake.citedRules ?? undefined,
    policyVersion: intake.policyVersion,
    autonomyCeilingAtDecision: intake.autonomyCeilingAtDecision,
  }
}

export async function loadSubmissionDetail(
  trackingId: string,
): Promise<SubmissionDetail | null> {
  const [intake, notification] = await Promise.all([
    getSubmissionStatus(trackingId),
    getNotificationOutcome(trackingId),
  ])

  if (!intake) {
    return null
  }

  return toSubmissionDetail(intake, notification)
}

export async function loadSubmissionHistory(): Promise<SubmissionRecord[]> {
  const submissions = await requestJson<SubmissionStatusResponse[]>(API_ENDPOINTS.submissions)

  // Use the list payload directly to avoid N+1 status/notification calls that trip gateway rate limits.
  return submissions.map((submission) => toSubmissionRecord(submission, null))
}

export { hasDuplicateResubmits }
