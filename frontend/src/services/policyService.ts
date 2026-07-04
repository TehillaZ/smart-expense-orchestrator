import { API_ENDPOINTS } from '../config/api'
import type { PolicySettings, PolicySettingsUpdatePayload } from '../types/api'
import { requestJson } from './apiClient'

export async function loadPolicySettings(): Promise<PolicySettings> {
  return requestJson<PolicySettings>(API_ENDPOINTS.policy)
}

export async function updatePolicySettings(
  payload: PolicySettingsUpdatePayload,
): Promise<PolicySettings> {
  return requestJson<PolicySettings>(API_ENDPOINTS.policy, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  })
}
