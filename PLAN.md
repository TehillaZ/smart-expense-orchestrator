# ApprovalFlow Project Plan

## Scope

This plan covers only:

- Must-Have requirements: `M1-M18`
- Nice-to-Have requirements: `N1-N6`
- Development process requirements: `D1-D7`

Bonus requirements are intentionally not included yet.

## Recommended Technologies

- Backend: `.NET 9`, ASP.NET Core Minimal APIs
- Microservices: Intake, Approval, Payment, Notification
- Service communication: Dapr service invocation and Dapr pub/sub
- State: Dapr state store backed by PostgreSQL for durable workflow, payment, audit, and idempotency data
- Message broker: Redis Streams or RabbitMQ through Dapr pub/sub
- Secrets: Dapr secret store, local file secret store for development
- API Gateway: YARP reverse proxy or Ocelot, with rate limiting
- Frontend: React + Vite
- Database: PostgreSQL as the durable source of truth; Redis/RabbitMQ can support messaging and local infrastructure
- AI provider: swappable interface with a stub provider for tests and CI
- Observability: structured JSON logs with correlation id; OpenTelemetry later as nice-to-have
- CI: GitHub Actions
- Containers: Dockerfiles per service + Docker Compose

---

# Frontend Plan

## UI Principles

- Keep the UI minimal, clean, and demo-friendly — not a full design system.
- Use React + Vite with functional components and hooks.
- All API calls go through the API Gateway / IntakeService entry point from Docker Compose.
- Every screen should show plain-language status and decision reasons, not raw backend codes.
- Use a simple top navigation bar with role-based links once auth exists.
- Layout: single-page app with routed screens, light background, card-based content, clear primary actions.

Suggested visual style:
- Header: product name `ApprovalFlow`, current role badge, navigation links.
- Content: centered column, max width ~960px, white cards on light gray background.
- Status badges: color-coded pills (`Received`, `Processing`, `AutoApproved`, `Escalated`, `Paid`, etc.).
- Forms: labeled inputs, inline validation errors, one primary submit button.
- Tables/lists: compact rows with key fields first, detail view on click.

## Screen Inventory

| Screen | Priority | Covers | Purpose |
|--------|----------|--------|---------|
| Submit Expense | Must-Have | `M7`, `F1` | Submit invoice/expense and receive tracking id |
| Track Submission | Must-Have | `M7`, `F2` | View status, reason, and final decision |
| Escalation Queue | Must-Have for demo | `F4`, `F5`, `F6` | Approver reviews escalated items |
| Escalation Detail | Must-Have for demo | `F4`, `F5` | Approve, reject, or request more info |
| Controller Dashboard | Should-Have | `F8` | Throughput and auto vs human approval metrics |
| Policy Settings | Should-Have | `F7`, `M13` | Change approval ceiling and policy thresholds |
| Audit Trail | Should-Have | `F9` | View full decision history by tracking/correlation id |
| Login / Role Switch | Nice-to-Have | `N1` | Submitter, Approver, Admin access control |

## Screen Designs

### 1. Submit Expense (`/submit`)

Who uses it:
- Submitter / employee

What it looks like:
- Page title: `Submit Expense`
- Card with a short form:
  - Invoice id
  - Vendor name
  - Category
  - Amount
  - Currency
  - Description / notes
  - Optional attachment reference (no OCR required)
- Primary button: `Submit for Approval`
- Secondary link: `Track an existing submission`

After submit:
- Show success panel with tracking id in large text
- Button: `View Status`
- Small note: `Processing continues in the background`

API:
- `POST /submissions` through gateway
- Expect `202 Accepted` with tracking id

### 2. Track Submission (`/status/:trackingId` or `/track`)

Who uses it:
- Submitter

What it looks like:
- Search box at top: enter tracking id
- Status card showing:
  - Tracking id
  - Current status badge
  - Plain-language reason
  - Submitted amount and vendor
  - Timeline/steps: Received → Processing → Decision → Payment
  - Last updated timestamp
