# Delivery Plan

> Status: ACTIVE — milestones are built for real, one after another, in the order below. **M1 is the MVP**: the smallest product a hiring team can use, so it comes first and nothing else starts before it works. Progress is tracked only in [Progress](13-progress.md).

The backlog ([INVEST requirements](03-invest-requirements.md)) has 35 stories and 154 points: 27 Must-have (120 points) and 8 Should-have (34 points). After a foundation milestone, the plan delivers four vertical slices. Each slice ends with a demo a hiring team could use end to end, and the riskiest part of the product (AI screening with evidence) comes first.

## 1. Milestones

| Milestone | Goal / demo | Stories (in build order) | Points | Release |
|---|---|---|---|---|
| **M0 Foundation** | CI green on every push; Postgres + pgvector locally; backend and frontend skeletons; first migration matches `schema.sql` | — (technical) | 0 | — |
| **M1 Core screening** | Log in → create a position with weighted criteria → add a CV → AI scores it per criterion with quotes → override a criterion → move the application through stages | US01, US02, US20, US04, US05, US06, US07, US09, US08, US11, US12, US13 | 46 | **MVP** |
| **M2 Interviews and candidates** | Publish a job post with share links → a candidate applies on the career site with consent and gets a receipt → schedule an interview → blind scorecards → panel members are notified in the app and by email → outcome emails with a 10-minute cancel window → the candidate leaves the pool or deletes their data | US29, US27, US22, US23, US24, US31, US14, US15, US35 | 45 | — |
| **M3 Intake, pool, and insight** | Emailed CVs land in the CV inbox → search the talent pool and add a past candidate → export to Excel → dashboard, reports, and audit log → production deployment | US25, US26, US30, US03, US17, US19 | 29 | **v1.0** (all Must-have) |
| **M4 Should-have** | CV replacement, profile, editable templates, report export, bulk import, and the three AI-assistance features | US10, US21, US28, US18, US16, US32, US34, US33 | 34 | v1.1 |

M1–M3 together are exactly the 27 Must-have stories. M4 can be cut or reordered without breaking anything delivered before it.

### Why this order

- **M1 first (MVP)** proves the product's main idea and its largest technical risk — text extraction, redaction, prompting, quote verification, the Worker queue — while the codebase is still small. AI runs with `FakeResumeAnalyzer` in tests and DeepSeek on synthetic CVs in development (AD-12).
- **M2** adds the second frontend and outgoing email; US29 (sender settings) comes first because US27 needs it, and US27 before US24 because applying sends a receipt.
- **M3** builds on data that M1 and M2 produce: the inbox reuses intake, the pool needs consented candidates, and reports need stage history and scorecards. It ends with the production deployment, so v1.0 is all Must-have stories running on the real domains.
- **M4** depends only on earlier milestones; US33 also needs embeddings (AD-13).

## 2. Work inside each milestone

Technical tasks (T) are the parts no single story owns. They are done at the point shown, so each story can be finished completely when it is started.

### M1 Core screening (MVP)

| Order | Work |
|---|---|
| 1 | **T1.1 Cross-cutting:** `AppException` + `ErrorCodes` → ProblemDetails, `Actor`, `AuditWriter`, `UnitOfWork`, timestamps from `IClock`, OpenAPI contract test (every `operationId` has exactly one route) |
| 2 | **T1.2 Seed:** first Admin from environment variables, departments |
| 3 | **T1.3 Internal app shell:** React Router, TanStack Query, `api-client` auth (`auth.ts`, `problem.ts`), AppShell, sidebar by role, 403 page |
| 4 | US01 Login, US02 Logout, US20 Manage users |
| 5 | US04 View positions, US05 Create position, US06 Edit position, US07 Close / reopen |
| 6 | **T1.4 CV files:** `IFileStorage` (local volume), text extraction (PdfPig, OpenXml), file rules (BR-15) |
| 7 | US09 Add application with CV, US08 View applications, US11 Move stage |
| 8 | **T1.5 AI screening core:** `Redactor`, `QuoteVerifier`, `FitScoreCalculator`, `FakeResumeAnalyzer`, `OpenAiCompatibleResumeAnalyzer`, analysis queue, Worker `AnalysisRunner` |
| 9 | US12 Analyze CV with AI, US13 Override AI result |
| 10 | **T1.6 MVP demo:** Compose services `api`, `worker`, `internal-web`; demo script with synthetic CVs; README "Getting started" updated |

