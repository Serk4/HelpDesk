# Copilot Instructions

## Project Guidelines
- For ticket workflow, store updates in a separate Ticket Notes history table with timestamped entries, record claim assignee using the logged-in ASP.NET Identity user Id, and when abandoning a claimed ticket set status back to New and clear the assignee.
- Use PRD.md in the repository root as the authoritative source of requirements. Keep the Agent Pipeline feature as a non-functional placeholder demo and do not expand it beyond its current structure; use OrchestratorAgent only as a reasoning shell, not a real execution pipeline.