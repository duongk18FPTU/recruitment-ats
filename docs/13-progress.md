# Progress

> The single place where implementation progress is tracked. Updated in the same commit as the work (Definition of Done #7 in the [delivery plan](12-delivery-plan.md)); the README shows only the milestone line.

**Status values:** `TODO` · `DOING` · `PARTIAL` (done except criteria waiting for a later milestone) · `DONE`.
**AC:** acceptance criteria from [INVEST requirements](03-invest-requirements.md#acceptance-criteria) that pass as automated tests / total.

## Summary

| Milestone | Release | Stories done | Points done | Status |
|---|---|---|---|---|
| M0 Foundation | — | — | — | `DONE` |
| M1 Core screening | **MVP** | 3 / 12 | 7 / 46 | `DOING` |
| M2 Interviews and candidates | — | 0 / 9 | 0 / 45 | `TODO` |
| M3 Intake, pool, and insight | **v1.0** | 0 / 6 | 0 / 29 | `TODO` |
| M4 Should-have | v1.1 | 0 / 8 | 0 / 34 | `TODO` |
| **Total** | | **3 / 35** | **7 / 154** | |

**Now:** M1 → US04 View positions.

## M0 Foundation — `DONE`

| # | Work | Status | Commit |
|---|---|---|---|
| 0.1 | Delivery plan | `DONE` | `ebdabdd` |
| 0.2 | CI: OpenAPI validation, reference schema on Postgres + pgvector, backend and frontend jobs | `DONE` | `ebdabdd` |
| 0.3 | Local Postgres 16 + pgvector (Compose), `.env.example`, schema check script | `DONE` | `ebdabdd` |
| 0.4 | .NET 10 solution skeleton, architecture tests | `DONE` | `c9d3320` |
| 0.5 | M1 persistence: 13 tables, migration `M1CoreScreening`, compared with `schema.sql` | `DONE` | `0fe53df` |
| 0.6 | Frontend npm workspace: internal and career apps, `api-client`, `ui` | `DONE` | `8134f8f` |

## M1 Core screening (MVP) — `DOING`

| Order | Item | Pts | Backend | Frontend | AC | Status |
|---|---|---|---|---|---|---|
| 1 | T1.1 Cross-cutting (errors, actor, audit, unit of work, contract test) | — | ✓ | — | — | `DONE` |
| 2 | T1.2 Seed (first Admin, departments) | — | ✓ | — | — | `DONE` |
| 3 | T1.3 Internal app shell (router, query, auth client, AppShell, 403) | — | — | ✓ | — | `DONE` |
| 4 | US01 Login (+ first-login password change) | 3 | ✓ | ✓ | 4 / 4 | `DONE` |
| 4 | US02 Logout | 1 | ✓ | ✓ | 2 / 2 | `DONE` |
| 4 | US20 Manage users | 3 | ✓ | ✓ | 5 / 5 | `DONE` |
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
| 6 | US35 Notifications | 5 | | | 0 / 5 | `TODO` |

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
| 2026-11-04 | The contract has 33 error codes (documents said 32); C# `ErrorCode` is checked against the YAML enum | 09 §3.2, 10 §1 |
| 2026-11-04 | `Actor.Type` reuses `AuditActorType`; `IpAddress` is `IPAddress?` (stored as `inet`) | 10 §1 |
| 2026-11-04 | `CreatedAt` / `UpdatedAt` set by `TimestampInterceptor` from `IClock` | 09 §3.3, 10 §1 |
| 2026-11-04 | Seed runs with the migrations as `api migrate`; the first Admin comes from secrets. The first-login password change was an open question here; it was decided on 2026-11-05 (below) | 06, 07, 09 §3.3, §7, README |
| 2026-11-04 | `Pbkdf2PasswordHasher` added in T1.2 (needed by the seed), not US01 | 09 §3.3 |
| 2026-11-05 | **Issued passwords must be replaced at the first login** (accounts are issued, never self-registered, and the Admin knows a temporary password): column `users.must_change_password` (migration `UserMustChangePassword`), error code `PASSWORD_CHANGE_REQUIRED`, AC-US01 #4 and AC-US20 #5. `changePassword` (US21) is built in M1 for this; the rest of US21 stays in M4 | 02, 03, 07, 08, 10, 11, `openapi.yaml`, `schema.sql` |
| 2026-11-05 | SCR-01 has no "Ghi nhớ đăng nhập" (the refresh cookie already lasts 7 days) and no "Quên mật khẩu?" (no reset API; an Admin issues a new password in US20) | 05 |
| 2026-11-05 | Every endpoint requires a signed-in user unless `[AllowAnonymous]`; an unknown `/api` route therefore answers 401 to anonymous callers (existence is not revealed) | 10 §2 |
| 2026-11-05 | Be Vietnam Pro self-hosted with `@fontsource/be-vietnam-pro` (found missing when SCR-01 was compared with Figma in the browser) | 05, 09 §4.5 |
| 2026-11-05 | Logout clears the client session even when the API call fails, and is allowed before an issued password is replaced (a user can always leave) | 10 §2 |
| 2026-11-05 | `updateUser` takes an optional `temporaryPassword`: an Admin issues a new password to a user who forgot theirs (must be replaced at the next login; the user's sessions end; a lockout ends). This is the "Admin issues a new password" promised above for SCR-01 | 02, 08, 10, `openapi.yaml` |
| 2026-11-05 | Deactivating a user revokes their refresh tokens; an access token already issued stays valid until it expires (at most 15 minutes) | 10 |
| 2026-11-05 | SCR-70 labels a deactivated account "Đã khoá" (Figma); the row menu offers "Khoá tài khoản" / "Mở khoá", hidden on one's own row. The user form is a modal (not in Figma) | 05 |
| 2026-11-05 | Roles from `x-roles` are enforced with `[Authorize(Roles = RoleNames.…)]`; enum query parameters bind by their contract names (`?role=HR_MANAGER`), other spellings → 400 | 09 §3.4, 10 |
| 2026-11-05 | Internal shared components `Modal`, `Pagination`, `RowMenu`, `Toasts` live in `apps/internal/src/components` (internal only); form and button styles moved from `auth.css` to `app.css`. The user form uses plain controlled inputs (React Hook Form + Zod still arrive with US05) | 09 §4.2 |
| 2026-11-05 | The user form checks a taken email while the Admin types (400 ms after the last key) with `listUsers` filtered to an exact match — no new endpoint; `DUPLICATE_EMAIL` (409) still covers two Admins saving at once | 05 |
| 2026-11-05 | Pagination (Figma component `Pagination`, Style=Table, and code): the "1–20 trong 126" summary is removed, controls are 28 px and right-aligned. Updated in Figma for SCR-10, 11, 20, 40, 45, 70 and in their exported screenshots | 05, `images/screens` |
| 2026-11-05 | The add-user button uses the Figma `plus` icon (16 px) instead of a typed "+" | — |
| 2026-11-05 | **New story US35 Notifications (M, 5 points, M2 after US15) and BR-29**: the design told nobody inside the team about new work (the interview invitation goes to the candidate only; panel members had to open the dashboard). Bell with unread count, panel `SCR-91`, notifications to the people an event concerns, interview emails to panel members (10-minute window like BR-23), scorecard reminder after 24 h. Table `notifications`, 4 operations (92 in total) | 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 12, README, `openapi.yaml`, `schema.sql` |
| 2026-11-05 | No global search: the Figma topbar had a "Tìm ứng viên, vị trí, mã hồ sơ" box that no story or operation covered; removed from the `Topbar` component (17 screens re-exported). Each list keeps its own search | 01, 04, 05, `images/screens` |
| 2026-11-05 | Known limitation: two browser tabs refreshing with the same cookie at the same moment look like token reuse and end the session (single-flight works per tab only) | — |
| 2026-11-04 | Internal app: every IA page has its route and sidebar item from T1.3; a page whose story is not built shows "Chức năng này đang được xây dựng". Nav icons come with the `ui` icon set | 09 §4.2 |
| 2026-11-04 | React Router 8 (docs did not name a version); `packages/api-client` has its own Vitest project | 09 §4.1 |
| 2026-11-04 | Contract test checks routes against the YAML (name, method, path, no duplicates); operations of unbuilt stories have no route yet | 09 §3.6 |
