using Microsoft.EntityFrameworkCore;
using SprintFlow.Web.Server.Data;
using SprintFlow.Web.Server.Extensions;
using SprintFlow.Web.Server.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<SprintFlowDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddAuthenticationServices(builder.Configuration);

var app = builder.Build();

await app.ValidateAuthenticationConfigurationAsync(); // Fail fast rather than run misconfigured.

app.UseMiddleware<ExceptionHandlingMiddleware>(); // First in the pipeline, so it catches everything after it.

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Builds HttpContext.User from the incoming token.

app.UseAuthorization(); // Checks whether that user may access the endpoint. Order matters.

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
