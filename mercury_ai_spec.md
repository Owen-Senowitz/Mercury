# AI Engineering Specification (Project Mercury)

**Project Name:** Mercury
**System Type:** Multi-Tenant B2B SaaS (Monorepo)
**Objective:** Automated speed-to-lead quoting and Twilio SMS dispatch for home service businesses.
**Core Rule:** All database reads and writes must be strictly partitioned by `TenantId`.

## 1. Technology Stack

- **Backend:** C# / .NET 10 Web API
- **Frontend:** React 18, TypeScript, Vite
- **Database:** PostgreSQL (Local Docker Container)
- **ORM:** Entity Framework Core (via `Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Authentication & Organization Management:** Clerk (via `@clerk/clerk-react` and Clerk .NET SDK)
- **External APIs:** Twilio REST API (for outbound SMS), Stripe (for billing)

## 2. Repository Structure

The project is a monorepo containing both client and server applications.

```
/src
  /backend
    /Mercury.Api            # Main .NET 10 Web API project
    /Mercury.Core           # Entities, Interfaces, Enums
    /Mercury.Infrastructure # EF Core DbContext, Twilio Client, Stripe Client
  /frontend
    /src
      /components           # Reusable React components
      /pages                # Route-level components (Dashboard, Settings, Inbox)
      /services             # Axios/Fetch API wrappers
      /hooks                # Custom React hooks
```

## 3. Core Entities & Data Models

Generate Entity Framework Core models based on the following relational structure. All entities (except `Tenant`) must include a `TenantId` property.

- **Tenant:** Represents a subscribed business.
  - `Id` (UUID/Guid, PK)
  - `CompanyName` (string)
  - `TwilioPhoneNumber` (string)
  - `StripeCustomerId` (string)

- **User:** An employee scoped to a `Tenant`.
  - `Id` (UUID/Guid, PK)
  - `TenantId` (Guid, FK)
  - `ExternalAuthId` (string, the ID provided by Clerk)
  - `Email` (string)
  - `FullName` (string)
  - `Role` (Enum: Owner = 1, Admin = 2, User = 3)

- **PricingRule:** Deterministic pricing configurations for a `Tenant`.
  - `Id` (UUID/Guid, PK)
  - `TenantId` (Guid, FK)
  - `BaseRate` (decimal)
  - `PerBedroomRate` (decimal)
  - `PerBathroomRate` (decimal)

- **Lead:** An incoming prospect inquiry.
  - `Id` (UUID/Guid, PK)
  - `TenantId` (Guid, FK)
  - `CustomerName` (string)
  - `PhoneNumber` (string)
  - `CalculatedQuote` (decimal)
  - `Status` (Enum: New, Quoted, Booked, Lost)

## 4. Mandatory Security Rules for Code Generation

1. **Global Query Filters:** The `AppDbContext` must implement `.HasQueryFilter()` for all entities with a `TenantId`. This filter must securely read the active `TenantId` from a scoped context service (e.g., `ICurrentUserService`).
2. **No Cross-Tenant Queries:** Under no circumstances should the AI generate a LINQ query that bypasses the global query filter unless it is explicitly inside an administrative background job.
3. **Authentication Middleware:** Assume every incoming API request contains a valid Bearer token from Clerk. The token payload will include the user's `ExternalAuthId` and their active `OrganizationId` (which maps to our `TenantId`).
4. **RBAC Checks:** API endpoints modifying configuration (e.g., updating a `PricingRule`) must verify the user's `Role` is either `Owner` or `Admin`.

## 5. Core API Workflows to Generate

When prompted to generate features, adhere to these operational flows:

- **Webhook Ingestion (`POST /api/webhooks/lead`):**
  1. Parse incoming JSON payload.
  2. Identify the target `TenantId` based on an API key or routing parameter.
  3. Retrieve the `Tenant`'s active `PricingRule`.
  4. Calculate the total quote deterministically.
  5. Save the `Lead` to the database.
  6. Dispatch the outbound SMS via the Twilio Service.

- **Frontend Data Fetching:**
  1. Use Axios with an interceptor to attach the Clerk Bearer token to the `Authorization` header.
  2. React components should consume data via custom hooks (e.g., `useLeads()`, `usePricingRules()`).

## 6. Code Style Guidelines

- **C# / .NET:** Use minimal APIs where appropriate, or clean Controller classes. Utilize Dependency Injection for all services (`ITwilioService`, `IQuoteCalculator`). Ensure `async`/`await` is used for all I/O operations. Use PostgreSQL-specific EF Core features (like `UseNpgsql`) where applicable.
- **React:** Use functional components and hooks. Strictly type all props and API responses using TypeScript interfaces. Use Tailwind CSS for styling.
