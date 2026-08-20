# HelpDesk

A Help Desk / Ticket Management system built with **.NET 9**, **Blazor**, and **Entity Framework Core**.

This project demonstrates complex workflow architecture, domain-driven design, audit trail patterns, role-based access control, and clean separation of concerns — reflecting real-world practices used in high-integrity, regulated systems.

---

## Features

- Full ticket lifecycle management (Create → Assign → In Progress → Resolved → Closed)
- Enforced status transitions with automatic audit history
- Role-based access (Requester, Agent, Admin)
- Public comments and internal notes
- Assignment and reassignment of tickets
- Search, filtering, and dashboard views
- Soft deletes and data integrity protections
- Optimistic concurrency support

---

## Tech Stack

- **.NET 9**
- **Blazor Web App** (Interactive Server)
- **ASP.NET Core Identity** (role-based authorization)
- **Entity Framework Core** + SQL Server
- **Clean / Layered Architecture**

---

## Architecture Overview

The solution follows a clean separation of concerns:

- **Domain** – Core entities and business rules (Ticket aggregate, status transitions, audit behavior)
- **Application** – Use cases / application services
- **Infrastructure** – EF Core, Identity, persistence
- **Presentation** – Blazor components and pages

Key design goals:
- Ticket as an aggregate root with encapsulated behavior
- Explicit status transition rules
- Full audit history for every status change
- High data integrity (soft deletes, concurrency tokens)

---

## Getting Started

### Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB, SQL Server Express, or full instance)
- Visual Studio 2022 (17.12+) or VS Code

### Setup

1. Clone the repository
   ```bash
   git clone https://github.com/Serk4/HelpDesk.git
   cd HelpDesk
   ```
2. Update the connection string in `appsettings.json` to point to your SQL Server instance.
3. Apply EF Core migrations to create the database:
   ```bash
   dotnet ef database update
   ```
4. Run the application:
   ```bash
   dotnet run --project HelpDesk.Web
   ```

### Project Structure

HelpDesk/
├── Domain/                 # Entities, enums, domain logic
├── Application/            # Interfaces and application services
├── Infrastructure/         # EF Core, Identity, repositories
├── HelpDesk/               # Blazor Web App (Presentation)
└── README.md

### Domain Highlights

- Ticket is treated as an aggregate root
- Status changes are controlled through domain methods (not open setters)
- Every status transition automatically records an audit entry
- Clear separation between public comments and internal notes

### Roadmap

- `[]` File attachments
- `[]` Basic SLA tracking / overdue highlighting
- `[]` Background job example (auto-escalation)
- `[]` Reporting (volume & resolution time)
- `[]` Improved dashboard visualizations

### License

This project is licensed under the MIT License.