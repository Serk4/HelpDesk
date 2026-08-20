namespace HelpDesk.Application.Agents;

public enum AgentRole
{
    Orchestrator,
    Planner,
    Coder,
    Designer
}

public sealed record AgentRequest(
    string Objective,
    string? Context = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record AgentResponse(
    AgentRole Role,
    string Output,
    IReadOnlyList<string>? Actions = null,
    string? TaskId = null,
    IReadOnlyList<string>? Files = null);

public sealed record PlanStep(
    string Id,
    AgentRole Assignee,
    string Description,
    IReadOnlyList<string> Files,
    IReadOnlyList<string>? DependsOn = null);

public sealed record PlannerOutput(
    string Summary,
    IReadOnlyList<PlanStep> Steps,
    IReadOnlyList<string> EdgeCases,
    IReadOnlyList<string> OpenQuestions);

public sealed record ExecutionPhase(
    int Number,
    IReadOnlyList<PlanStep> Tasks,
    bool RunsInParallel,
    IReadOnlyList<int> DependsOnPhases);

public sealed record AuditEntry(DateTimeOffset TimestampUtc, string Message);

public sealed record OrchestrationResult(
    string Objective,
    PlannerOutput Planner,
    IReadOnlyList<ExecutionPhase> Phases,
    IReadOnlyList<AgentResponse> Outputs,
    IReadOnlyList<AuditEntry> AuditTrail,
    string Report);

public interface IAgent
{
    AgentRole Role { get; }
    Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

public interface IPlannerAgent : IAgent
{
    Task<PlannerOutput> CreatePlanAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

public sealed record CodeChange(
    string FilePath,
    string Description,
    IReadOnlyList<string> Lines);

public sealed record CoderOutput(
    string Summary,
    IReadOnlyList<CodeChange> Changes,
    IReadOnlyList<string> TestCasesGenerated,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Warnings,
    bool RequiresReview = false);

public interface ICoderAgent : IAgent
{
    Task<CoderOutput> GenerateCodeAsync(
        PlanStep planStep,
        string? previousContext = null,
        CancellationToken cancellationToken = default);
}

public sealed record UIComponent(
    string Name,
    string Markup,
    IReadOnlyList<string> Dependencies);

public sealed record DesignerOutput(
    string Summary,
    IReadOnlyList<UIComponent> Components,
    IReadOnlyList<string> Styles,
    IReadOnlyList<string> InteractionPatterns,
    IReadOnlyList<string> AccessibilityConsiderations,
    bool RequiresUserTesting = false);

public interface IDesignerAgent : IAgent
{
    Task<DesignerOutput> DesignUIAsync(
        PlanStep planStep,
        string? userRequirements = null,
        CancellationToken cancellationToken = default);
}
