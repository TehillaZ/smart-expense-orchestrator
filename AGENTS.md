# Project: Microservices System based on Dapr and React

## 1. Project Structure (Monorepo Layout)
- The project is strictly divided as follows:
  - `backend/` - Contains the .NET 9 microservices (Intake, Approval, Payment, Notification).
  - `frontend/` - Contains the React application.
  - `infra/` - Contains infrastructure files: `docker-compose.yml` and the `dapr/components` folder.
- Never create duplicated configuration files outside of these directories.

## 2. Architecture & Documentation Rule
- **CRITICAL:** Do NOT create or modify any file related to the architecture documentation (such as `ARCHITECTURE.md` or similar). The user manages the architecture documentation independently.

## 3. Backend Development Rules (.NET 9 + Dapr)
- **Version:** All C# projects must target .NET 9.
- **Dapr Integration:** Service-to-service communication, state management, and pub/sub messaging must go through the Dapr SDK exclusively.
  - Pub/Sub Component Name: `order-pubsub`
  - State Store Component Name: `approval-state`
- **Rule:** Never write direct connection code to Redis. Always use the Dapr Client.

## 4. Frontend Development Rules (React)
- Use modern functional components and Hooks.
- The Frontend communicates with the Backend via the API Gateway / IntakeService port defined in the Docker Compose configuration.

## 5. Automation & Testing (Auto-Tests)
- **Automated Testing:** You are required to include or suggest automated tests (Auto-Tests) for every business logic component or API endpoint you generate. 
- Ensure that unit tests or integration tests can be run seamlessly to verify the code stability.

## 6. Agent Execution Style
- Provide focused, modular, and concise solutions. Do not rewrite entire files if only a specific function needs modification.
- Always follow the sequence defined in `PLAN.md`. Do not skip ahead until the current task is fully verified and functional.
- Include robust error handling (Try-Catch) and structured logging across all endpoints and event subscribers.
- **CRITICAL GITHUB RULE:** Do NOT automatically perform any Git commit or push to GitHub. You must explicitly ask the user for permission and receive approval before staging, committing, or uploading any code.

## 7. Industry Production Standards
- **Security:** NEVER hardcode secrets, API keys, or passwords. Always use Environment Variables or configuration files (`appsettings.json`).
- **Naming Conventions:** Follow strict language standards (PascalCase for C# methods/classes, camelCase for variables; PascalCase for React components and file names).
- **Asynchronous Code:** All I/O bound operations, database queries, and network calls in .NET must use `async/await` pattern.
- **Clean Code:** Use Constants or Enums instead of magic strings or magic numbers. Keep functions short, focused, and single-purpose.



