# Clinic Management System

ASP.NET Core 8 Web API for a clinic (people, patients, doctors, appointments, visits,
medicines, prescriptions), with JWT auth. An Angular frontend is planned but not started.

## Where the code lives

Three sibling projects under this working directory, `Clinic Fullstack App\backend\`:

| Project | Path | Contains |
|---|---|---|
| API | `Clinic Management System\` | Controllers, DTOs, `Program.cs` |
| Business | `Clinic_Business\` | `Entities\`, `Repositories\`, `Interfaces\` |
| Data access | `Clinic_DataAccess\` | `clsUserDA`, `clsPersonDA`, … (nine `*DA.cs`) |

The repository root is one level up at `Clinic Fullstack App\`, where the Angular
`frontend\` will sit beside `backend\`. `Clinic Management System.slnx` lists all three
projects. The API references `Clinic_Business` by project reference; the business layer
calls the `*DA` classes directly. Build with
`dotnet build "Clinic Management System\Clinic Management System.csproj"`.

Moved here on 28 August 2026 from three flat folders under `source\repos\`. The API
must keep its own folder rather than sitting at `backend\` root: its `**/*.cs` glob
would otherwise compile the two libraries' generated `obj` files and fail with
duplicate-attribute errors (`CS0579`).

`Clinic_Business` is organised **folder-per-type**, and every namespace matches its folder:

| Folder | Namespace | Contains |
|---|---|---|
| `Entities\` | `Clinic_Business.Entities` | the nine `clsX` classes |
| `Repositories\` | `Clinic_Business.Repositories` | the nine `XRepository` implementations |
| `Interfaces\` | `Clinic_Business.Interfaces` | the nine `IXRepository` abstractions |

Nothing lives in the bare `Clinic_Business` namespace any more, so a file in the API project
usually needs **both** `using Clinic_Business.Entities;` and
`using Clinic_Business.Interfaces;`. `Program.cs` additionally needs
`using Clinic_Business.Repositories;` for its registrations.

Consider enabling `dotnet_style_namespace_match_folder` / **IDE0130** in an `.editorconfig`:
during the move three files kept their old namespace and only *one* produced a compile error,
because C# name lookup walks outward through enclosing namespaces — a type in
`Clinic_Business.Repositories` can still see `Clinic_Business` without a `using`, so a wrong
namespace is caught only when something actually becomes unreachable.

## Architecture

Classic three-tier, 3-layer naming (`clsX` / `clsXDA`). Data access is **raw ADO.NET**
— `SqlConnection` / `SqlCommand` / `SqlParameter`, no EF Core, and that is a deliberate
choice. Don't propose EF Core.

Business classes are **active record** (Fowler's term, and a deliberate pattern rather than
debt): the object wraps a row, holds its own persistence, and carries its own validation.
Persistence ownership was reviewed on 8 September and the object keeps it — see the roadmap.

Every entity is reached through a **repository** injected into the controller. The nine
repositories delegate to the entity's own statics rather than calling `*DA` directly, so the
injected and any remaining static path cannot drift. Reads return entities or projection
records; writes still happen on the object (`appointment.SaveAsync()`), which is the recorded
active-record decision.

Two shapes exist side by side, and the difference matters:

- **`clsUser` is the migrated one.** It has explicit `Create()` and `Update()`, each carrying
  the validation rule that applies to it. `enMode`, `_Mode`, `Save()` and `IsValid()` are
  gone. A caller states which operation it wants; nothing is inferred from hidden state.
- **The other six still use the flag.** `clsAppointment`, `clsDiagnosis`, `clsDoctor`,
  `clsMedicine`, `clsPatient` and `clsVisit` each have a private `enMode _Mode` that
  `SaveAsync()` switches on — `Add` inserts, `Update` updates, and `IsValid()` silently means
  different things depending on it. Converting them is optional and should follow need, not
  momentum.

Where a list query cannot produce a full entity — no join, or a join across three tables —
the entity declares a nested **projection record** instead of returning a `DataTable`:
`clsUser.UserItem`, `clsPatient.PatientItem`, `clsDoctor.DoctorItem`. Where the query does
cover every column, the list returns `List<clsX>` directly.

Two things worth remembering:

- `clsUser._CreateUser()` writes **both** the `People` and `Users` rows in a single
  `CreateUser` stored-procedure call, so there is no two-step orphan-row window in C#.
  Whether the procedure itself is transactional cannot be verified from this repo — see
  the note about unscripted procedures below.
- **The stack is `async` end to end**, `*DA` → `clsX` → `IXRepository` → controller, reads
  and writes, with no exceptions: `Clinic_DataAccess` contains **zero** synchronous
  `connection.Open()`, `ExecuteReader()`, `ExecuteNonQuery()` or `ExecuteScalar()` calls. The
  `IsValid()` methods that remain synchronous are pure in-memory validation and touch no I/O.
- **No `ref` parameter survives anywhere in the data-access layer** (every remaining match for
  `ref ` is in a comment). Getting there was the point of the conversion: `ref` cannot cross an
  `await` (`CS1988`), because an async method's stack does not survive suspension, so every
  `Find` had to be reshaped before it could be made async. The shape used throughout is to
  return a `DataRow?` (or a `record`) and let a `MapX` method build the object.
  `clsPersonDA.Find` was the worst case at **ten** `ref` parameters.
- `MapPerson` is shared by `clsUser` and `clsDoctor`, which load a person along with their own
  row, so a new `People` column is added in one place.
- Naming slips still present: `clsUserDA.Find` returns a `Task` but lacks the `Async` suffix
  its siblings have, and `clsUser.Create()` / `Update()` are async without the suffix either.

**The data-access layer no longer swallows exceptions.** All 52 `try/catch` +
`Console.WriteLine` blocks were removed, and `Clinic_DataAccess` now contains zero `catch`
statements. A `SqlException` propagates to `UseExceptionHandler` and becomes a 500
`ProblemDetails` with the exception in the log. So a `false`/`null` return now means
"validation failed" or "zero rows", not "something threw" — which is what makes the
distinction between 422 and 500 in the controllers meaningful.

## Dependency injection

`Program.cs` is the composition root. Registered there, all **`AddScoped`** (lines 92-104):

| Registration | Count | Why scoped |
|---|---|---|
| the three `IAuthorizationHandler`s | 3 | each injects a scoped repository |
| `IXRepository` → `XRepository` | 9 | per-request is the lifetime a DB-touching service wants |

All three authorization handlers **had** to move from `AddSingleton` to `AddScoped` as they
gained injected repositories, and that is the part worth understanding. A singleton may not consume a scoped service — the
container refuses at startup with *"Cannot consume scoped service … from singleton"*, because
the singleton would capture the first request's instance forever. Changing that one line from
`AddSingleton` to `AddScoped` is what made injecting the repository possible. Note the
container does **not** catch the reverse mistake: a `Transient` injected into a singleton is
silently constructed once and lives as long as the singleton.

There are **nine** interfaces, one per entity, and every member is async. Every controller
takes its repositories by constructor injection; `PeopleController` was the last one that had
no constructor at all.

**No static entity call survives in any controller.** The last one was
`clsMedicine.IsValidMedicines`, now `IMedicineRepository.AreValidMedicinesAsync` and injected
into `PrescriptionsController` alongside its two other repositories. It still takes a
`List<string>` of ids stringified from `int`s and passes them to `STRING_SPLIT` in SQL —
the signature is worth revisiting even though the wiring is now right.

Entity persistence is still called on the object (`user.Create()`, `appointment.SaveAsync()`).
That is the recorded decision, not an oversight.

## Database facts not visible in the code

- `dbo.Users.Role` **defaults to `'Staff'`** — `clsUserDA.Create` deliberately omits the
  column. An admin promotes a user afterwards through `PUT /api/Users/{id}`, whose
  `UpdateUserDTO.Role` is constrained by
  `[AllowedValues(null, "Admin", "Doctor", "Staff")]`. The `null` in that list is
  load-bearing: `[AllowedValues]` rejects `null` unless `null` is listed, and `Role` is
  nullable because the update is partial.
- Three roles are in use: `'Admin'`, `'Staff'` and `'Doctor'`. Note the attribute strings
  are written five different ways across the controllers (`"Admin,Staff"`,
  `"Admin, Staff"`, `"Admin,Staff,Doctor"`, `"Admin,Doctor,Staff"`, `"Admin,Doctor"`) —
  inconsistent, and worth normalising.
- **Eight stored procedures and one trigger exist only inside the running database.** There
  is no `.sql` file anywhere in the three projects, so they cannot be read, reviewed, or
  restored from this repo. Scripting them to a `Database/` folder is an open roadmap item.
  A trigger or a procedure also invalidates a row count, which is why every procedure call
  now reads its own `RETURN` value rather than `rowsAffected`.

## Configuration

Two secrets, both from configuration, and the app **refuses to start** without either:

- `Jwt:Key` — bound onto `JwtOptions.Key`, validated `[Required]` + `[MinLength(32)]`.
- `ConnectionStrings:Clinic` — read in `Program.cs` and assigned to the static
  `clsDataAccessSettings.ConnectionString`, which is how the data-access layer reaches it.

Both live in user-secrets, which load **only in Development**. Elsewhere use environment
variables, where `:` becomes `__` (`ConnectionStrings__Clinic`). User-secrets is not an
alternative to configuration — it is one of its *providers*, and it wins on precedence in
Development, which is why options binding sees it.

**JWT settings use the options pattern.** `Options/JwtOptions.cs` holds `Key`, `Issuer`,
`Audience`, `AccessTokenMinutes` and `RefreshTokenDays`, bound and validated in `Program.cs`:

```csharp
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`ValidateOnStart()` is the point: a missing, short or out-of-range value produces
`Hosting failed to start` naming the property and the rule, instead of a 500 on someone's
first login. `AuthController` injects `IOptions<JwtOptions>` and holds `.Value`; it no longer
takes `IConfiguration` at all. `Program.cs` binds the section a second time, directly, because
the JWT bearer handler is configured while the container is still being built — that is the
startup sequence, not a duplication to remove.

**Two traps already hit here, both worth remembering:**

- `const` and `static` members are invisible to configuration binding *and* to
  `ValidateDataAnnotations` — binding needs somewhere to write, and validation only inspects
  instance properties. Attributes on a `const` look correct and do nothing. Naming the section
  constant `Key` once collapsed it with the signing key and produced a 3-byte HMAC key.
- `JwtOptions.SectionName` is `"jwt"` while the secret is `Jwt:Key`. That binds only because
  configuration keys are case-insensitive.

## Current focus

The project doubles as a learning exercise; see the roadmap and code review at
https://claude.ai/code/artifact/b9312fba-3f16-4f6e-a14b-1de5d8531f72 — it is the source of
truth for what is fixed and what is still open. Update it there rather than restating
findings elsewhere.
