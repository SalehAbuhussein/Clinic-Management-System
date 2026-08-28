# Clinic Management System

A web application for running a small clinic. It keeps track of patients, doctors,
appointments, visits, diagnoses and prescriptions, and controls who is allowed to
see and change what.

The backend is finished enough to use. The frontend is not started yet.

## What it does

- Register patients and doctors, and keep their contact and personal details
- Book appointments, and stop double booking for the same doctor or patient
- Check a patient in for a visit, record a diagnosis, then check them out
- Keep a medicine list and attach prescriptions to a visit
- Sign in with an email and password, and stay signed in with refresh tokens
- Restrict actions by role, so an admin, a staff member and a doctor see different things

Deleting a doctor or a patient does not erase them. It marks them inactive, so past
appointments and visits still make sense.

## Built with

- ASP.NET Core 8 for the web API
- SQL Server for the database, accessed with plain ADO.NET
- JWT for sign in
- Angular for the frontend, planned

## Project layout

```
backend/
  Clinic Management System/   the web API
  Clinic_Business/            business rules
  Clinic_DataAccess/          database access
frontend/                     Angular app, not started
```

## Running it

You need the .NET 8 SDK and a SQL Server instance with the clinic database.

The app does not store passwords or connection strings in the repository, so set them
once on your machine before the first run:

```
cd "backend/Clinic Management System"
dotnet user-secrets set "Jwt:Key" "any random string of 32 characters or more"
dotnet user-secrets set "ConnectionStrings:Clinic" "your SQL Server connection string"
```

Then start it:

```
dotnet run
```

Open `https://localhost:7180/swagger` to see the endpoints and try them.

If you skip the two settings above, the app stops on startup and tells you which one
is missing.

## A note on the database

The database itself is not in this repository. It has several stored procedures that
live only on the server, so you need an existing clinic database to point at.

## Status

The backend works and is still being tidied up. Known gaps are tracked separately,
the main ones being prescriptions having no endpoints yet, and no automated tests.
