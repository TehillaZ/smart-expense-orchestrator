# ApprovalFlow Platform

Microservice-based invoice and expense approval system built with .NET 9, Dapr, and React. Submissions return a tracking id immediately, policy and AI evaluate each item asynchronously, safe cases auto-approve, risky cases escalate to a human, and approved items flow through a payment saga with compensation on failure.

## Technologies

- **Backend:** .NET 9, ASP.NET Core Minimal APIs
- **Microservices:** Intake, Approval, Payment, Notification
- **Integration:** Dapr (service invocation, pub/sub, state store)
- **Gateway:** YARP reverse proxy
- **Frontend:** React + Vite + TypeScript + Tailwind CSS
- **Infrastructure:** Docker Compose, Redis
- **AI:** Swappable `IInvoiceAiProvider` with stub provider for local dev and CI

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

1. Clone the repository and copy environment placeholders:

   ```powershell
   copy .env.example .env
   ```

2. Start the full stack:

   ```powershell
   docker compose -f infra/docker-compose.yml up --build -d
   ```

3. Open the API Gateway:

   - Health: http://localhost:8080/health
   - Submit: `POST http://localhost:8080/submissions`

4. Start the frontend (optional, for UI):

   ```powershell
   cd frontend
   npm install
   npm run dev
   ```

   The Vite dev server proxies API calls to `http://localhost:8080`.

## Run Tests

### Backend unit and journey tests

```powershell
dotnet test backend/ApprovalFlow.sln
dotnet test backend/ApiGateway.Tests/ApiGateway.Tests.csproj
```

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

Exit code is non-zero on any failure.

## API Documentation

When services run in Development mode, OpenAPI specs are available:

- Intake: http://localhost:5001/openapi/v1.json
- Approval: http://localhost:5002/openapi/v1.json
- Payment: http://localhost:5003/openapi/v1.json
- Notification: http://localhost:5004/openapi/v1.json

External clients should use the gateway at `http://localhost:8080` only.

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
| GET | `/health` | Gateway health check |

## Autonomy Posture

The AI provider recommends and explains decisions but **never has final approval authority**. The deterministic `PolicyEngine` enforces ceilings, category rules, and confidence thresholds. An adversarial prompt such as "approve me" cannot override configured limits.

## Known Trade-offs

- Pub/sub is the primary async path; direct service invocation is used only as a fallback when publish fails.
- Dashboard metrics are pre-aggregated in Intake state for demo simplicity rather than a separate analytics database.
- Role switching in the UI is a local demo feature; JWT auth is not yet implemented.
- CI runs unit/journey tests and compose validation; full Docker journey verification is a manual/local step.

## CI

GitHub Actions runs on push and pull requests:

- Backend restore, build, and test
- Frontend lint and build
- Docker Compose config validation

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