- If final decision exists:
  - Decision summary box (`AutoApproved`, `HumanApproved`, `Rejected`, `Paid`, `PaymentFailed`, etc.)
  - Payment outcome if relevant
- Refresh button: `Check Again`

API:
- `GET /submissions/{trackingId}` through gateway

### 3. Escalation Queue (`/approvals`)

Who uses it:
- Approver / manager

What it looks like:
- Page title: `Escalation Queue`
- Filter bar: status, category, amount range
- Table/list of escalated items only:
  - Invoice id
  - Vendor
  - Amount
  - Category
  - AI recommendation (`Approve`, `Reject`, `Escalate`, `RequestInfo`)
  - Confidence score
  - Submitted date
  - Action button: `Review`
- Empty state: `No escalated items right now`

API:
- `GET /approvals/queue` through gateway

### 4. Escalation Detail (`/approvals/:trackingId`)

Who uses it:
- Approver

What it looks like:
- Two-column layout on desktop, stacked on mobile
- Left card: submission details
  - Invoice data
  - Amount
  - Category
  - Notes
- Right card: AI recommendation panel
  - Recommendation badge
  - Confidence
  - Cited policy rules
  - AI reasoning summary
- Bottom action bar with 3 buttons:
  - `Approve`
  - `Reject`
  - `Request More Info`
- Optional comment box for approver note

After action:
- Success toast/message
- Redirect back to queue
- Item disappears from escalated list once resumed

API:
- `GET /approvals/{trackingId}`
- `POST /approvals/{trackingId}/decision`

### 5. Controller Dashboard (`/dashboard`)

Who uses it:
- Controller / finance

What it looks like:
- Page title: `Approval Operations`
- Top metric cards:
  - Total submissions today
  - Auto-approval rate
  - Escalation rate
  - Total auto-approved amount
  - Total human-approved amount
- Simple chart or bar summary:
  - Auto vs human approvals
  - Rejections
  - Payment failures
- Recent activity table:
  - Tracking id
  - Status
  - Amount
  - Decision type
  - Timestamp

API:
- `GET /reports/summary` through gateway

### 6. Policy Settings (`/admin/policy`)

Who uses it:
- Admin / controller

What it looks like:
- Page title: `Policy & Autonomy Settings`
- Form sections:
  - Auto-approval ceiling amount
  - Allowed categories
  - Blocked vendors or keywords
  - Escalation thresholds
- Read-only preview of current policy version
- Save button: `Update Policy`
- Warning text: `Changes apply to new submissions immediately`

API:
- `GET /policy`
- `PUT /policy`

### 7. Audit Trail (`/audit/:trackingId`)

Who uses it:
- Auditor / admin / demo reviewer

What it looks like:
- Search by tracking id or correlation id
- Vertical timeline card showing:
  - Extracted invoice data
  - Policy version used
  - Rules applied
  - AI recommendation and reasoning
  - Human decision if any
  - Payment result
  - Correlation id copied to clipboard
- Designed for evaluator demo and debugging

API:
- `GET /audit/{trackingId}` through gateway

### 8. Login / Role Switch (`/login`) — Nice-to-Have

Who uses it:
- All roles

What it looks like:
- Simple login card with role selector for local demo:
  - Submitter
  - Approver
  - Admin
- After login, navbar shows only allowed screens

API:
- JWT auth through gateway once `N1` is implemented

## Frontend Navigation

Must-Have navigation for first demo:
- `Submit`
- `Track`
- `Approvals` (can be hidden until escalation flow exists)

Expanded navigation after backend features exist:
- `Submit`
- `Track`
- `Approvals`
- `Dashboard`
- `Policy`
- `Audit`

## Frontend Build Phases

Phase 1 — Must-Have demo (`M7`):
1. Submit Expense
2. Track Submission
3. Basic shared layout and status badges

Phase 2 — Workflow demo:
4. Escalation Queue
5. Escalation Detail

