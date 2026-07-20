using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using SportBook.Api.Endpoints;
using SportBook.Api.Middleware;
using SportBook.Application;
using SportBook.Application.Security;
using SportBook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Enums as their string names (e.g. "Tennis"), not numeric indices - matches the manual
// ToString() mapping already used for read DTOs and lets write DTOs (e.g. CreateCourtRequest)
// bind sportType from the same string values the frontend already sends/displays. Minimal API
// endpoints read Http.Json.JsonOptions, NOT Mvc.JsonOptions - this must be configured here even
// though there are no more MVC controllers (consilium 2026-07-20: the two are separate option
// objects and neither carries over to the other automatically).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSportBookInfrastructure(builder.Configuration);
builder.Services.AddSportBookApplication(builder.Configuration);

// Command/query dispatch for the Application layer's Features slices (consilium 2026-07-20:
// martinothamar/Mediator, not MediatR - MIT-licensed, source-generated, no revenue-gated
// commercial tier). The generator discovers IRequestHandler<> implementations across the
// compilation, including SportBook.Application's referenced assembly. ServiceLifetime.Scoped
// (not the library's Singleton default) - every handler takes a scoped SportBookDbContext, and
// registering singletons that capture a scoped dependency is a captive-dependency bug the DI
// container's own validation rejects.
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Global fallback policy: any endpoint without an explicit [AllowAnonymous] requires
// authentication by default (spec FR-014 - no unauthenticated access anywhere).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors(DevCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapAvailabilityEndpoints();
app.MapBookingsEndpoints();
app.MapCitiesEndpoints();
app.MapCourtsEndpoints();
app.MapReviewsEndpoints();
app.MapUsersEndpoints();
app.MapVenuesEndpoints();

app.Run();

/// <summary>Exposed for <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.</summary>
public partial class Program;
