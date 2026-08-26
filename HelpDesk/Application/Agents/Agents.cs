using System.Text;

namespace HelpDesk.Application.Agents;

public sealed class PlannerAgent : IPlannerAgent
{
    public AgentRole Role => AgentRole.Planner;

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await CreatePlanAsync(request, cancellationToken);

        return new AgentResponse(
            Role,
            plan.Summary,
            plan.Steps.Select(step => $"{step.Id}: {step.Description}").ToArray());
    }

    public Task<PlannerOutput> CreatePlanAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var featureKey = ToFeatureKey(request.Objective);

        var steps = new List<PlanStep>
        {
            new(
                "step-1",
                AgentRole.Designer,
                "Create UX plan and interaction rules for the requested objective.",
                [
                    $"HelpDesk/Components/Pages/{featureKey}.razor",
                    "HelpDesk/Components/Layout/NavMenu.razor"
                ]),
            new(
                "step-2",
                AgentRole.Coder,
                "Implement application and infrastructure changes for the objective.",
                [
                    $"HelpDesk/Application/{featureKey}/",
                    "HelpDesk/Data/"
                ]),
            new(
                "step-3",
                AgentRole.Coder,
                "Integrate the new workflow into Blazor endpoints and UI routes.",
                [
                    "HelpDesk/Program.cs",
                    $"HelpDesk/Components/Pages/{featureKey}.razor"
                ],
                ["step-1", "step-2"])
        };

        var plan = new PlannerOutput(
            "Create a sequenced plan, separate file ownership, and execute by phase with parallel work where no file overlap exists.",
            steps,
            [
                "A task cannot start until dependencies are complete.",
                "Parallel tasks must not overlap file scopes.",
                "Ambiguous requirements should be captured as open questions before coding starts."
            ],
            [
                "Should design-only changes be approved before code tasks begin?"
            ]);

        return Task.FromResult(plan);
    }

    private static string ToFeatureKey(string objective)
    {
        var cleaned = new string(objective
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : ' ')
            .ToArray());

        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "FeatureWorkbench";
        }

        var selected = words.Take(3)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]);

        return string.Concat(selected);
    }
}

public sealed class CoderAgent : ICoderAgent
{
    public AgentRole Role => AgentRole.Coder;

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var coderOutput = await GenerateCodeAsync(
            new PlanStep(
                Id: request.Metadata?.GetValueOrDefault("TaskId") ?? Guid.NewGuid().ToString(),
                Assignee: AgentRole.Coder,
                Description: request.Objective,
                Files: ParseFiles(request.Metadata?.GetValueOrDefault("Files")),
                DependsOn: null),
            request.Context,
            cancellationToken);

