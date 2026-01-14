using FluentValidation;
using PastebinApp.Application.Interfaces;
using PastebinApp.Application.Services;
using PastebinApp.Infrastructure.Extensions;
using PastebinApp.Application.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPasteService, PasteService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePasteDtoValidator>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new 
    { 
        status = "healthy", 
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.MapGet("/", () => Results.Ok(new 
    { 
        message = "PastebinApp API",
        version = "1.0.0",
        endpoints = new
        {
            health = "/health",
            swagger = "/swagger",
            pastes = "/api/pastes"
        }
    }))
    .ExcludeFromDescription();

app.Run();