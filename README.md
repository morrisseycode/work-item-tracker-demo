# WorkItemTracker

A small ASP.NET Core Web API for tracking work items in projects.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Optional: Visual Studio, or VS Code with the REST Client extension, to run the
  requests in `WorkItemTracker.http`.

Run every command below from the repository root.

## 1. Create and seed the database

```
dotnet run --project tools/WorkItemTracker.Seeder
```

The seeder creates the SQLite database `src/WorkItemTracker.Api/workitemtracker.db`
from the EF Core migrations and fills it with the same data every time:

- two users, `nadia` and `tobias`, with three projects each
- 20 assignees
- 200 work items spread across projects, statuses, priorities, and due dates; about
  15 percent are unassigned

Run it again whenever you want to reset the data. Stop the API first, because the
seeder deletes and recreates the database file.

For a different number of work items, pass `--work-items`:

```
dotnet run --project tools/WorkItemTracker.Seeder -- --work-items 100000
```

## 2. Mint a development token for each user

The seeded data belongs to the users `nadia` and `tobias`. A token's `sub` claim is
the name passed to `--name`, so mint one token per user:

```
dotnet user-jwts create --project src/WorkItemTracker.Api --name nadia --output token
dotnet user-jwts create --project src/WorkItemTracker.Api --name tobias --output token
```

Each command prints a token. The signing key is stored in your user secrets, so the
tokens work only on your machine.

Create a file named `.env` in the repository root and put one token on each line:

```
NADIA_TOKEN=<nadia's token>
TOBIAS_TOKEN=<tobias's token>
```

Git ignores `.env`, so the tokens never reach source control.

## 3. Run the API

```
dotnet run --project src/WorkItemTracker.Api --launch-profile http
```

The API listens on `http://localhost:5272`. In Development, each SQL command is
logged to the console.

Call it with a token:

```
curl -H "Authorization: Bearer <token>" http://localhost:5272/api/workitems
```

Or open `WorkItemTracker.http` and send the requests in order. In VS Code, the
REST Client extension reads the tokens from `.env`. In other tools, put the tokens in
the `@nadiaToken` and `@tobiasToken` variables instead, and don't commit them.
Every request without a valid token returns `401 Unauthorized`.

## Endpoints

| Method and route | Behavior |
| --- | --- |
| `GET /api/projects` | Your projects |
| `POST /api/projects` | Create a project |
| `GET /api/assignees` | All assignees, ordered by display name |
| `GET /api/workitems` | Your work items; filters and paging below |
| `GET /api/workitems/{id}` | One of your work items |
| `POST /api/workitems` | Create a work item in one of your projects |
| `PUT /api/workitems/{id}` | Update title, description, priority, due date, assignee |

`GET /api/workitems` takes `projectId`, `status`, `dueBefore` (exclusive, `yyyy-MM-dd`),
`page` (default 1), and `pageSize` (default 50). A `pageSize` above 100 is rejected
with `400 Bad Request`.

## Run the tests

```
dotnet test
```

The tests use an in-memory SQLite database and mint their own tokens, so they need
none of the steps above.

## Changing the schema

The EF Core command-line tool is pinned in `dotnet-tools.json`. Run `dotnet tool restore`
before using `dotnet ef` to add migrations.