Phase 3 — Controller/audit demo:
6. Controller Dashboard
7. Policy Settings
8. Audit Trail

Phase 4 — Nice-to-Have:
9. Login / role-based routing

## Frontend Tests

- Submit form validates required fields before API call.
- Successful submit shows tracking id.
- Track screen renders each workflow status correctly.
- Approver queue shows only escalated items.
- Approver detail exposes AI recommendation, confidence, and cited rules.
- Dashboard renders auto-approved vs human-approved totals separately.
- UI works against gateway endpoints, not direct service ports.

---

# Must-Have Plan

## Step 1: Define Architecture First

Goal:
Create `ARCHITECTURE.md` with service boundaries, request flow, Dapr usage, payment compensation flow, and key ADRs.

Need to know:
Decide which service owns each responsibility:

- Intake Service: receives submissions and returns tracking id
- Approval Service: policy checks, AI recommendation, auto-approval or escalation
- Payment Service: reservation/payment/rollback logic
- Notification Service: final result notification
- API Gateway: single external entry point
- UI: submit item and view status

Risk:
Starting code before boundaries are clear can cause duplicated logic and unclear ownership.

Tests:
- Architecture has a Mermaid system diagram.
- Architecture has a Mermaid sequence diagram for submit-to-decision.
- Architecture has a payment saga diagram with compensation/rollback.
- Every must-have requirement maps to at least one service.
- ADRs exist for the autonomy posture, payment consistency approach, Dapr usage, state persistence, and LLM provider strategy.

## Step 2: Repository Hygiene

Goal:
Prepare the private GitHub monorepo for evaluation.

Need to know:
Everything needed to run and test must be inside the repo.

Technologies:
GitHub, GitHub Flow, `.gitignore`, `.env.example`, `LICENSE`, README.

Risk:
Committed secrets, missing setup files, missing project references, or unclear branch discipline can fail evaluation.

Tests:
- `git status` does not show generated junk like `bin`, `obj`, or `node_modules`.
- `.env.example` exists and contains placeholder values only.
- No real API keys are committed.
- `backend/ApprovalFlow.sln` includes all backend projects.
- Feature work is done on branches and merged through pull requests.
- `main` contains only what should be evaluated and stops receiving commits after `12/07`.
- Required evaluators are added as repository contributors before submission.

## Step 3: Containerize Every Service

Goal:
Each backend service must have its own Dockerfile.

Technologies:
Docker, Docker Compose, ASP.NET Core container images.

Risk:
Current `docker-compose.yml` references Dockerfiles that do not exist yet, so the system cannot build.

Tests:
- `docker compose config` succeeds.
- `docker compose build` succeeds.
- Each service starts in a container.
- Each service exposes a health endpoint.

## Step 4: Complete Docker Compose Infrastructure

Goal:
A single `docker compose up` should start the whole system.

Technologies:
Docker Compose, Redis or RabbitMQ, PostgreSQL if used, Dapr sidecars.

Risk:
`depends_on` does not guarantee services are healthy, only started.

Tests:
- `docker compose up` starts all services.
- Redis/message broker is reachable.
- Dapr sidecars start for every service.
- Restarting a service does not break the whole stack.
- Healthchecks pass.

## Step 5: Add Dapr Components

Goal:
Use Dapr for required communication and infrastructure.

Need to implement:
- Dapr service invocation
- Dapr pub/sub
- Dapr state store
- Dapr secrets

Technologies:
Dapr, Redis/RabbitMQ, Dapr component YAML files.

Risk:
The assignment requires all of these. Using normal HTTP only is not enough.

Tests:
- One service calls another through Dapr service invocation.
- Submission processing is triggered by a Dapr pub/sub event.
- Submission/workflow state is saved through Dapr state.
- Secrets are loaded through Dapr secret store.

## Step 6: Add API Gateway

Goal:
Expose only the API Gateway externally.

Technologies:
YARP or Ocelot, ASP.NET Core rate limiting.

Risk:
If every service is directly exposed, requirement `M6` is not fully satisfied.

