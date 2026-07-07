import { API_ENDPOINTS } from '../config/api'
import type { ComplianceReportResponse, ReportingSummaryResponse } from '../types/api'
import { requestJson } from './apiClient'

export async function loadReportingSummary(): Promise<ReportingSummaryResponse> {
  return requestJson<ReportingSummaryResponse>(API_ENDPOINTS.reportsSummary)
}

export async function loadComplianceReport(): Promise<ComplianceReportResponse> {
  return requestJson<ComplianceReportResponse>(API_ENDPOINTS.reportsCompliance)
}
