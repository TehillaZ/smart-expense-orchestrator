const STORAGE_KEY = 'approvalflow.duplicate-tracking-ids'

function readIds(): Set<string> {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return new Set()
    }

    const parsed: unknown = JSON.parse(raw)
    if (!Array.isArray(parsed)) {
      return new Set()
    }

    return new Set(parsed.filter((id): id is string => typeof id === 'string'))
  } catch {
    return new Set()
  }
}

function writeIds(ids: Set<string>): void {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify([...ids]))
}

export function markDuplicateTrackingId(trackingId: string): void {
  const ids = readIds()
  ids.add(trackingId)
  writeIds(ids)
}

export function isDuplicateTrackingId(trackingId: string): boolean {
  return readIds().has(trackingId)
}

export function loadDuplicateTrackingIds(): Set<string> {
  return readIds()
}
