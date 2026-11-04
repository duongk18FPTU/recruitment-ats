# Delivery Plan

> Status: AGREED — implementation follows these milestones in order. Dates are not fixed yet; each milestone ends with a demo, not on a calendar day.

The backlog ([INVEST requirements](03-invest-requirements.md)) has 34 stories and 149 points: 26 Must-have (115 points) and 8 Should-have (34 points). The plan delivers it as four vertical slices after a foundation milestone. Every slice ends with something a hiring team could use and demo end to end, and the riskiest part of the product (AI screening with evidence) comes first.

## 1. Milestones

| Milestone | Goal / demo | Stories | Points |
|---|---|---|---|
| **M0 Foundation** | CI is green on every push; Postgres + pgvector runs locally; solution skeleton passes architecture tests; first EF migration matches `schema.sql` | — (technical) | 0 |
| **M1 Core screening** | Log in → create a position with weighted criteria → add a CV → AI scores it per criterion with quotes → override a criterion → move the application through stages | US01, US02, US20, US04, US05, US06, US07, US08, US09, US11, US12, US13 | 46 |
| **M2 Interviews and candidates** | Publish a job post with share links → a candidate applies on the career site with consent and gets a receipt → schedule an interview → blind scorecards → outcome emails with a 10-minute cancel window → the candidate leaves the pool or deletes their data | US14, US15, US22, US23, US24, US27, US29, US31 | 40 |
| **M3 Intake, pool, and insight** | Emailed CVs land in the CV inbox → search the talent pool and add a past candidate → export to Excel → dashboard, reports, and audit log | US25, US26, US30, US03, US17, US19 | 29 |
| **M4 Should-have** | Bulk import, report export, profile, editable templates, CV replacement, and the three AI-assistance features | US10, US16, US18, US21, US28, US32, US33, US34 | 34 |

M1–M3 together are exactly the 26 Must-have stories. M4 can be cut or reordered without breaking anything delivered before it.

**Cross-milestone note.** AC-US09 #1 also requires a receipt email, which needs US27 (M2). In M1, adding an application queues no email; the receipt is added and the AC fully passes in M2.

### Why this order

- **M1 first** proves the product's main idea and its largest technical risk (prompting, redaction, quote verification, the Worker queue) while the codebase is still small. AI runs with `FakeResumeAnalyzer` in tests and DeepSeek on synthetic CVs in development (AD-12).
- **M2** adds the second frontend and outgoing email; US29 (sender settings) lands here because US27 needs it.
- **M3** builds on data that M1 and M2 produce: the inbox reuses intake, the pool needs consented candidates, and reports need stage history and scorecards.
- **M4** depends only on earlier milestones; US33 also needs embeddings (AD-13).

## 2. M0 Foundation checklist

| # | Task | Needs network | Status |
|---|---|---|---|
| 1 | Delivery plan (this document) | No | Done |
| 2 | CI workflow: OpenAPI validation, reference schema on Postgres + pgvector, backend and frontend jobs that start once the code exists | No (runs on GitHub) | Done |
| 3 | Local Postgres 16 + pgvector with Docker Compose, `.env.example`, schema check script | ~150 MB image | Done |
| 4 | Install .NET 10 SDK; solution skeleton, `Directory.Build.props`, `global.json`, architecture tests | ~250 MB SDK + NuGet | Done |
| 5 | Persistence: `AtsDbContext`, migration `M1CoreScreening` (13 M1 tables), compared with `schema.sql` by Testcontainers | NuGet | Done |
| 6 | Frontend npm workspace: two Vite apps, shared packages, API types generated from `openapi.yaml` | ~175 MB npm | Done |

M0 is complete: every row above is done and CI checks the contract, the reference schema, the backend, and the frontend on every push. Auth and Users end to end is the first slice of M1.

## 3. Definition of Done (every story)

1. All acceptance criteria of the story pass as automated tests (unit or integration; external services use fakes).
2. Authorization and error cases from the acceptance criteria are tested, not only the happy path.
3. The API matches `docs/api/openapi.yaml`; a contract change is made in the YAML first, in the same pull request.
4. Schema changes come as an EF Core migration and keep `docs/database/schema.sql` equivalent.
5. Actions that the business rules require to be audited write an audit entry.
6. CI is green; no secrets or real CVs are committed.
7. Documentation changes in the same commit as the code: when the implementation differs from a document (structure, packages, schema, contract, decisions), the document is updated, and this plan's checklist and the README progress are ticked.

## 4. Local environment

```bash
cp .env.example .env                 # then set POSTGRES_PASSWORD
docker compose up -d postgres        # Postgres 16 + pgvector on localhost:5433
./deploy/postgres/check-schema.sh    # applies schema.sql to a throwaway database

cd src/backend
dotnet build                         # warnings are errors
dotnet test                          # all test projects (Microsoft.Testing.Platform)
dotnet run --project src/RecruitmentAts.Api   # http://localhost:5080/health

# apply migrations to the dev database (ConnectionStrings__Ats from .env)
export ConnectionStrings__Ats="$(grep '^ConnectionStrings__Ats=' ../../.env | cut -d= -f2-)"
dotnet tool restore
dotnet ef database update --project src/RecruitmentAts.Infrastructure --startup-project src/RecruitmentAts.Infrastructure

cd ../frontend
npm install
npm run dev:internal                 # http://localhost:5173 (career site: npm run dev:career → 5174)
```

The development database `ats` is created with the `vector` and `btree_gist` extensions; EF Core migrations own its tables. The other services in [`compose.yaml`](09-folder-structure.md#2-repository-layout) (api, worker, web apps, Caddy, Mailpit, GreenMail) are added when the code that needs them exists.

**Related:** [INVEST requirements](03-invest-requirements.md) · [Folder structures — order of creation](09-folder-structure.md#8-order-of-creation) · [C4 deployment](06-c4-architecture.md)
