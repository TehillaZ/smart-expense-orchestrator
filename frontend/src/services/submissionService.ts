import { API_ENDPOINTS } from '../config/api'

import type {

  NotificationResponse,

  SubmissionAcceptedResponse,

  SubmissionRequestPayload,

  SubmissionStatusResponse,

} from '../types/api'

import type { ExpenseCategory, SubmissionDetail, SubmissionRecord } from '../types/submission'

import { mapBackendStatusToUi } from '../utils/mapSubmissionStatus'

import { ApiRequestError, requestJson } from './apiClient'



export type SubmitExpenseInput = {

  vendorName: string

  invoiceNumber: string

  amount: number

  category: ExpenseCategory

}



export async function submitExpense(

  input: SubmitExpenseInput,

): Promise<SubmissionAcceptedResponse> {

  const payload: SubmissionRequestPayload = {

    vendor: input.vendorName,

    invoiceNumber: input.invoiceNumber,

    amount: input.amount,

    category: input.category,

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

  isDuplicateResubmit = false,

): SubmissionRecord {

  const backendStatus = notification?.status ?? intake.status



  return {

    trackingId: intake.trackingId,

    vendor: intake.vendor,

    amount: notification?.amount ?? intake.amount,

    status: mapBackendStatusToUi(backendStatus),

    invoiceNumber: intake.invoiceNumber,

    category: (intake.category as ExpenseCategory | undefined) ?? 'Other',

    backendStatus,

    reason: notification?.reason ?? intake.reason,

    isDuplicateResubmit,

  }

}



function toSubmissionDetail(

  intake: SubmissionStatusResponse,

  notification: NotificationResponse | null,

  isDuplicateResubmit = false,

): SubmissionDetail {

  const record = toSubmissionRecord(intake, notification, isDuplicateResubmit)



  return {

    ...record,

    correlationId: intake.correlationId,

    reason: intake.reason,

    notificationReason: notification?.reason ?? null,

    aiRecommendation: intake.aiRecommendation,

    aiConfidence: intake.aiConfidence,

    citedRules: intake.citedRules ?? undefined,

    policyVersion: intake.policyVersion,

  }

}



async function enrichSubmission(

  summary: SubmissionStatusResponse,

  duplicateIds?: Set<string>,

): Promise<SubmissionRecord> {

  try {

    const [latestStatus, notification] = await Promise.all([

      getSubmissionStatus(summary.trackingId),

      getNotificationOutcome(summary.trackingId),

    ])



    const isDuplicateResubmit = duplicateIds?.has(summary.trackingId) ?? false

    return toSubmissionRecord(latestStatus ?? summary, notification, isDuplicateResubmit)

  } catch {

    return toSubmissionRecord(summary, null, duplicateIds?.has(summary.trackingId) ?? false)

  }

}



export async function loadSubmissionDetail(

  trackingId: string,

  isDuplicateResubmit = false,

): Promise<SubmissionDetail | null> {

  const [intake, notification] = await Promise.all([

    getSubmissionStatus(trackingId),

    getNotificationOutcome(trackingId),

  ])



  if (!intake) {

    return null

  }



  return toSubmissionDetail(intake, notification, isDuplicateResubmit)

}



export async function loadSubmissionHistory(

  duplicateIds?: Set<string>,

): Promise<SubmissionRecord[]> {

  const submissions = await requestJson<SubmissionStatusResponse[]>(API_ENDPOINTS.submissions)



  if (submissions.length === 0) {

    return []

  }



  return Promise.all(submissions.map((submission) => enrichSubmission(submission, duplicateIds)))

}


