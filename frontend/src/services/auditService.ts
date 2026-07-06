import { API_ENDPOINTS } from '../config/api'
import type { AuditTrailResponse } from '../types/api'
import { requestJson } from './apiClient'

export async function loadAuditTrail(trackingId: string): Promise<AuditTrailResponse> {
  return requestJson<AuditTrailResponse>(API_ENDPOINTS.auditById(trackingId.trim()))
}
