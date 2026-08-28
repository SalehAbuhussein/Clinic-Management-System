using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

// ✅ NEW (Rate limiting)
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Clinic_Management_System.Authorization.People;
using Microsoft.AspNetCore.Authorization;
using Clinic_Management_System.Authorization.Appointment;
using Clinic_DataAccess;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured. run dotnet user-secrets set.");
var connectionString = builder.Configuration["ConnectionStrings:Key"]  ?? throw new InvalidOperationException("ConnectionStrings:Key is not configured. run dotnet user-secrets set.");

clsDataAccessSettings.ConnectionString = connectionString;

// ===============================
// 1) Authentication (JWT Bearer)
// ===============================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TokenValidationParameters define how incoming JWTs will be validated.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Ensures the token was issued by a trusted issuer.
            ValidateIssuer = true,

            // Ensures the token is intended for this API (audience check).
            ValidateAudience = true,

            // Ensures the token has not expired.
            ValidateLifetime = true,

            // Ensures the token signature is valid and was signed by the API.
            ValidateIssuerSigningKey = true,

            // The expected issuer value (must match the issuer used when creating the JWT).
            ValidIssuer = "ClinicApi",

            // The expected audience value (must match the audience used when creating the JWT).
            ValidAudience = "ClinicApiUsers",

            // The secret key used to validate the JWT signature.
            // This must be the same key used when generating the token.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClinicApiCorsPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7180",
                "http://localhost:5124"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ===============================
// 2) Authorization
// ===============================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PersonOwnerOrAuthorized",
        policy => policy.Requirements.Add(new PersonOwnerOrAuthorizedRequirement()));
    options.AddPolicy("CanDoctorViewAppointment",
        policy => policy.Requirements.Add(new CanDoctorViewAppointmentRequirement()));

});
builder.Services.AddSingleton<IAuthorizationHandler, PersonOwnerOrAuthorizedHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, CanDoctorViewAppointmentHandler>();

builder.Services.AddProblemDetails();
// ✅ NEW
// ===============================
// 2.5) Rate Limiting (Login + Refresh)
// ===============================
builder.Services.AddRateLimiter(options =>
{
    // Always return 429 when blocked
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy: max 5 requests per 1 minute per IP
    options.AddPolicy("AuthLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    var order = new Dictionary<string, int>
    {
        ["Auth"] = 1,
        ["Users"] = 2,
        ["Patients"] = 3,
        ["Doctors"] = 4,
        ["People"] = 5,
        ["Appointments"] = 6,
        ["Visits"] = 7,
        ["Diagnoses"] = 8,
        ["Prescriptions"] = 9
    };

    options.OrderActionsBy(api =>
    {
        var controller = api.ActionDescriptor.RouteValues["controller"];
        return $"{order.GetValueOrDefault(controller!, 999): 000}_{controller}";
    });
});

var app = builder.Build();

app.UseExceptionHandler();

// ===============================
// 4) Middleware Pipeline
// ===============================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("ClinicApiCorsPolicy");

//Return a safe 429 message (without revealing limits)
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
    {
        await context.Response.WriteAsync("Too many attempts. Please try again later.");
    }
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();