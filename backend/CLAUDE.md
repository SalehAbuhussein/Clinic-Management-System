# Clinic Management System

ASP.NET Core 8 Web API for a clinic (people, patients, doctors, appointments, visits,
medicines, prescriptions, leaves, auditing), with JWT auth. The Angular 22 frontend lives
beside it in `frontend/` and is built out across twelve features.

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
records; writes still happen on the object (`appointment.UpdateAsync()`), which is the
recorded active-record decision.

**The `enMode` migration is finished.** As of 25 October 2026 every entity has explicit
`CreateAsync()` and `UpdateAsync()`, each carrying only the validation rule that applies to
it. `enMode`, `_Mode`, `SaveAsync()` and the mode-dependent `IsValid()` have **zero**
references left in the solution. A caller states which operation it wants; nothing is
inferred from hidden state.

`clsUser` went first, and `clsAppointment`, `clsDiagnosis`, `clsDoctor`, `clsLeave`,
`clsMedicine`, `clsPatient` and `clsVisit` followed. Three of them had validation that
branched on the flag, which is what made it worth doing: `clsVisit`, `clsDoctor` and
`clsAppointment` each had one `IsValid()` that meant two different things. Those rules now
sit in the method they belong to, so `clsAppointment.CreateAsync` refuses a start in the past
while `UpdateAsync` allows it, visibly rather than by implication.

Two things that came out of it and are worth knowing:

- **`AppointmentsController` lost a guard.** It called `IsValid()` on a freshly loaded
  appointment and then overwrote `StartAt` and `EndAt` seven lines later, so it validated
  values that were about to be replaced. It could only fire on an already-corrupt row, and
  then blocked the edit that would have fixed it.
- **`clsLeave` never validated anything.** Its `SaveAsync()` had no `IsValid()` call, so
  `CreateAsync` and `UpdateAsync` have no guard either. That is faithful to what was there,
  not an oversight introduced by the migration.

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

`Program.cs` is the composition root. Registered there, all **`AddScoped`** (lines 103-126):

| Registration | Count | Why scoped |
|---|---|---|
| the seven `IAuthorizationHandler`s | 7 | each injects a scoped repository |
| `IXRepository` → `XRepository` | 13 | per-request is the lifetime a DB-touching service wants |

The authorization handlers **had** to move from `AddSingleton` to `AddScoped` as they
gained injected repositories, and that is the part worth understanding. A singleton may not consume a scoped service — the
container refuses at startup with *"Cannot consume scoped service … from singleton"*, because
the singleton would capture the first request's instance forever. Changing that one line from
`AddSingleton` to `AddScoped` is what made injecting the repository possible. Note the
container does **not** catch the reverse mistake: a `Transient` injected into a singleton is
silently constructed once and lives as long as the singleton.

There are **thirteen** interfaces, one per entity, and every member is async. Every controller
takes its repositories by constructor injection; `PeopleController` was the last one that had
no constructor at all.

**No static entity call survives in any controller.** The last one was
`clsMedicine.IsValidMedicines`, now `IMedicineRepository.AreValidMedicinesAsync` and injected
into `PrescriptionsController` alongside its two other repositories. It still takes a
`List<string>` of ids stringified from `int`s and passes them to `STRING_SPLIT` in SQL —
the signature is worth revisiting even though the wiring is now right.

Entity persistence is still called on the object (`user.Create()`, `appointment.UpdateAsync()`).
That is the recorded decision, not an oversight.

## Logging

`ILogger<T>` needs **no registration** — the host adds it to the container before user code
runs. Coverage is deliberately scoped rather than everywhere, on three rules:

1. **Never duplicate the framework.** `Microsoft.AspNetCore.Diagnostics` already logs
   `"An unhandled exception has occurred…"` at **Error** with the full exception, so a
   `SqlException` reaching `UseExceptionHandler` is logged without us writing anything. Adding
   our own `LogError` there would report the same failure twice.
2. **Log in the API layer only.** `clsX` and `clsXDA` are static-entry, so there is no
   constructor to inject into, and the data layer throws rather than swallowing — failures
   surface as exceptions the framework logs once at the top. Instrumenting those layers would
   double-report and would require reshaping the classes first.
3. **Log what the response deliberately hides, plus audit-worthy state changes.** Not ordinary
   404s or validation failures: the caller already knows and it is noise.

Applied, that is:

| Where | Event | Level |
|---|---|---|
| `AuthController.Login` | unknown email; wrong password; success | Warning / Warning / Information |
| `AuthController.Refresh` | revoked token used; hash mismatch | Warning |
| `UsersController` | role/active change, deactivate, activate — with the acting admin's id | Information |
| `Program.cs` `OnRejected` | rate-limit rejection | Warning |
| the seven authorization handlers | denial, with acting user and resource id | Warning |

**Every one of those also writes an `AuditEntries` row**, added 18 October, plus rows for
`Register` and `ChangePassword` which had no logging at all before. The log is for us and is
lost on restart; the table is queryable, admin-readable at `GET /api/Audit`, and is what the
"a user owns audit history" rule above actually refers to. `AuditAction` holds the ten action
names as constants so the screen and the writers cannot drift.

