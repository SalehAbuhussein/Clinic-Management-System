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
using Clinic_Management_System.Authorization.Prescription;
using Clinic_Business;
using Clinic_Business.Interfaces;
using Clinic_Management_System.Options;

var builder = WebApplication.CreateBuilder(args);
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("The Jwt configuration section is missing.");
var connectionString = builder.Configuration.GetConnectionString("Clinic") ?? throw new InvalidOperationException("ConnectionStrings:Clinic is not configured. run dotnet user-secrets set.");

clsDataAccessSettings.ConnectionString = connectionString;

// ===============================
// 0) Options 
// ===============================

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

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
            ValidIssuer = jwtOptions.Issuer,

            // The expected audience value (must match the audience used when creating the JWT).
            ValidAudience = jwtOptions.Audience,

            // The secret key used to validate the JWT signature.
            // This must be the same key used when generating the token.
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
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
    options.AddPolicy("ManagePrescription",
        policy => policy.Requirements.Add(new ManagePrescriptionRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, PersonOwnerOrAuthorizedHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CanDoctorViewAppointmentHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ManagePrescriptionHandler>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
builder.Services.AddScoped<IPrescriptionItemRepository, PrescriptionItemRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();

builder.Services.AddProblemDetails();
// ✅ NEW
// ===============================
// 2.5) Rate Limiting (auth endpoints)
// ===============================
builder.Services.AddRateLimiter(options =>
{
    // Always return 429 when blocked
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // One bucket per caller per action, named "<ip>:<action>", e.g. "127.0.0.1:login".
    // Separate buckets matter: without them, five bad passwords also block the
    // refresh and logout calls, which signs the user out for getting a password wrong.
    options.AddPolicy("AuthLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // last path segment names the action: /api/Auth/login -> "login"
        var path = httpContext.Request.Path.Value ?? "";
        var action = path[(path.LastIndexOf('/') + 1)..].ToLowerInvariant();
        if (action.Length == 0)
            action = "auth";

        var permitLimit = action switch
        {
            "login" => 5,       // the brute-force target, keep it tight
            "register" => 10,   // Admin only, but cheap to guard
            _ => 30             // refresh and logout are routine, not attacks
        };

        // The limit must vary only by something already in the key: the factory
        // runs once per new key and its options are cached against that key.
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{ip}:{action}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
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