Tests:
- UI/API users call only the gateway.
- Gateway routes submit/status/approval actions correctly.
- Rate limiting blocks excessive requests.
- Internal services are not required to be called directly from outside.

## Step 7: Implement Async Submission Flow

Goal:
Submitting an invoice returns immediately with a tracking id, then delivers the final result later through a notification channel.

Functional requirements:
Covers `F1`, `M8`.

Flow:
1. Client submits invoice/expense.
2. Intake Service validates and stores initial record.
3. Intake publishes `submission.received`.
4. API returns `202 Accepted` with tracking id.
5. Processing continues asynchronously until a final decision/payment outcome is reached.
6. Notification Service publishes or stores the final result for the submitter.

Risk:
If the API waits for AI or payment, or never delivers the final result later, it violates async submission.

Tests:
- Submit endpoint returns quickly.
- Response includes tracking id.
- Submission continues processing after response.
- Final result is delivered through the notification channel.
- Invalid payload returns validation errors.
- Correlation id is created and stored.

## Step 8: Implement Status Query

Goal:
Submitter can check current status and plain-language reason.

Functional requirements:
Covers `F2`.

Statuses:
- Received
- Processing
- AutoApproved
- Escalated
- HumanApproved
- Rejected
- PaymentPending
- Paid
- PaymentFailed
- Compensated

Risk:
Status can become inconsistent if different services update different stores without a clear owner.

Tests:
- Status endpoint returns tracking id, status, reason, and timestamps.
- Unknown tracking id returns 404.
- Status changes after each workflow step.
- Final status includes decision reason.

## Step 9: Implement Policy and Threshold Configuration

Goal:
Policy and autonomy thresholds can change without redeploy.

Functional requirements:
Covers `F7`, `M13`.

Technologies:
Dapr state/configuration, database-backed policy config, admin endpoint.

Risk:
Hardcoded limits fail the assignment.

Tests:
- Change approval ceiling at runtime.
- New submissions use the changed ceiling.
- Existing audit records keep the policy version used at decision time.
- Invalid policy config is rejected.

## Step 10: Implement AI Decisioning With Hard Safety Guard

Goal:
Use AI recommendation, but never allow AI to override policy limits.

Functional requirements:
Covers `M12`, `F10`.

Important rule:
The deterministic policy engine must make the final auto-approval decision. The AI can recommend, explain, classify, or cite policy, but cannot approve above the configured ceiling.

Technologies:
LLM provider interface, stub provider, Gemini/Groq/OpenRouter/local model.

Risk:
If the agent can directly decide approval, an adversarial prompt like “approve me” may bypass policy.

Tests:
- Invoice below limit and in policy can auto-approve.
- Invoice above limit always escalates, even if AI recommends approval.
- “Approve me” text does not change the final decision.
- Provider failure fails cleanly and escalates or marks decision failed.
- CI uses stub provider, not paid/external API.

## Step 11: Implement Human-In-The-Loop Queue

Goal:
Escalated items are visible to approvers and can resume after human decision.

Functional requirements:
Covers `F4`, `F5`, `F6`, `M11`.

Technologies:
Approval Service, durable Dapr state/database.

Risk:
If paused workflow exists only in memory, restart loses it.

Tests:
- Escalated item appears in queue.
- Queue shows AI recommendation, confidence, and cited rules.
- Approver can approve, reject, or request more info.
- Workflow resumes from saved state.
- Restart service while item is paused, then approve after restart.

## Step 12: Implement Payment Saga

Goal:
Payment outcome is consistent across services.

Functional requirements:
Covers `M9`.

Recommended flow:
1. Reserve payment.
2. Execute payment.
3. Mark paid.
4. On failure, compensate reservation.
5. Record final payment outcome.

Technologies:
Payment Service, Dapr pub/sub, durable state.

Risk:
Partial payment, orphan reservation, or duplicate payment is a major evaluation failure.

