# Product Requirements Document (PRD)

**Project:** HelpDesk  
**Type:** Blazor Web App (.NET 9) + SQL Server  
**Owner:** Serk4
**Goal:** Portfolio project demonstrating complex workflow architecture, domain modeling, audit trails, role-based access, and modern .NET practices.

---

## 1. Overview

HelpDesk is a professional Help Desk / Ticket Management system that allows users to create, assign, track, and resolve support tickets. The application showcases:

- Clean domain-driven design  
- Enforced status workflow transitions  
- Full audit history  
- Role-based access  
- Solid relational data modeling  

The architecture reflects patterns used in regulated forensic and enterprise environments.

---

## 2. Target Users & Roles

### Role Definitions

| Role          | Description                           | Key Permissions |
|---------------|---------------------------------------|-----------------|
| **Requester** | End user who submits tickets          | Create tickets, view own tickets, add comments |
| **Agent**     | Support staff                         | View/assign tickets, change status, add internal notes |
| **Admin**     | System administrator                  | Full access, user/role management, reporting |

---

## 3. Core Features (MVP)

### Ticket Management
- Create, view, edit, and soft-delete tickets  
- Fields: Title, Description, Category, Priority, Status, Requester, Assignee, Created/Updated timestamps  
- Enforced status workflow (e.g., **New → Assigned → In Progress → Resolved → Closed**)  
- Priority levels: **Low, Medium, High, Critical**

### Comments & History
- Public comments (visible to requester)  
- Internal notes (agents/admins only)  
- Automatic status change history (who, when, old → new)

### Assignment & Ownership
- Assign / reassign tickets to agents  
- “My Tickets” views for both requesters and agents  

### Search, Filter & Dashboard
- Filter by Status, Priority, Category, Assignee, Date range  
- Dashboard:  
  - Open tickets by status/priority  
  - My Assigned  
  - Recently updated  

### Authentication & Authorization
- ASP.NET Core Identity  
- Role-based access  
- Secure pages and actions based on role  

---

## 4. Future / Stretch Features (Post-MVP)

- [ ] File attachments (metadata stored in DB)  
- [ ] Basic SLA tracking / overdue highlighting  
- [ ] Email notification stubs or background service example  
- [ ] Reporting (ticket volume, average resolution time)  
- [ ] Ticket linking (parent/child relationships)

---

## 5. Technical Requirements

### Stack
- .NET 9  
- Blazor Web App (Interactive Server)  
- ASP.NET Core Identity  
- Entity Framework Core + SQL Server  
- Bootstrap or lightweight custom styling  

### Architecture Goals
- Clean separation: **Domain → Application → Infrastructure → Presentation**  
- Ticket as an **aggregate root** with behavior (not anemic)  
- Explicit status transition rules  
- Full audit trail for status changes  
- Soft deletes where appropriate  
- Optimistic concurrency on Ticket entity  

### Database
- Normalized relational design  
- History table for status changes  
- Proper indexing for common filters  
- EF Core Code-First with migrations  

---

## 6. Non-Functional Requirements
- Clean, readable, maintainable code suitable for a senior portfolio  
- Clear README with architecture notes, screenshots, and setup instructions  
- Demonstrates workflow patterns and data integrity from regulated environments  
- Easy to run locally (SQL Server LocalDB or Docker optional)

---

## 7. Success Criteria
- Full end-to-end ticket lifecycle (**create → assign → comment → resolve → close**)  
- Visible audit history on every ticket  
- Role-based UI and authorization  
- Domain model enforces business rules  
- Professional GitHub presentation ready for recruiters

## 8. Agent Collaboration (Orchestrator Pattern)

- Add an **Orchestrator Agent** that receives a user objective and coordinates specialist agents.
- Specialist agents:
  - **Planner Agent**: decomposes objectives into executable tasks.
  - **Coder Agent**: proposes backend/domain/data implementation steps.
  - **Designer Agent**: proposes UI/UX and Blazor component changes.
- Communication model:
  - Shared context object (goal, constraints, artifacts, history).
  - Orchestrator dispatches tasks to specialists and aggregates outputs.
- Non-functional:
  - Deterministic routing rules first (no hidden magic).
  - Full audit trail of agent decisions and outputs.
  - Pluggable LLM provider (Azure OpenAI/OpenAI/local) behind interfaces.