# Clinic Management System

ASP.NET Core 8 Web API for a clinic (people, patients, doctors, appointments, visits,
medicines, prescriptions), with JWT auth. An Angular frontend is planned but not started.

## Where the code lives

Three sibling projects under this working directory, `Clinic Fullstack App\backend\`:

| Project | Path | Contains |
|---|---|---|
| API | `Clinic Management System\` | Controllers, DTOs, `Program.cs` |
| Business | `Clinic_Business\` | `clsUser`, `clsPerson`, `clsPatient`, … |
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

## Architecture

Classic three-tier, 3-layer naming (`clsX` / `clsXDA`). Data access is **raw ADO.NET**
— `SqlConnection` / `SqlCommand` / `SqlParameter`, no EF Core, and that is a deliberate
choice. Don't propose EF Core.

Business classes are **active record** (Fowler's term, and a deliberate pattern rather than
debt): the object wraps a row, holds its own `Save()`, and carries its own `IsValid()`.
`Save()` gates on `IsValid()` and then switches on a private `enMode _Mode` field — `Add`
calls `_CreateUser()`, `Update` calls `_UpdateUser()`. `_Mode` is set to `Add` by the public
constructor and to `Update` by the private one that only `MapUser` uses, so an object loaded
from the database updates and an object built by hand inserts.

Mostly static-entry (`clsUser.FindByEmail(...)`), with one exception described under
*Dependency injection* below.

Two things worth remembering:

- `clsUser._CreateUser()` writes **both** the `People` and `Users` rows in a single
  `CreateUser` stored-procedure call, so there is no two-step orphan-row window in C#.
  Whether the procedure itself is transactional cannot be verified from this repo — see
  the note about unscripted procedures below.
- Most `Find`-style methods pass results back through `ref` parameters, which is why they
  can't be made `async` without changing shape first. **`clsUser` and `clsUserDA` are the
  exception** — they pass a `DataRow?` and map it, so the user path is already async-able.
  The eight-`ref` signatures live in `clsPerson`, `clsAppointment`, `clsVisit`,
  `clsDiagnosis`, `clsMedicine` and `clsPrescriptionItem`.

**The data-access layer no longer swallows exceptions.** All 52 `try/catch` +
`Console.WriteLine` blocks were removed, and `Clinic_DataAccess` now contains zero `catch`
statements. A `SqlException` propagates to `UseExceptionHandler` and becomes a 500
`ProblemDetails` with the exception in the log. So a `false`/`null` return now means
"validation failed" or "zero rows", not "something threw" — which is what makes the
distinction between 422 and 500 in the controllers meaningful.

## Dependency injection

`Program.cs` is the composition root. Registered there: three
`AddSingleton<IAuthorizationHandler, …>` handlers, and
`AddScoped<IUserRepository, UserRepository>()` at line 85.

`IUserRepository` (in `Clinic_Business/Interfaces/`, implemented by
`Clinic_Business/UserRepository.cs`) is the **only** interface in either library and the only
constructor-injected service of its kind. It has one member, `FindByEmail`, and
`AuthController` receives it through its constructor. The implementation currently delegates
to the static `clsUser.FindByEmail` **on purpose**: `UsersController` and
`PersonOwnerOrAuthorizedHandler` still call that static, and two independent copies of the
load logic would let the injected and static paths drift. Inline the `clsUserDA` call once
the last static caller is gone.

`user.Save()` is still called directly on the object in all four `AuthController` actions.
Persistence ownership — active record versus repository — is an open question, not an
oversight; see the roadmap.

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

Both secrets come from configuration and the app **refuses to start** without either:

- `Jwt:Key` — read once in `Program.cs:18` and once each in `AuthController`'s `Login` and
  `Refresh` (via the injected `IConfiguration`). All three read the same key; there is no
  hardcoded copy left.
- `ConnectionStrings:Clinic` — read in `Program.cs:19` and assigned to the static
  `clsDataAccessSettings.ConnectionString` at line 21, which is how the data-access layer
  still reaches it.

Both live in user-secrets, which load **only in Development**. For any other environment use
environment variables, where `:` becomes `__` (`ConnectionStrings__Clinic`).

## Current focus

The project doubles as a learning exercise; see the roadmap and code review at
https://claude.ai/code/artifact/b9312fba-3f16-4f6e-a14b-1de5d8531f72 — it is the source of
truth for what is fixed and what is still open. Update it there rather than restating
findings elsewhere.
