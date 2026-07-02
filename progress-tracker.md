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

## Day 3 - 30%

**Focus:** Replace template backend endpoints with the first real ApprovalFlow API contracts.

**Why this is next:**

- Architecture and infrastructure are now defined.
- Docker Compose config/build checks passed.
- `IntakeService` and `ApprovalService` still needed to move from template endpoints to real API contracts.
- The assignment requires asynchronous submission, tracking id, status lookup, and human approval workflow.

**Completed:**

- In `IntakeService`, replaced the template `/weatherforecast` endpoint with first real endpoints:
  - `POST /submissions`
  - `GET /submissions/{trackingId}`
  - `GET /health`
- Defined request/response models for the first submission flow:
  - submission request
  - accepted response with tracking id
  - submission status response
  - validation error response
- `POST /submissions` returns `202 Accepted` with a generated tracking id and correlation id.
- Stores an initial in-memory placeholder status of `Received`.
- Status responses include the plain-language reason: `Invoice received and is pending analysis.`
- Added basic validation:
  - vendor is required
  - invoice number is required
  - amount must be greater than zero
  - category is required
- Added a correlation id to the submission response and status data.
- In `ApprovalService`, removed the template endpoint and added first contract stubs:
  - `GET /approvals/queue`
  - `POST /approvals/{trackingId}/decision`
  - `GET /health`
- Added structured logging scopes that include correlation id for the new endpoints.
- Kept business logic simple for Day 3. No AI, Dapr pub/sub workflow, payment saga, or UI was implemented.

**Day 3 verification checklist:**

- `dotnet build backend/ApprovalFlow.sln` passes.
- `docker compose -f infra/docker-compose.yml build` passes.
- `/weatherforecast` is removed from IntakeService and ApprovalService.
- Linter diagnostics report no issues for IntakeService and ApprovalService.
- Local runtime smoke tests pass for IntakeService health, validation, submission creation, and status lookup.
- Local runtime smoke tests pass for ApprovalService health, empty queue, invalid decision validation, and valid decision stub.

**Outcome:** The first real Intake and Approval API contracts exist, compile, build into Docker images, and pass local endpoint smoke tests. The next stage is to connect the submission flow to Dapr state/pub-sub.

---

## Day 4 - 45%

**Focus:** Connect the submission flow to Dapr state and pub/sub, then expose services through a single API Gateway entry point.

**Why this is next:**

- Day 3 left submission storage in memory and did not publish workflow events.
- PLAN Step 5 requires Dapr state, pub/sub, and service communication through Dapr.
- PLAN Step 6 requires a single external entry point with rate limiting before UI work begins.

**Completed:**

- Replaced `InMemorySubmissionStore` with `DaprSubmissionStore` using the `approval-state` Dapr state component.
- Intake `POST /submissions` now publishes `submission.received` through the `order-pubsub` Dapr pub/sub component after saving initial state.
- Added `SubmissionReceivedEvent` model shared between Intake publish and Approval subscribe paths.
- ApprovalService now subscribes to `submission.received` through Dapr CloudEvents at `/events/submission-received`.
- Added `SubmissionReceivedHandler` to move submissions from `Received` to `Processing` in durable Dapr state.
- Added `DaprSubmissionStateStore` in ApprovalService for reading and updating shared workflow state.
- Registered Dapr client, CloudEvents middleware, and subscription handler in both services.
- Added unit test projects to the solution:
  - `IntakeService.Tests` for submission validation and store behavior
  - `ApprovalService.Tests` for decision validation and submission-received handling
  - `ApiGateway.Tests` for correlation id middleware behavior
- Added `ApiGateway` with YARP reverse proxy routing:
  - `POST /submissions` and `GET /submissions/{trackingId}` to IntakeService
  - `/approvals/**` to ApprovalService
  - `GET /health` on the gateway itself
- Added fixed-window rate limiting on proxied gateway routes.
- Added correlation id middleware that creates or forwards `X-Correlation-Id`.
- Added ApiGateway Dockerfile and wired the gateway into `infra/docker-compose.yml` on port `8080`.
- Aligned Dapr runtime and SDK versions to `1.14.4` after the invalid `1.18.4` Docker image tag blocked compose startup.
- Fixed ApiGateway build by adding the missing `Microsoft.AspNetCore.RateLimiting` import for rate limiter setup.
- Verified manual gateway smoke tests: `GET /health`, `POST /submissions`, and `GET /submissions/{trackingId}` through port `8080`.

**Day 4 verification checklist:**

- `dotnet build backend/ApprovalFlow.sln` passes.
- `dotnet test backend/ApprovalFlow.sln` passes for IntakeService, ApprovalService, and ApiGateway unit tests.
- `docker compose -f infra/docker-compose.yml up --build` starts the full stack including gateway and Dapr sidecars.
- Intake writes submission state through Dapr instead of in-memory storage.
- Approval subscribes to `submission.received` and can update status to `Processing`.
- External clients can use the gateway on port `8080` instead of calling service ports directly.