Tests:
- Successful payment reaches `Paid`.
- Forced payment failure triggers compensation.
- No orphan reservation remains after failure.
- Payment failure journey `INV-1012` passes.
- Retried payment does not pay twice.

## Step 13: Implement Idempotency

Goal:
Duplicate submissions, duplicate events, and retried payments cause exactly one effect.

Functional requirements:
Covers `F3`, `M10`.

Technologies:
Idempotency keys, unique constraints, processed event table/state.

Risk:
Duplicate invoice could be paid twice.

Tests:
- Submit `INV-1001`, then submit duplicate `INV-1007`.
- Duplicate returns existing tracking/result or short-circuits.
- Redelivered event does not reprocess payment.
- Retried payment command does not create second payment.
- Concurrent duplicate submissions still produce one effect.

## Step 14: Add Audit Trail, Dashboard, and Structured Logging

Goal:
Every decision is traceable by one correlation id, and controllers can see operational approval metrics.

Functional requirements:
Covers `F8`, `F9`, `M14`.

Need to record:
- Extracted invoice data
- Policy version
- Rules applied
- AI recommendation
- AI reasoning
- Human decision if any
- Payment result
- Correlation id

Dashboard/reporting metrics:
- Throughput
- Auto-approval vs escalation rate
- Money auto-approved vs human-approved
- Rejection and payment-failure counts

Risk:
Logs alone are not enough for audit if they are not queryable or consistent.

Tests:
- Every log line includes correlation id.
- Audit endpoint returns complete decision trail.
- Reporting endpoint returns throughput and approval-rate metrics.
- Dashboard shows auto-approved money separately from human-approved money.
- Auto-approved item shows deterministic rule decision.
- Human-approved item shows human actor/action.
- Payment failure shows compensation result.

## Step 15: Build Minimal UI

Goal:
User can submit item and view status/decision.

Functional requirements:
Covers `M7`.

Technologies:
React + Vite.

Screens to implement first:
- Submit Expense
- Track Submission

Optional in same step if time allows:
- Escalation Queue
- Escalation Detail

See `Frontend Plan` above for screen layout, navigation, and build phases.

Risk:
A UI that only looks nice but cannot drive real backend flows is not enough.

Tests:
- Submit invoice from UI.
- UI shows tracking id.
- UI can refresh/view status.
- UI shows final decision reason.
- UI can show escalated queue if approver features are included.

## Step 16: Add One-Command Verification

Goal:
A single command proves the required journeys.

Functional requirements:
Covers `D5`.

Required journeys:
- Auto-approve: `INV-1001`
- Escalate and resume: `INV-1003`
- Duplicate paid once: `INV-1007`
- Payment failure and compensation: `INV-1012`

Also test:
- At least 2 items auto-approve with no human.
- “Approve me” note does not flip the decision.

Risk:
Manual demo without repeatable verification is weaker and easier to fail.

Tests:
- One command prints pass/fail.
- Command exits non-zero on failure.
- Runs locally without paid services.
- Uses sample fixtures.

## Step 17: Add CI Pipeline and Runtime Quality Gates

Goal:
Every push runs quality gates and automated tests, and the implementation satisfies clean-code and runtime reliability expectations.

Functional requirements:
Covers `M15`, `M16`, `M17`.

Technologies:
GitHub Actions.

Recommended checks:
- Backend restore/build/test
- Frontend install/lint/build
- Docker Compose config validation
- Verification command if possible
- Health endpoint checks for every service
- Error-handling tests for provider failures and invalid inputs
- Configuration test proving the LLM provider is swappable without code changes

Risk:
CI that depends on real LLM API keys may be flaky or fail. Silent provider failures, weak separation of concerns, or missing health checks can also fail `M15`.

Tests:
- CI passes on GitHub.
- CI fails on broken backend build.
- CI fails on broken frontend build.
- CI uses stub AI provider.
- Services fail cleanly and visibly on LLM/provider errors.
- Health checks expose readiness/liveness for each service.
- Business logic stays separated from transport, persistence, and provider adapters.

