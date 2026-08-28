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

Business classes are static-entry (`clsUser.FindByEmail(...)`) with an `enMode.Add/Update`
save pattern: `Save()` calls `base.Save()` for the Person row first, then `_CreateUser()`
or `_UpdateUser()`. Two consequences worth remembering:

- A failure in the user insert leaves an **orphan `Person` row**, because the person was
  already committed.
- `Find`-style methods use `ref` parameters, which is why they can't be made `async`
  without changing shape first.

Every `*DA` method wraps its exception in `try/catch` and `Console.WriteLine`s it, so SQL
errors surface to the API as a plain `false`/`null` and a generic `BadRequest`. When
something fails inexplicably, suspect a swallowed `SqlException`.

## Database facts not visible in the code

- `dbo.Users.Role` **defaults to `'Staff'`** — `clsUserDA.Create` deliberately omits the
  column. An admin promotes a user to `'Admin'` afterwards; there's no endpoint for it yet.
- Role names in use are `'Admin'` and `'Staff'` (see `[Authorize(Roles = ...)]` on
  Doctors, Patients, Visits).
- Connection string is hardcoded in `clsDataAccessSettings.ConnectionString`.
- The JWT signing key is hardcoded in three places: `Program.cs`, and `Login` and
  `Refresh` in `AuthController`. Changing one silently breaks token validation.

## Current focus

The project doubles as a learning exercise; see the roadmap and code review at
https://claude.ai/code/artifact/b9312fba-3f16-4f6e-a14b-1de5d8531f72 — it is the source of
truth for what is fixed and what is still open. Update it there rather than restating
findings elsewhere.
