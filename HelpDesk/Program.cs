using HelpDesk.Application.Agents;
using HelpDesk.Application.Security;
using HelpDesk.Application.Services;
using HelpDesk.Application.Tickets;
using HelpDesk.Application.Users;
using HelpDesk.Components;
using HelpDesk.Components.Account;
using HelpDesk.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUserAdministrationService, UserAdministrationService>();

// Agent registrations
builder.Services.AddScoped<IAgent, PlannerAgent>();
builder.Services.AddScoped<IPlannerAgent, PlannerAgent>();
builder.Services.AddScoped<IAgent, CoderAgent>();
builder.Services.AddScoped<ICoderAgent, CoderAgent>();
builder.Services.AddScoped<IAgent, DesignerAgent>();
builder.Services.AddScoped<IDesignerAgent, DesignerAgent>();
builder.Services.AddScoped<OrchestratorAgent>();

// HTTP Client and Services
builder.Services.AddHttpClient<IAgentsService, AgentsService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in ApplicationRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Orchestration endpoints
app.MapPost("/api/agents/orchestrate", async (
    AgentRequest request,
    OrchestratorAgent orchestrator,
    CancellationToken ct) =>
{
    var result = await orchestrator.RunAsync(request, ct);
    return Results.Ok(result);
});

app.MapPost("/api/agents/kickoff", async (
    AgentRequest request,
    OrchestratorAgent orchestrator,
    CancellationToken ct) =>
{
    var result = await orchestrator.RunAsync(request, ct);
    return Results.Ok(result);
});

app.Run();