## Step 18: Write README and API Documentation

Goal:
Evaluator can understand, run, and test the project.

Functional requirements:
Covers `M18`, `D4`, `D6`, `D7`.

README should include:
- Purpose
- Technologies
- Architecture diagram
- How to run
- How to test
- Verification command
- Known trade-offs
- Autonomy posture
- Link to API documentation
- Link to the 2-5 minute demo recording
- Submission checklist with repo URL, demo URL, and required contributors

Risk:
A working project can lose points if it is hard to run or understand.

Tests:
- Fresh clone can follow README.
- `docker compose up` instructions work.
- API docs are available through Swagger/Scalar/OpenAPI.
- Demo recording shows the four key journeys.
- Submission checklist is complete before the deadline.

---

# Nice-To-Have Plan

## N1: Add AuthN/AuthZ With Roles

Goal:
Protect routes by role.

Roles:
- Submitter
- Approver
- Admin

Technologies:
Self-signed JWT, ASP.NET Core authentication/authorization, gateway validation.

Risk:
Auth can slow development if added too early.

Tests:
- Submitter can submit and view own status.
- Approver can access escalation queue.
- Admin can update policy.
- Submitter cannot approve escalated item.
- Unauthenticated request is rejected.

## N2: Add CD Pipeline

Goal:
After CI passes, build and publish deployable artifacts.

Technologies:
GitHub Actions, Docker image build, GitHub Container Registry.

Risk:
Publishing broken or secret-containing images.

Tests:
- CD runs only after CI succeeds.
- Images are built with expected tags.
- No secrets are printed in logs.
- Published image can run locally.

## N3: Add Reliability Extras

Goal:
Improve resilience with outbox and bulkhead/throttling.

Technologies:
Outbox table/state, background publisher, Polly or .NET rate limiting/bulkhead policies.

Risk:
Without outbox, saved state and published events can diverge.

Tests:
- If event publishing fails, outbox retries later.
- Duplicate outbox messages remain idempotent.
- AI provider calls are throttled.
- Payment Service cannot be overwhelmed by retries.

## N4: Add OpenTelemetry Observability

Goal:
Create end-to-end trace across services and AI calls.

Technologies:
OpenTelemetry, Jaeger/Tempo, correlation id propagation.

Risk:
Tracing can be noisy if added without consistent correlation rules.

Tests:
- One trace shows gateway, intake, approval, payment, notification.
- Trace includes AI provider span or stub span.
- Trace id/correlation id appears in logs.
- Metrics show throughput and failure counts.

## N5: Add RAG Over Policy Document

Goal:
Retrieve relevant policy clauses instead of sending full policy to the LLM.

Technologies:
Local embeddings or simple vector store, policy chunking, retrieval layer.

Risk:
Bad retrieval can miss important policy clauses and reduce safety.

Tests:
- Invoice category retrieves relevant policy clause.
- Unknown category retrieves fallback/general rule.
- Final deterministic guard still prevents over-limit approval.
- AI prompt includes cited retrieved clauses.

## N6: Add Multi-Layer Tests

Goal:
Improve confidence beyond simple unit tests.

Test layers:
- Unit tests for policy engine and idempotency
- Integration tests for Dapr components/state
- API tests for gateway routes
- End-to-end tests for four required journeys

Risk:
Too many slow tests can make CI painful.

Tests:
- Unit tests run fast.
- Integration tests can run locally.
- E2E test uses Docker Compose.
- CI separates fast checks from slower verification.

---

# Suggested Build Order

1. Architecture and ADRs
2. Repo hygiene and solution setup
3. Dockerfiles and Compose
4. Dapr components
5. Gateway
6. Async submission and status
7. Policy configuration
8. AI decisioning with hard guard
9. Human escalation
10. Payment saga
11. Idempotency
12. Audit, dashboard, and logging
13. UI
14. Verification command
15. CI and runtime quality gates
16. README, API docs, and demo
17. Nice-to-have features only after must-have flow works