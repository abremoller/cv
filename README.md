# CV / Résumé Site

A data-driven personal CV website built with **Blazor WebAssembly** and an **ASP.NET Core** API backed by **SQL Server**. The site renders entirely from data served by a secured API — **no personal data lives in this repository**. The code is the rendering engine; the content lives in a database and is loaded through a secured admin endpoint.

> **Live demo:** _add your URL here_

---

## Why it's built this way

The repository is public, so it is deliberately **data-free**. Everything personal (contact details, history, etc.) is stored in SQL Server and pushed in at deploy time through a secured API. Anyone who clones the repo gets the full application running against **placeholder demo data** — never real data.

## Architecture

Single hosted site: one ASP.NET Core app serves both the API and the compiled WebAssembly client, so there is one deployment, one origin, and no CORS to configure.

```
┌─────────────────────────────────────────────┐
│                 CV.Api (host)                │
│                                              │
│  Browser ──GET /api/cv──► read ──► SQL Server│
│     ▲                                  ▲     │
│     │ serves WASM                      │     │
│     │                     PUT /api/cv  │     │
│  CV.Web (Blazor WASM)   (admin, secured)     │
└─────────────────────────────────────────────┘
```

| Project        | Responsibility                                                        |
| -------------- | --------------------------------------------------------------------- |
| **CV.Shared**  | DTO records — the contract shared by the API and the client           |
| **CV.Api**     | ASP.NET Core host: EF Core + SQL Server, REST API, serves the WASM app |
| **CV.Web**     | Blazor WebAssembly UI; fetches the CV from `/api/cv` and renders it    |
| **CV.Tests**   | xUnit unit + integration tests                                        |

### Tech stack

.NET 8 · Blazor WebAssembly · ASP.NET Core Minimal APIs · EF Core 8 (SQL Server, code-first migrations) · xUnit + FluentAssertions + `WebApplicationFactory` · Swagger / OpenAPI · GitHub Actions

## API

| Method & route   | Auth        | Description                                |
| ---------------- | ----------- | ------------------------------------------ |
| `GET /api/cv`    | public      | Returns the CV document as JSON            |
| `PUT /api/cv`    | **admin**   | Replaces the entire CV document            |

### Security on the admin endpoint

- **API key** supplied via the `CvAdmin__ApiKey` environment variable — never committed.
- **Constant-time** key comparison (`CryptographicOperations.FixedTimeEquals`) to avoid timing attacks.
- **Fail-closed**: if no key is configured, every write is rejected (`503`).
- **Rate-limited** (fixed window) to blunt brute-force attempts (`429`).
- **HTTPS** redirection + HSTS, with forwarded-headers support for a reverse proxy (Plesk / nginx).

## Running locally

No database required. With no connection string configured, the app falls back to an **in-memory** store seeded with placeholder demo data.

```bash
dotnet run --project CV.Api
```

Then open the printed URL. Swagger UI is available at `/swagger` in Development.

## Configuration

Real configuration is provided by environment variables in the hosting environment — never in the repo.

| Variable                    | Purpose                                                        |
| --------------------------- | -------------------------------------------------------------- |
| `ConnectionStrings__CvDb`   | SQL Server connection string. If unset, an in-memory DB is used. |
| `CvAdmin__ApiKey`           | Long random secret required by the `PUT /api/cv` admin endpoint. |

EF Core migrations are applied automatically on startup when a SQL Server connection string is present.

## Updating the CV content

Edit your data file (kept out of source control) and publish it with the helper script:

```powershell
$env:CV_ADMIN_API_KEY = '<your-secret>'
./tools/publish-cv.ps1 -BaseUrl https://your-domain.com -DataFile ./cv-data.local.json
```

## Tests

```bash
dotnet test
```

Covers the data store (round-trip, ordering, replace, seed-once) and the API pipeline (public read, and the fail-closed / unauthorized / authorized write paths).

## Deployment (CI/CD)

[`.github/workflows/deploy.yml`](.github/workflows/deploy.yml) builds, tests, publishes the hosted app, and deploys to Plesk over FTPS on push to `main`. Configure these GitHub secrets: `PLESK_FTP_SERVER`, `PLESK_FTP_USERNAME`, `PLESK_FTP_PASSWORD`, and set the host environment variables above.
