import { FormEvent, useState } from 'react'

type TrackSubmissionSearchProps = {
  onSearch: (trackingId: string) => void
  isLoading?: boolean
}

export function TrackSubmissionSearch({ onSearch, isLoading = false }: TrackSubmissionSearchProps) {
  const [trackingId, setTrackingId] = useState('')

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = trackingId.trim()
    if (!trimmed) {
      return
    }

    onSearch(trimmed)
  }

  return (
    <form
      className="flex flex-wrap items-end gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
      onSubmit={handleSubmit}
    >
      <div className="min-w-[16rem] flex-1">
        <label htmlFor="track-search" className="mb-1.5 block text-sm font-medium text-slate-700">
          Track by tracking id
        </label>
        <input
          id="track-search"
          value={trackingId}
          onChange={(event) => setTrackingId(event.target.value)}
          placeholder="Paste a tracking id"
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-100"
        />
      </div>
      <button
        type="submit"
        disabled={isLoading}
        className="rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isLoading ? 'Loading…' : 'Check Status'}
      </button>
    </form>
  )
}
