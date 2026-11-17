# Farm Production Management System (FPMS)

An on-premise web application for whiteleg shrimp production: ponds, stocking, daily logs, harvests, sales, permits and reports for a farm's owners and technicians. It runs on a farm's local network without Internet access.

## Tech stack
- **Backend:** ASP.NET Core on .NET 9 with ASP.NET Boilerplate 10.2 and EF Core 9 (`aspnet-core/`)
- **Frontend:** Angular 19.2 (`angular/`)
- **Database:** SQL Server

## Run
Open the repository in GitHub Codespaces or a VS Code Dev Container; `.devcontainer/start-fpms.sh` starts SQL Server, creates the database and starts the API (port 44311) and the app (port 4200).

To run it on your own computer, install the .NET 9 SDK, Node 22 and Docker, then from the repository root:

```
(cd angular && yarn install)
bash .devcontainer/start-fpms.sh
```

Set `SQL_SA_PASSWORD` and `ADMIN_INITIAL_PASSWORD` in the environment, or let the script generate them into `.devcontainer/local.env`. Sign in as `admin` with a blank farm code. `SEED_SAMPLE_DATA=0` starts with a clean database instead of the sample farm.