# Project Mercury 🚀

A multi-tenant speed-to-lead quoting engine and dispatch dashboard for home service operators. 

When a prospect submits a request (via Thumbtack, web form, etc.), Mercury ingests the payload, evaluates the business's deterministic pricing formula, and dispatches a personalized SMS quote and booking link within seconds.

## 🏗️ Architecture (Monorepo)

This repository contains both the client and server applications:

*   **/src/frontend**: React + TypeScript client. Provides the dashboard for operators to view leads, adjust pricing formulas, and manage RBAC (Role-Based Access Control) user settings.
*   **/src/backend**: C# / .NET 10 Web API. Handles webhook ingestion, Entity Framework Core multi-tenant data isolation, pricing logic, and Twilio API integrations.
*   **Database**: PostgreSQL (containerized for local development), accessed through the `Npgsql.EntityFrameworkCore.PostgreSQL` provider.

## 📂 Project Structure

```text
mercury/
├── src/
│   ├── backend/          # .NET 10 solution and API projects
│   └── frontend/         # React application (Vite)
├── docker-compose.yml    # Spins up the local PostgreSQL database and dependencies
├── mercury_ai_spec.md    # Engineering specification: stack, entities, security rules
└── README.md