**In progress / blockers:**

- PaymentService and NotificationService are scaffolded only; no business workflow yet.
- Approval queue and decision endpoints are still placeholders, not backed by real escalated state.
- No policy engine, AI guard, payment saga, idempotency, audit, UI, verification command, or CI yet.
- Dapr secrets component exists but is not used in service code yet.
- Frontend is still the default Vite template.

**Outcome:** Submission intake is now durable and event-driven through Dapr, and the platform has a single external API entry point. The next stage is to complete the async workflow: policy checks, AI recommendation with hard safety guard, escalation queue backed by state, and notification delivery.

---

## Current project situation (~45%)

**What works today:**

- Docker Compose stack: Redis, ApiGateway (`8080`), Intake, Approval, Payment, Notification, and Dapr sidecars.
- Submit and track submissions through the gateway.
- Dapr state store for submissions and Dapr pub/sub event from Intake to Approval.
- Unit tests for validation, submission store behavior, approval handler, and gateway correlation id middleware.

**What does not exist yet (per PLAN.md):**

- Policy configuration and AI decisioning with hard safety guard
- Real escalation queue and human approval workflow
- Payment saga and idempotency
- Audit trail, dashboard, and structured reporting
- React submit/track UI
- One-command verification and GitHub Actions CI

**Recommended next step (Day 5):** Implement policy loading and deterministic approval logic in ApprovalService after `Processing`, then wire the escalation queue to real Dapr state.

---

## Machine-readable progress data

The block below is consumed by `scripts/show-progress.mjs` and `scripts/show_progress.py`.

```progress-data
{
  "project": "ApprovalFlow",
  "repository": "ApprovalFlow-Platform",
  "summary": "Real progress tracker for the ApprovalFlow assignment. The project is currently at Day 4: Dapr workflow wiring and API Gateway entry point (~45%).",
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
        "Dockerfiles and Dapr component files exist for local infrastructure",
        "Docker Compose config validation passed",
        "Docker Compose build completed successfully",
        "Dapr runtime image is aligned to the Dapr.AspNetCore package generation"
      ],
      "blockers": [
        "Backend services still contain template weatherforecast endpoints",
        "API Gateway and real business endpoints are not implemented yet"
      ],
      "planned": [
        "Define first API contracts for submissions, status, approvals, and health",
        "Replace template endpoints with real Intake and Approval endpoints"
      ]
    },
    {
      "day": 3,
      "progress": 30,
      "title": "First backend API contracts",
      "focus": "Replace template endpoints with real Intake submission/status and Approval contract stubs",
      "completed": [
        "Replaced IntakeService weatherforecast endpoint with POST /submissions, GET /submissions/{trackingId}, and GET /health",
        "Added IntakeService request/response models, validation, and in-memory submission status storage",
        "Replaced ApprovalService weatherforecast endpoint with approval queue, approval decision, and health endpoint stubs",
        "Added structured logging scopes with correlation id for the new endpoints",
        "Verified dotnet build backend/ApprovalFlow.sln passes",
        "Verified docker compose build passes",
        "Smoke tested IntakeService health, validation, submission creation, and status lookup",
        "Smoke tested ApprovalService health, empty queue, invalid decision validation, and valid decision stub"
      ],
      "blockers": [
        "Dapr pub/sub and state are configured but not used by service code yet",
        "API Gateway and full workflow are not implemented yet"
      ],
      "planned": [
        "Replace in-memory submission storage with Dapr state",
        "Publish submission.received through Dapr pub/sub",
        "Start API Gateway work after service endpoints are stable"
      ]
    },
    {
      "day": 4,
      "progress": 45,
      "title": "Dapr workflow wiring and API Gateway",
      "focus": "Durable submission state, pub/sub event flow, external gateway entry point, and unit tests",
      "completed": [
        "Replaced in-memory Intake storage with DaprSubmissionStore using approval-state",
        "Intake publishes submission.received through order-pubsub after saving state",
        "ApprovalService subscribes to submission.received and moves status to Processing",
        "Added ApiGateway with YARP routing, rate limiting, and correlation id middleware on port 8080",
        "Added IntakeService.Tests, ApprovalService.Tests, and ApiGateway.Tests to the solution",
        "Aligned Dapr runtime image and SDK versions to 1.14.4",
        "Verified dotnet build and unit tests pass",
        "Verified docker compose up --build and manual gateway submit/status smoke tests"
      ],
      "blockers": [
        "Payment and Notification services have no business workflow yet",
        "Approval queue and decision endpoints are still placeholders",
        "No policy engine, AI guard, payment saga, idempotency, audit, UI, verification command, or CI",
        "Dapr secrets component is configured but not used in code yet"
      ],
      "planned": [
        "Implement policy loading and deterministic approval logic in ApprovalService",
        "Back escalation queue with real Dapr state",
        "Start React submit and track UI against the gateway"
      ]
    }
  ]
}
```