        return new AgentResponse(
            Role,
            coderOutput.Summary,
            coderOutput.Changes.Select(c => c.Description).ToList(),
            request.Metadata?.GetValueOrDefault("TaskId"),
            coderOutput.Changes.Select(c => c.FilePath).ToList());
    }

    public async Task<CoderOutput> GenerateCodeAsync(
        PlanStep planStep,
        string? previousContext = null,
        CancellationToken cancellationToken = default)
    {
        // Stub implementation for now
        // TODO: Integrate with LLM for actual code generation

        var changes = new List<CodeChange>
        {
            new(
                "Implementation_Stub",
                "Code generation logic to be implemented",
                new[] { "// TODO: Implement" })
        };

        return new CoderOutput(
            $"Implementation task accepted: {planStep.Description}",
            changes,
            [],
            [],
            ["Follow existing project architecture and conventions.",
             "Keep behavior deterministic and testable.",
             "Validate with build/tests after changes."],
            RequiresReview: true);
    }

    private static IReadOnlyList<string> ParseFiles(string? files)
    {
        if (string.IsNullOrWhiteSpace(files))
        {
            return [];
        }

        return files.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

public sealed class DesignerAgent : IDesignerAgent
{
    public AgentRole Role => AgentRole.Designer;

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var designerOutput = await DesignUIAsync(
            new PlanStep(
                Id: request.Metadata?.GetValueOrDefault("TaskId") ?? Guid.NewGuid().ToString(),
                Assignee: AgentRole.Designer,
                Description: request.Objective,
                Files: ParseFiles(request.Metadata?.GetValueOrDefault("Files")),
                DependsOn: null),
            request.Context,
            cancellationToken);

        return new AgentResponse(
            Role,
            designerOutput.Summary,
            designerOutput.Components.Select(c => $"Create component: {c.Name}").ToList(),
            request.Metadata?.GetValueOrDefault("TaskId"),
            designerOutput.Components.Select(c => $"{c.Name}.razor").ToList());
    }

    public async Task<DesignerOutput> DesignUIAsync(
        PlanStep planStep,
        string? userRequirements = null,
        CancellationToken cancellationToken = default)
    {
        // Stub implementation for now
        // TODO: Integrate with LLM for UI/UX design and Blazor component generation

        var components = new List<UIComponent>
        {
            new(
                "UIStub",
                "<!-- TODO: Implement UI design -->",
                new[] { "BlazorComponent" })
        };

        return new DesignerOutput(
            $"Design task accepted: {planStep.Description}",
            components,
            [],
            ["Prioritize usability and accessibility.",
             "Align interaction patterns with Blazor component structure.",
             "Deliver concrete UI acceptance criteria."],
            ["WCAG 2.1 Level AA accessibility"],
            RequiresUserTesting: true);
    }

    private static IReadOnlyList<string> ParseFiles(string? files)
    {
        if (string.IsNullOrWhiteSpace(files))
        {
            return [];
        }

        return files.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

public sealed class OrchestratorAgent
{
    private readonly IPlannerAgent _planner;
    private readonly ICoderAgent _coder;
    private readonly IDesignerAgent _designer;
    private readonly IReadOnlyDictionary<AgentRole, IAgent> _specialists;

    public OrchestratorAgent(
        IPlannerAgent planner,
        ICoderAgent coder,
        IDesignerAgent designer)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _coder = coder ?? throw new ArgumentNullException(nameof(coder));
        _designer = designer ?? throw new ArgumentNullException(nameof(designer));
        _specialists = new Dictionary<AgentRole, IAgent>
        {
            { AgentRole.Coder, coder },
            { AgentRole.Designer, designer }
        };
    }

    public async Task<OrchestrationResult> RunAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var audit = new List<AuditEntry>();
        audit.Add(NewAudit("Step 1: requesting plan from Planner."));

        var plannerOutput = await _planner.CreatePlanAsync(request, cancellationToken);
        audit.Add(NewAudit($"Planner returned {plannerOutput.Steps.Count} step(s)."));

        var phases = BuildPhases(plannerOutput.Steps);
        audit.Add(NewAudit($"Step 2: parsed plan into {phases.Count} phase(s)."));

        var responses = new List<AgentResponse>();
        var contextChain = request.Context ?? string.Empty;

        foreach (var phase in phases)
        {
            audit.Add(NewAudit($"Step 3: executing phase {phase.Number} with {phase.Tasks.Count} task(s)."));

            var tasks = phase.Tasks.Select(planStep => ExecutePlanStepAsync(request, planStep, contextChain, cancellationToken)).ToArray();
            var phaseOutputs = await Task.WhenAll(tasks);
            responses.AddRange(phaseOutputs);

            // Chain context: each phase's output becomes input context for the next phase
            if (phaseOutputs.Any())
            {
                contextChain = string.Join("\n---\n", phaseOutputs.Select(r => r.Output));
            }

            audit.Add(NewAudit($"Completed phase {phase.Number}."));
        }

        audit.Add(NewAudit("Step 4: orchestration complete. Results aggregated."));

        var report = BuildReport(request.Objective, plannerOutput, phases, responses, audit);

        return new OrchestrationResult(
            request.Objective,
            plannerOutput,
            phases,
            responses,
            audit,
            report);
    }

    private async Task<AgentResponse> ExecutePlanStepAsync(
        AgentRequest originalRequest,
        PlanStep step,
        string contextChain,
        CancellationToken cancellationToken)
    {
        if (!_specialists.TryGetValue(step.Assignee, out var specialist))
        {
            throw new InvalidOperationException($"No specialist agent registered for role '{step.Assignee}'.");
        }

        var metadata = new Dictionary<string, string>(originalRequest.Metadata ?? new Dictionary<string, string>())
        {
            ["TaskId"] = step.Id,
            ["Files"] = string.Join('|', step.Files)
        };

        var taskRequest = new AgentRequest(step.Description, contextChain, metadata);
        return await specialist.ExecuteAsync(taskRequest, cancellationToken);
    }

    private static List<ExecutionPhase> BuildPhases(IReadOnlyList<PlanStep> orderedSteps)
    {
        var pending = orderedSteps.ToList();
        var completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var phaseResults = new List<ExecutionPhase>();
        var phaseNumber = 1;

        while (pending.Count > 0)
        {
            var selected = new List<PlanStep>();
            var selectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var step in pending)
            {
                if (!DependenciesMet(step, completed))
                {
                    continue;
                }

                if (HasFileConflict(step.Files, selectedFiles))
                {
                    continue;
                }

                selected.Add(step);
                foreach (var file in step.Files)
                {
                    selectedFiles.Add(file);
                }
            }

            if (selected.Count == 0)
            {
                selected.Add(pending[0]);
            }

            var selectedIds = selected.Select(step => step.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            pending.RemoveAll(step => selectedIds.Contains(step.Id));

            foreach (var step in selected)
            {
                completed.Add(step.Id);
            }

            phaseResults.Add(new ExecutionPhase(
                phaseNumber,
                selected,
                selected.Count > 1,
                phaseNumber == 1 ? [] : [phaseNumber - 1]));

            phaseNumber++;
        }

        return phaseResults;
    }

    private static bool DependenciesMet(PlanStep step, HashSet<string> completed)
    {
        if (step.DependsOn is null || step.DependsOn.Count == 0)
        {
            return true;
        }

        return step.DependsOn.All(completed.Contains);
    }

    private static bool HasFileConflict(IEnumerable<string> candidateFiles, HashSet<string> selectedFiles)
    {
        foreach (var file in candidateFiles)
        {
            if (selectedFiles.Contains(file))
            {
                return true;
            }
        }

        return false;
    }

    private static AuditEntry NewAudit(string message)
        => new(DateTimeOffset.UtcNow, message);

    private static string BuildReport(
        string objective,
        PlannerOutput planner,
        IReadOnlyList<ExecutionPhase> phases,
        IReadOnlyList<AgentResponse> responses,
        IReadOnlyList<AuditEntry> audit)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Orchestration Result");
        sb.AppendLine($"Objective: {objective}");
        sb.AppendLine();

        sb.AppendLine("### Planner Summary");
        sb.AppendLine(planner.Summary);
        sb.AppendLine();

        sb.AppendLine("### Execution Plan");
        foreach (var phase in phases)
        {
            sb.AppendLine($"Phase {phase.Number} {(phase.RunsInParallel ? "(parallel)" : "(sequential)")}");
            foreach (var task in phase.Tasks)
            {
                sb.AppendLine($"- {task.Id}: {task.Description} -> {task.Assignee}");
                sb.AppendLine($"  Files: {string.Join(", ", task.Files)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("### Specialist Outputs");
        foreach (var response in responses)
        {
            sb.AppendLine($"- {response.Role} [{response.TaskId ?? "n/a"}]: {response.Output}");
        }

        sb.AppendLine();
        sb.AppendLine("### Audit Trail");
        foreach (var entry in audit)
        {
            sb.AppendLine($"- {entry.TimestampUtc:O} {entry.Message}");
        }

        return sb.ToString();
    }
}
