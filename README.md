# ApprovalFlow Platform

Microservice-based invoice and expense approval system built with .NET 9, Dapr, and React. Submissions return a tracking id immediately, policy and AI evaluate each item asynchronously, safe cases auto-approve, risky cases escalate to a human, and approved items flow through a payment saga with compensation on failure.

## Technologies

- **Backend:** .NET 9, ASP.NET Core Minimal APIs
- **Microservices:** Intake, Approval, Payment, Notification
- **Integration:** Dapr (service invocation, pub/sub, state store)
- **Gateway:** YARP reverse proxy
- **Frontend:** React + Vite + TypeScript + Tailwind CSS
- **Infrastructure:** Docker Compose, Redis
- **AI:** Swappable `IInvoiceAiProvider` (`Stub` default, `Gemini` / `Groq` / `OpenRouter` via config + Dapr secret `LlmProvider:ApiKey`)

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for service boundaries, request flow, payment saga, and ADRs.

```text
UI → API Gateway → Intake Service
                      ↓ pub/sub
                 Approval Service → Payment Service → Notification Service
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for full stack)
- PowerShell 5.1+ (for journey verification script)

## Quick Start (Docker)

1. Clone the repository and copy environment / secrets placeholders:

   ```powershell
   copy .env.example .env
   copy infra\dapr\components\secrets.example.json infra\dapr\components\secrets.json
   ```

   `secrets.json` is gitignored. Dapr needs this file at startup even when using the Stub LLM (placeholder values are fine).

2. Start the full stack:

   ```powershell
   docker compose -f infra/docker-compose.yml up --build -d
   ```

3. Open the stack:

   - UI: http://localhost:5173
   - API Gateway health: http://localhost:8080/health
   - API docs (Scalar): http://localhost:8080/scalar
   - Submit: `POST http://localhost:8080/submissions`

4. Local frontend development (optional, hot reload):

   ```powershell
   cd frontend
   npm install
   npm run dev
   ```

   The Vite dev server proxies API calls to `http://localhost:8080`. Compose already serves the built UI on port `5173`.

## Git Hooks

Install local hooks to keep the branch synced with `main` and enforce Conventional Commits:

```powershell
pwsh -File scripts/install-git-hooks.ps1
```

Hooks:

- `pre-commit` — fetches and merges `origin/main` before each commit
- `commit-msg` — validates lowercase `feat:`, `fix:`, `test:`, `docs:`, and `chore:` prefixes

## Run Tests

### Backend unit and journey tests

```powershell
dotnet test backend/ApprovalFlow.sln
```

The solution includes the API Gateway and all service test projects, including in-process journey tests for the four required verification scenarios.

### Frontend lint and build

```powershell
cd frontend
npm ci
npm run lint
npm run build
```

### End-to-end journey verification (requires running Docker stack)

```powershell
pwsh -File scripts/verify-journeys.ps1
```

This script verifies:

- `INV-1001` — auto-approve and pay
- `INV-1003` — escalate, human approve, pay
- `INV-1007` — duplicate submission paid once
- `RS-90021` — payment failure and compensation
- Adversarial "approve me" prompt does not bypass policy ceiling
- Two eligible submissions auto-approve without human intervention
- Escalated approval resumes after approval service restart (`INV-1003-RESTART`)

Exit code is non-zero on any failure.

## API Documentation (D4)

OpenAPI is **auto-generated from the Minimal API endpoints**. Each service (and the gateway) also hosts an interactive **Scalar** UI so you can explore and try requests in the browser.

### Scalar UI (recommended)

| Surface | Scalar UI | OpenAPI JSON |
|---------|-----------|--------------|
| API Gateway (preferred for clients) | http://localhost:8080/scalar | http://localhost:8080/openapi/v1.json |
| Intake | http://localhost:5001/scalar | http://localhost:5001/openapi/v1.json |
| Approval | http://localhost:5002/scalar | http://localhost:5002/openapi/v1.json |
| Payment | http://localhost:5003/scalar | http://localhost:5003/openapi/v1.json |
| Notification | http://localhost:5004/scalar | http://localhost:5004/openapi/v1.json |