Levels are set per category in `appsettings.json`, matched by **prefix, most specific wins**:
`Default` is `Warning`, while the `AuthController` category and `Clinic.RateLimiting` stay at
`Information`. `appsettings.Development.json` overrides `Default` back to `Information` locally.

**Authorization failures are logged by our own handlers, not by the framework.** Enabling the
`Microsoft.AspNetCore.Authorization` category surfaces them (the broad `Microsoft.AspNetCore:
Warning` entry hides them, since the framework logs them at Information), but that was tried
and **reverted by decision** — it means taking the framework's verbosity and wording. Note the
three handlers deny by *never calling* `context.Succeed`; none calls `Fail()`. So there is no
failure branch, and the log sits behind `if (!context.HasSucceeded)` at the end of the method.
That is only correct because each policy holds a single requirement — with several,
`HasSucceeded` would reflect all of them. It needs no `appsettings.json` entry, because
`LogWarning` already passes under `Default: Warning`.

Two things to keep in mind:

- **Always message templates, never interpolation.** `LogWarning("… {Email}", email)` records a
  named property; `$"… {email}"` flattens it into an unqueryable string.
- **The login failure paths return an identical 401 on purpose** (anti-enumeration) **but log
  different messages.** Responses are for the caller, logs are for us; they need not agree.
  Deliberately *not* logged: an unknown email on `Refresh`, which is indistinguishable from a
  stale client, and which `Login` already covers for enumeration purposes.

Still open: no log scopes, so nothing carries a correlation id across a request — the
`traceId` now on every `ProblemDetails` is the value a scope would carry.

## Resource shape

Three decisions here are deliberate and easy to mistake for gaps:

- **A child that cannot exist without its parent is nested and singular.** A visit has at
  most one prescription and at most one diagnosis, so both are singular sub-resources:
  `api/Visits/{visitId}/prescription` and `api/Visits/{visitId}/diagnosis`. Neither carries
  an id segment, and neither create DTO carries `VisitID` — the parent is in the URL.
  `POST` returns 409 if one already exists.
- **`Users` has both a soft and a hard delete, and the soft one is the default.**
  `PATCH {id}/deactivate` and `{id}/activate` are what the UI uses, because a user owns audit
  history and deactivating keeps it. `DELETE /api/Users/{id}` was added on 25 October 2026 for
  the accounts that have none. Three rules guard it. The caller must be an admin
  (`[Authorize(Roles = "Admin")]`) and may not delete their own account, both checked in
  `UsersController.DeleteUser`. Whether the target may be deleted at all is the
  `DeleteAccount` policy, which succeeds only for a doctor or a staff member, so one admin
  cannot delete another. Like the other seven handlers it logs the denial and writes an
  `AccessDenied` audit row.
  A delete that any `AuditEntries`, `Doctors` or `Leaves` row still references fails on a
  foreign key, which the endpoint catches as `SqlException` 547 and answers 409 telling the
  caller to deactivate instead. In practice that is most accounts: anyone who has logged in
  owns a `LoginSucceeded` row.
- **`People` has no `POST`.** A person is only ever created as part of a role —
  `POST /api/Auth/register`, `POST /api/Patients`, `POST /api/Doctors`, all of which inherit
  `CreatePersonDTO`. `DELETE /api/People/{id}` was removed on 11 September and **restored on
  18 October**, admin-only and guarded: `clsPersonDA.IsUsedAsync` refuses any person a user or
  a patient belongs to, which is almost all of them. The original worry was destroying a person
  the API cannot recreate; the guard is what answers it.

**Both sub-resources of a visit are guarded the same way**, as of 11 September. All four
diagnosis actions call
`_authorizationService.AuthorizeAsync(User, visitId, "ManagePrescription")` and `Forbid()` on
failure, matching the prescription endpoints. A doctor who is not on the visit gets 403 for
either. Three things about that:

- **A role attribute cannot express this rule.** `[Authorize(Roles = …)]` runs before the
  action and never sees which visit was requested, so it can only ask *what role are you*, not
  *is this your patient*. That is why the check is imperative, in the method body.
- **The guard sits above the 404 and the 409**, so a doctor who is not on the visit cannot
  learn whether a diagnosis exists by probing — the same reasoning as the identical 401 on the
  two login branches.
- **The strict direction was a decision, not a fix.** The mismatch could have been resolved by
  loosening prescriptions instead, since covering doctors are normal in a clinic. That was
  considered and declined on 11 September.

`IAuthorizationService` is injected by **constructor** here rather than `[FromServices]`,
because all four actions need it; `PrescriptionsController` still uses `[FromServices]`.

Two loose ends: the policy is still named `ManagePrescription` while guarding a diagnosis in
four places (`ManageVisitRecord` would describe it), and `POST` checks `visit.IsLocked` while
`PUT`/`DELETE` check `HasCheckedOutAsync` — two ways to ask the same question.

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
- **The whole schema lives in the committed `.bak` and nowhere else.** There is no `.sql`
  file in the repo and no migration mechanism, by decision: eight stored procedures, one
  trigger and every table can be read only by restoring the backup. A schema change means
  running the DDL by hand and refreshing `ClinicManagementSystem.bak` afterwards. Backing up
  straight into the repo fails with **OS error 5** because SQL Server's service account cannot
  write inside the user folder, so back up to the instance's own `MSSQL\Backup` directory and
  copy the file across.
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
