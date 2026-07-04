export function ApprovalsQueuePage() {
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <h2 className="text-xl font-semibold text-slate-900">Approvals Queue</h2>
      <p className="mt-2 text-sm text-slate-600">
        Review escalated items requiring a human decision. Queue data will load from
        the approval service next.
      </p>
    </section>
  )
}