### How to exercise endpoints

1. Open the **gateway** Scalar UI: http://localhost:8080/scalar
2. Call `POST /auth/login` with a role (`Submitter`, `Approver`, or `Admin`) to get a JWT
3. In Scalar, set the `Authorization` header to `Bearer <token>`
4. Call proxied business routes (`/submissions`, `/approvals/queue`, `/policy`, …)

You can also import any `/openapi/v1.json` URL into **Postman** or **Swagger Editor**.

External clients should use the gateway at `http://localhost:8080` only.

## Observability

OpenTelemetry metrics and traces export to Jaeger (`http://localhost:16686`) when the compose stack is up. Async pub/sub events carry `traceParent` so Approval → Payment → Notification spans stitch into the same trace as the gateway/intake request. Every business path also propagates `X-Correlation-Id`.

## Key Endpoints (via Gateway)

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/submissions` | Submit expense, returns tracking id |
| GET | `/submissions/{id}` | Check submission status |
| GET | `/approvals/queue` | List escalated items |
| POST | `/approvals/{id}/decision` | Human approve/reject/request info |
| GET/PUT | `/policy` | Read/update policy thresholds |
| GET | `/reports/summary` | Dashboard metrics |
| GET | `/audit/{trackingId}` | Full audit trail |
| GET | `/reports/compliance` | Auto-approval limit compliance report |
| GET | `/notifications/{trackingId}` | Final notification outcome for a submission |
| GET | `/health` | Gateway health check |

## Autonomy Posture

The AI provider recommends and explains decisions but **never has final approval authority**. The deterministic `PolicyEngine` enforces ceilings, category rules, and confidence thresholds. An adversarial prompt such as "approve me" cannot override configured limits.

Swap the LLM without code changes:

- Default: `LlmProvider__Provider=Stub` (used in CI and local compose)
- External: set `LlmProvider__Provider=Groq` (recommended when Gemini TLS fails), `Gemini`, or `OpenRouter`, and put a real key in `infra/dapr/components/secrets.json` under `LlmProvider:ApiKey`
- Optional resilience: set `LlmProvider__FallbackProvider=Groq` (with `LlmProvider:FallbackApiKey`) plus `RetryCount` / `RetryDelayMs` — primary retries transient errors, then falls back once; both failing escalates to human review
- Groq example: `LlmProvider__Model=llama-3.3-70b-versatile`, `LlmProvider__BaseUrl=https://api.groq.com/openai/v1/`
- Copy `infra/dapr/components/secrets.example.json` to `secrets.json` for local secrets (gitignored)
- Provider HTTP/API failures escalate the submission (never silent approve)

## Known Trade-offs

- Pub/sub is the primary async path; direct service invocation is used only as a fallback when publish fails.
- Dashboard metrics are pre-aggregated in Intake state for demo simplicity rather than a separate analytics database.
- Role switching in the UI is a local demo feature; JWT auth is not yet implemented.
- CI runs backend/frontend tests, journey tests, compose config validation, and a compose build smoke check.
- Full Docker journey verification remains available locally via `scripts/verify-journeys.ps1`.

## CI

GitHub Actions runs on push and pull requests:

- Backend restore, build, and test (including ApiGateway and journey tests)
- Frontend lint, test, and build
- Docker Compose config validation and image build smoke check

See [.github/workflows/ci.yml](.github/workflows/ci.yml).

## Submission Checklist

Before evaluation deadline:

- [ ] Repository URL shared with evaluators
- [ ] Evaluators added as contributors
- [ ] `docker compose up` works from fresh clone
- [ ] `dotnet test` passes
- [ ] `pwsh -File scripts/verify-journeys.ps1` passes against running stack
- [ ] 2–5 minute demo recording uploaded and linked here
- [ ] Demo URL or local run instructions verified

## Demo Recording

_Add link to your 2–5 minute demo video here._

## License

This project is licensed under the MIT License — see [LICENSE](LICENSE).
