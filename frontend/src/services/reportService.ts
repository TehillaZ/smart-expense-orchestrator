import { API_ENDPOINTS } from '../config/api'
import type { ReportingSummaryResponse } from '../types/api'
import { requestJson } from './apiClient'

export async function loadReportingSummary(): Promise<ReportingSummaryResponse> {
  return requestJson<ReportingSummaryResponse>(API_ENDPOINTS.reportsSummary)
}