**Done when:** every M1 acceptance criterion passes as an automated test, except those listed under "waiting for a later milestone" in [Progress](13-progress.md); the demo above runs from `docker compose up` with DeepSeek on synthetic CVs; CI is green.

### M2 Interviews and candidates

| Order | Work |
|---|---|
| 1 | **T2.1 Email:** email queue, Worker `EmailDispatcher` with retries, Mailpit in Compose, Data Protection keys for stored passwords |
| 2 | US29 Configure mailbox and sender, US27 Automatic candidate emails (+ the M1 email criteria waiting for it) |
| 3 | **T2.2 Career site:** routes, layout, public API client, Turnstile, rate limits (BR-21), Open Graph page for crawlers (AD-04) |
| 4 | US22 Manage job post, US23 Browse job posts, US24 Apply online, US31 Manage data consent |
| 5 | US14 Schedule interview (+ Interviewer assignment criteria from M1), US15 Submit scorecard |
| 6 | US35 Notifications (bell, panel, panel emails through `EmailDispatcher`, Worker `ScorecardReminderJob`) — after US14 and US15 because most events come from them |

**Done when:** every M2 criterion and the M1 criteria that waited for M2 pass; the M2 demo runs end to end with Mailpit; CI is green.

### M3 Intake, pool, and insight

| Order | Work |
|---|---|
| 1 | **T3.1 Mailbox:** Worker `MailboxPoller`, GreenMail in Compose |
| 2 | US25 Process CV inbox, US26 Use talent pool, US30 Export candidates (ClosedXML) |
| 3 | US03 Dashboard, US17 View reports (Chart.js), US19 View audit log |
| 4 | **T3.2 Retention:** Worker `RetentionJob` (BR-17, BR-22, BR-24) |
| 5 | **T3.3 Production:** Caddy with the two domains, health checks, nightly backup and restore test, production AI provider decision (C4 open item) |

**Done when:** all Must-have criteria pass; v1.0 runs on the production VM; a backup has been restored once.

### M4 Should-have

| Order | Work |
|---|---|
| 1 | US10 Edit candidate information, US21 Personal profile, US28 Manage email templates |
| 2 | US18 Export reports (QuestPDF), US16 Bulk import |
| 3 | US32 Suggest criteria, US34 Suggest interview questions |
| 4 | **T4.1 Embeddings:** `Pgvector.EntityFrameworkCore`, embedding tables migration, Worker `EmbeddingRunner` (AD-13) |
| 5 | US33 Match talent pool to a position |

## 3. Risks

| Risk | Effect | Response |
|---|---|---|
| AI answers are inconsistent or quote text that is not in the CV | Wrong scores in the MVP | Quote verification (`UNVERIFIED` counts 0), schema-checked JSON, fixed synthetic CV set compared before each prompt change |
| PDF text extraction fails on some CVs (scans, unusual layouts) | Application cannot be analyzed | Reject with 400 when no text is found (AC-US09 #4); OCR is out of scope |
| Development AI provider stores data abroad (DeepSeek) | Personal-data law (Law 91/2025) | Synthetic CVs only until a production provider is chosen in T3.3 (AD-12) |
| MVP is the largest milestone (46 points) | Late first demo | Build order above; each story is finished completely before the next starts |
| Emails land in spam or the SMTP account is blocked | Candidates are not informed | Mailpit in development, failures visible in the UI (US27), sender configured and tested by the Admin (US29) |
| Toolchain versions move fast (.NET 10, TypeScript, Vite) | Broken builds | Exact versions in `global.json`, `Directory.Packages.props`, and `package-lock.json`; upgrades in their own commit |

## 4. Definition of Done (every story)

1. All acceptance criteria of the story pass as automated tests (unit or integration; external services use fakes). A criterion that needs a later milestone is listed in [Progress](13-progress.md) and finished there.
2. Authorization and error cases from the acceptance criteria are tested, not only the happy path.
3. The API matches `docs/api/openapi.yaml`; a contract change is made in the YAML first, in the same commit.
4. Schema changes come as an EF Core migration and keep `docs/database/schema.sql` equivalent.
5. Actions that the business rules require to be audited write an audit entry.
6. CI is green; no secrets or real CVs are committed.
7. Documentation changes in the same commit as the code: when the implementation differs from a document (structure, packages, schema, contract, decisions), the document is updated, and [Progress](13-progress.md) is ticked.

**Related:** [Progress](13-progress.md) · [INVEST requirements](03-invest-requirements.md) · [Folder structures — order of creation](09-folder-structure.md#8-order-of-creation) · [C4 deployment](06-c4-architecture.md)
