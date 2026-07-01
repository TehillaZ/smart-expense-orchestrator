# ApprovalFlow Progress Tracker

This document records the real current progress of the ApprovalFlow platform. It should only include work that has actually been completed.

Run the terminal chart:

```bash
node scripts/show-progress.mjs
```

Or with Python:

```bash
python scripts/show_progress.py
```

Add `--verbose` to either command for daily details.

---

## Day 1 - 10%

**Focus:** Project initialization, assignment understanding, and first infrastructure planning.

**Completed:**

- Created or started the monorepo structure with `backend/`, `frontend/`, and `infra/` areas.
- Added initial planning notes in `PLAN.md` and infrastructure review notes in `NOTES.md`.
- Added or reviewed the first `infra/docker-compose.yml` draft for Redis, backend services, and Dapr sidecars.
- Added this progress tracker and scripts for showing progress in the terminal.

**In progress / blockers:**

- The project is not finished yet; it is still at the first-day planning/bootstrap stage.
- Need to confirm which files are real project deliverables and which are local notes that should stay out of GitHub.
- Need to verify the current infrastructure files against the assignment requirements before treating them as complete.
- Need to implement the real backend business APIs, Dapr components, API gateway, UI, tests, and CI.

**Outcome:** Day 1 records the project setup and planning baseline only. Future days should be added after the work is actually completed.

---

## Day 2 - Current Stage

**Focus:** Validate architecture and infrastructure before starting real business APIs.

**Already in place:**

- `ARCHITECTURE.md` now describes the four-service architecture: Intake, Approval, Payment, and Notification.
- `backend/ApprovalService/Data/policy.md` defines the policy rules and autonomy posture.
- All four backend projects are included in `backend/ApprovalFlow.sln`.
- Each backend service has a Dockerfile.
- `infra/docker-compose.yml` includes Redis, four backend services, and four Dapr sidecars.
- Dapr component files exist for pub/sub, state, and local secrets.
- `.gitignore` and `.dockerignore` exist.

**Tasks to complete today:**

- Verify the architecture document matches the actual four-service implementation and does not describe services that will not be built now.
- Write the first ADR drafts yourself: service boundaries, Dapr communication, payment saga, AI safety guard, and policy configuration.
- Run Docker Compose validation with `docker compose -f infra/docker-compose.yml config`.
- Run a container build check with `docker compose -f infra/docker-compose.yml build`.
- Decide whether to align the Dapr runtime image version with the `Dapr.AspNetCore` package version.
- Define the first API contracts before coding:
  - `POST /submissions`
  - `GET /submissions/{trackingId}`
  - `GET /approvals/queue`
  - `POST /approvals/{trackingId}/decision`
  - `GET /health`
- Confirm each endpoint belongs to the correct service.
- Keep implementation for the next stage: current service code still contains template `/weatherforecast` endpoints.

**Day 2 verification checklist:**

- `ARCHITECTURE.md` is understandable and matches the current service count.
- ADR drafts exist and are written in your own words.
- Docker Compose config validation passes.
- Docker image build check passes or has a documented error to fix.
- The next coding target is clear: replace template endpoints with real Intake and Approval API contracts.

**Outcome target:** Day 2 is complete when the design and infrastructure are verified enough to safely start real backend API implementation on Day 3.

---

## Machine-readable progress data

The block below is consumed by `scripts/show-progress.mjs` and `scripts/show_progress.py`.

```progress-data
{
  "project": "ApprovalFlow",
  "repository": "ApprovalFlow-Platform",
  "summary": "Real progress tracker for the ApprovalFlow assignment. The project is currently in Day 2 architecture and infrastructure validation.",
  "days": [
    {
      "day": 1,
      "progress": 10,
      "title": "Project planning and bootstrap",
      "focus": "Assignment understanding, repository setup, infrastructure review, and planning",
      "completed": [
        "Created or started the backend, frontend, and infra project areas",
        "Added PLAN.md with project steps and requirement mapping",
        "Added NOTES.md with infrastructure review findings",
        "Added progress tracker scripts for terminal progress display"
      ],
      "blockers": [
        "Project is still in Day 1 and not feature-complete",
        "Need to decide which local notes should be ignored by Git",
        "Need to implement real service APIs, Dapr components, gateway, UI, tests, and CI"
      ]
    },
    {
      "day": 2,
      "progress": 20,
      "title": "Architecture and infrastructure validation",
      "focus": "Verify architecture, Dapr/Docker setup, and API contracts before business API implementation",
      "completed": [
        "Architecture document describes the four-service implementation",
        "Policy file defines the approval rules and default autonomy posture",
        "Solution includes the four backend service projects",
        "Dockerfiles and Dapr component files exist for local infrastructure"
      ],
      "blockers": [
        "Backend services still contain template weatherforecast endpoints",
        "Docker Compose config and build checks still need to be run",
        "API Gateway and real business endpoints are not implemented yet"
      ],
      "planned": [
        "Write ADR drafts in your own words",
        "Run docker compose config and build checks",
        "Define first API contracts for submissions, status, approvals, and health",
        "Decide whether to align Dapr runtime and package versions"
      ]
    }
  ]
}
```

