# Progress

> The single place where implementation progress is tracked. Updated in the same commit as the work (Definition of Done #7 in the [delivery plan](12-delivery-plan.md)); the README shows only the milestone line.

**Status values:** `TODO` · `DOING` · `PARTIAL` (done except criteria waiting for a later milestone) · `DONE`.
**AC:** acceptance criteria from [INVEST requirements](03-invest-requirements.md#acceptance-criteria) that pass as automated tests / total.

## Summary

| Milestone | Release | Stories done | Points done | Status |
|---|---|---|---|---|
| M0 Foundation | — | — | — | `DONE` |
| M1 Core screening | **MVP** | 0 / 12 | 0 / 46 | `TODO` |
| M2 Interviews and candidates | — | 0 / 8 | 0 / 40 | `TODO` |
| M3 Intake, pool, and insight | **v1.0** | 0 / 6 | 0 / 29 | `TODO` |
| M4 Should-have | v1.1 | 0 / 8 | 0 / 34 | `TODO` |
| **Total** | | **0 / 34** | **0 / 149** | |

**Now:** M1 → T1.1 Cross-cutting.

## M0 Foundation — `DONE`

| # | Work | Status | Commit |
|---|---|---|---|
| 0.1 | Delivery plan | `DONE` | `ebdabdd` |
| 0.2 | CI: OpenAPI validation, reference schema on Postgres + pgvector, backend and frontend jobs | `DONE` | `ebdabdd` |
| 0.3 | Local Postgres 16 + pgvector (Compose), `.env.example`, schema check script | `DONE` | `ebdabdd` |
| 0.4 | .NET 10 solution skeleton, architecture tests | `DONE` | `c9d3320` |
| 0.5 | M1 persistence: 13 tables, migration `M1CoreScreening`, compared with `schema.sql` | `DONE` | `0fe53df` |
| 0.6 | Frontend npm workspace: internal and career apps, `api-client`, `ui` | `DONE` | `8134f8f` |

## M1 Core screening (MVP) — `TODO`

| Order | Item | Pts | Backend | Frontend | AC | Status |
|---|---|---|---|---|---|---|
| 1 | T1.1 Cross-cutting (errors, actor, audit, unit of work, contract test) | — | | — | — | `TODO` |
| 2 | T1.2 Seed (first Admin, departments) | — | | — | — | `TODO` |
| 3 | T1.3 Internal app shell (router, query, auth client, AppShell, 403) | — | — | | — | `TODO` |
| 4 | US01 Login | 3 | | | 0 / 3 | `TODO` |
| 4 | US02 Logout | 1 | | | 0 / 2 | `TODO` |
| 4 | US20 Manage users | 3 | | | 0 / 4 | `TODO` |
| 5 | US04 View positions | 3 | | | 0 / 2 | `TODO` |
| 5 | US05 Create position | 5 | | | 0 / 3 | `TODO` |
| 5 | US06 Edit position | 3 | | | 0 / 2 | `TODO` |
| 5 | US07 Close / reopen position | 2 | | | 0 / 2 | `TODO` |
| 6 | T1.4 CV files (storage, text extraction, BR-15) | — | | — | — | `TODO` |
| 7 | US09 Add application with CV | 5 | | | 0 / 5 | `TODO` |
| 7 | US08 View applications | 5 | | | 0 / 3 | `TODO` |
| 7 | US11 Move pipeline stage | 5 | | | 0 / 5 | `TODO` |
| 8 | T1.5 AI screening core (redactor, quote check, score, analyzers, Worker) | — | | — | — | `TODO` |
| 9 | US12 Analyze CV with AI | 8 | | | 0 / 5 | `TODO` |
| 9 | US13 Override AI result | 3 | | | 0 / 3 | `TODO` |
| 10 | T1.6 MVP demo (Compose api/worker/internal-web, demo script) | — | | | — | `TODO` |

## M2 Interviews and candidates — `TODO`

| Order | Item | Pts | Backend | Frontend | AC | Status |
|---|---|---|---|---|---|---|
| 1 | T2.1 Email (queue, `EmailDispatcher`, Mailpit, Data Protection) | — | | — | — | `TODO` |
| 2 | US29 Configure mailbox and sender | 3 | | | 0 / 3 | `TODO` |
| 2 | US27 Automatic candidate emails | 8 | | | 0 / 6 | `TODO` |
| 3 | T2.2 Career site (routes, public client, Turnstile, rate limits, Open Graph) | — | | | — | `TODO` |
| 4 | US22 Manage job post | 5 | | | 0 / 4 | `TODO` |
| 4 | US23 Browse job posts | 3 | | | 0 / 3 | `TODO` |
| 4 | US24 Apply online | 8 | | | 0 / 5 | `TODO` |
| 4 | US31 Manage data consent | 3 | | | 0 / 3 | `TODO` |
| 5 | US14 Schedule interview | 5 | | | 0 / 4 | `TODO` |
| 5 | US15 Submit scorecard | 5 | | | 0 / 3 | `TODO` |

## M3 Intake, pool, and insight — `TODO`

| Order | Item | Pts | Backend | Frontend | AC | Status |
|---|---|---|---|---|---|---|
| 1 | T3.1 Mailbox (`MailboxPoller`, GreenMail) | — | | — | — | `TODO` |
| 2 | US25 Process CV inbox | 8 | | | 0 / 5 | `TODO` |
| 2 | US26 Use talent pool | 5 | | | 0 / 2 | `TODO` |
| 2 | US30 Export candidates | 3 | | | 0 / 3 | `TODO` |
| 3 | US03 Dashboard | 5 | | | 0 / 3 | `TODO` |
| 3 | US17 View reports | 5 | | | 0 / 2 | `TODO` |
| 3 | US19 View audit log | 3 | | | 0 / 2 | `TODO` |
| 4 | T3.2 Retention (`RetentionJob`) | — | | — | — | `TODO` |
| 5 | T3.3 Production (Caddy, domains, backups, AI provider) | — | | | — | `TODO` |

## M4 Should-have — `TODO`

| Order | Item | Pts | Backend | Frontend | AC | Status |
|---|---|---|---|---|---|---|
| 1 | US10 Edit candidate information | 2 | | | 0 / 2 | `TODO` |
| 1 | US21 Personal profile | 2 | | | 0 / 2 | `TODO` |
| 1 | US28 Manage email templates | 3 | | | 0 / 3 | `TODO` |
| 2 | US18 Export reports | 3 | | | 0 / 2 | `TODO` |
| 2 | US16 Bulk import | 8 | | | 0 / 4 | `TODO` |
| 3 | US32 Suggest criteria from a job description | 3 | | | 0 / 5 | `TODO` |
| 3 | US34 Suggest interview questions | 5 | | | 0 / 5 | `TODO` |
| 4 | T4.1 Embeddings (pgvector, `EmbeddingRunner`) | — | | — | — | `TODO` |
| 5 | US33 Match talent pool to a position | 8 | | | 0 / 5 | `TODO` |

## Criteria waiting for a later milestone

A story is `PARTIAL` while one of these is open; it becomes `DONE` when the criterion passes in the later milestone.

| Criterion | Needs | Finished in |
|---|---|---|
| AC-US09 #1 — receipt email sent when an application is added | US27 emails | M2 |
| AC-US08 #3 — an Interviewer sees only assigned applications | US14 panel assignment | M2 |
| AC-US11 #5 — moving to `REJECTED` / `OFFER` creates a pending email | US27 emails | M2 |

## Decisions and changes during implementation

Deviations from the design documents, kept here so they are easy to review (each is also written into the affected document).

| Date | Change | Documents updated |
|---|---|---|
| 2026-11-04 | Tests on `xunit.v3` + Microsoft.Testing.Platform (VSTest not supported by .NET 10 SDK) | 09 §3.6, §3.8 |
| 2026-11-04 | Tables are migrated per milestone; EF foreign-key indexes off; enum CHECKs generated from C# enums | 07, 09 §3.3 |
| 2026-11-04 | TypeScript 5.9 (tooling does not support 7.0 yet); frontend libraries added with the first feature that needs them | 09 §4.1 |
| 2026-11-04 | Local Postgres on port 5433 (5432 is often taken by a native PostgreSQL) | `.env.example`, README |
| 2026-11-04 | M1 is the MVP; production deployment moved into M3 (v1.0) | 12 |
