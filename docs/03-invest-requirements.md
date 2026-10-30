# INVEST Requirements & Acceptance Criteria — Recruitment ATS V1

> Design status: TARGET V1

Each story is small enough to implement and test independently. Authentication, authorization, validation, auditing, and error handling are acceptance constraints, not hidden features. Business rules `BR-xx` are defined in [Use cases](02-use-cases.md#business-rules).

## INVEST checklist

| Principle | Application in this backlog |
|---|---|
| Independent | Each story produces a demonstrable outcome and lists only unavoidable prerequisites. |
| Negotiable | Layout and implementation details may change while the outcome, permissions, and business rules stay fixed. |
| Valuable | Every story names the user benefit. |
| Estimable | Inputs, outputs, roles, and failure outcomes are explicit; each story has a story-point estimate. |
| Small | Each story targets one capability and splits into API, application, persistence, UI, and test tasks; no story exceeds 8 points. |
| Testable | Every story has observable acceptance criteria (below). |

## Product backlog

Priority uses MoSCoW (`M` must, `S` should, `C` could). Story points use the Fibonacci scale.

| ID | User story | Value | Dependencies | Priority | Points |
|---|---|---|---|---|---|
| US01 | As an active user, I want to log in so that I can access the features of my role. | Protected access | Seeded account | M | 3 |
| US02 | As an authenticated user, I want to log out so that this client discards my token and private data. | Session safety | US01 | M | 1 |
| US03 | As any role, I want a dashboard for a date range so that I can see open positions, the pipeline, and upcoming interviews at a glance. | Fast overview | US01, US08 | M | 5 |
| US04 | As any role, I want to search and view positions so that I know which roles are being filled and what they require. | Visibility of open positions | US01 | M | 3 |
| US05 | As an Admin, HR Manager, or Recruiter, I want to create a position with weighted must-have and nice-to-have criteria so that candidates are evaluated against explicit requirements. | Consistent evaluation basis | US01 | M | 5 |
| US06 | As the position owner or an HR Manager, I want to edit a position so that requirements stay accurate; Recruiters may edit only their own positions. | Accurate positions | US04 | M | 3 |
| US07 | As an Admin or HR Manager, I want to close or reopen a position so that the team stops adding candidates once it is filled. | Controlled intake | US04 | M | 2 |
| US08 | As any role, I want to view applications as a Kanban board or table with filters so that I can find candidates quickly; Interviewers see only assigned applications. | Pipeline visibility | US01 | M | 5 |
| US09 | As an Admin, HR Manager, or Recruiter, I want to add an application by uploading a CV so that every candidate is stored in one place without duplicates. | Centralized candidates | US05 | M | 5 |
| US10 | As an Admin, HR Manager, or Recruiter, I want to edit candidate information or replace a CV so that records stay correct. | Accurate data | US09 | S | 2 |
| US11 | As an authorized user, I want to move an application through pipeline stages, with a reason when rejecting, so that progress is tracked and traceable. | Transparent process | US09 | M | 5 |
| US12 | As an Admin, HR Manager, or Recruiter, I want the system to analyze a CV with AI and score it against the position criteria with evidence so that I can prioritize screening. | Faster screening | US05, US09 | M | 8 |
| US13 | As an Admin, HR Manager, or Recruiter, I want to override an AI criterion result with a reason so that the final judgment stays human and auditable. | Human oversight | US12 | M | 3 |
| US14 | As an Admin, HR Manager, or Recruiter, I want to schedule an interview and assign a panel so that interviewers know when and whom they interview. | Coordinated interviews | US11 | M | 5 |
| US15 | As a panel member, I want to submit a scorecard without seeing others' feedback first so that my evaluation is independent. | Fair evaluation | US14 | M | 5 |
| US16 | As an Admin, HR Manager, or Recruiter, I want to import many applications from an Excel file and a ZIP of CVs atomically so that bulk intake is fast and safe. | Fast bulk intake | US09 | S | 8 |
| US17 | As an Admin or HR Manager, I want recruitment reports (funnel, time-to-hire, sources, rejection reasons, AI agreement) so that I can improve the hiring process. | Process insight | US11, US13 | M | 5 |
| US18 | As an Admin or HR Manager, I want to export the current report to PDF or XLSX so that I can share it with management. | Portable reporting | US17 | S | 3 |
| US19 | As an Admin or HR Manager, I want to inspect the audit log so that important actions on candidate data are traceable. | Accountability | US01 | M | 3 |
| US20 | As an Admin, I want to create, edit, activate, and deactivate users so that access is controlled. | Access administration | US01 | M | 3 |
| US21 | As an authenticated user, I want to maintain my profile and password without changing my role so that my account stays current and secure. | Self-service | US01 | S | 2 |

**Total:** 21 stories, 84 points. Must-have stories: 17 (69 points).

## Per-story INVEST review

`PASS` means the design provides enough information for planning and acceptance. It does not claim that implementation or tests exist.

| Story | I | N | V | E | S | T | Notes |
|---|---|---|---|---|---|---|---|
| US01 | PASS | PASS | PASS | PASS | PASS | PASS | Bearer token, 30-minute lifetime, no refresh token in V1. |
| US02 | PASS | PASS | PASS | PASS | PASS | PASS | Client-side discard; no server-side revocation in V1. |
| US03 | PASS | PASS | PASS | PASS | PASS | PASS | KPI list and Interviewer variant are defined. |
| US04 | PASS | PASS | PASS | PASS | PASS | PASS | Read / filter / paging is independently testable. |
| US05 | PASS | PASS | PASS | PASS | PASS | PASS | Criteria model (type, weight 1–5) is explicit. |
| US06 | PASS | PASS | PASS | PASS | PASS | PASS | Ownership (BR-14) and versioning of criteria are explicit. |
| US07 | PASS | PASS | PASS | PASS | PASS | PASS | Effect on intake (BR-05) is explicit. |
| US08 | PASS | PASS | PASS | PASS | PASS | PASS | Interviewer restriction (BR-08) is explicit. |
| US09 | PASS | PASS | PASS | PASS | PASS | PASS | File rules (BR-15) and duplicate rule (BR-06) are explicit. |
| US10 | PASS | PASS | PASS | PASS | PASS | PASS | CV replacement re-triggers analysis. |
| US11 | PASS | PASS | PASS | PASS | PASS | PASS | Transition table (BR-01..04) is explicit. |
| US12 | PASS | PASS | PASS | PASS | PASS | PASS | Largest story (8). Splits into: text extraction, redaction, AI call, quote verification, scoring. Uses a fake analyzer in tests. |
| US13 | PASS | PASS | PASS | PASS | PASS | PASS | Original result kept (BR-13). |
| US14 | PASS | PASS | PASS | PASS | PASS | PASS | Overlap rule for panel members is explicit. |
| US15 | PASS | PASS | PASS | PASS | PASS | PASS | Blind rule (BR-07) is enforced by the API and testable. |
| US16 | PASS | PASS | PASS | PASS | PASS | PASS | Atomic, bounded to 200 rows (BR-16). |
| US17 | PASS | PASS | PASS | PASS | PASS | PASS | Report metrics are listed. |
| US18 | PASS | PASS | PASS | PASS | PASS | PASS | Reuses US17 filters. |
| US19 | PASS | PASS | PASS | PASS | PASS | PASS | Read-only with filters. |
| US20 | PASS | PASS | PASS | PASS | PASS | PASS | Admin-only lifecycle; no self-deactivation. |
| US21 | PASS | PASS | PASS | PASS | PASS | PASS | Allowed fields and password verification are explicit. |

## Acceptance criteria

Format: **Given** context, **When** action, **Then** observable result.

### AC-US01 Login

1. Given an active account, when valid credentials are submitted, then the API returns 200 with an access token, expiry time, and a profile without the password hash, and the user lands on the Dashboard.
2. Given wrong credentials **or** a deactivated account, when submitted, then the API returns 401 with the same generic message in both cases.
3. Given an empty email or password, then the form shows a validation error and no request is sent.

### AC-US02 Logout

1. Given a logged-in user, when they log out, then a `LOGOUT` audit entry is written, the token and cached data are removed, and the Login screen is shown.
2. Given a logged-out client, when an internal URL is opened, then the user is redirected to Login.

### AC-US03 Dashboard

1. Given a date range, then KPIs show open positions, new applications, count per stage, interviews this week, and hires, using only data the role may see.
2. Given an Interviewer, then only "My upcoming interviews" and "Scorecards pending" are shown.
3. Given no data, then an empty state with a hint is shown instead of zeros without explanation.

### AC-US04 View positions

1. Given positions exist, when searching by title or filtering by status/department, then only matching positions are listed with paging (default 20, max 100).
2. Given an Interviewer, then only positions where they are on at least one interview panel are listed.

### AC-US05 Create position

1. Given valid data with at least one criterion, when saved, then the position is created with the creator as owner, version 1, and status `DRAFT` or `OPEN`.
2. Given no criteria, a weight outside 1–5, a past target hire date, or headcount < 1, then the API returns 400 with field errors.
3. Given an Interviewer, then the create action is hidden and the API returns 403.

### AC-US06 Edit position

1. Given a Recruiter who does not own the position, when editing, then the API returns 403.
2. Given criteria are changed, when saved, then the version increases and existing AI analyses are marked `OUTDATED`.

### AC-US07 Close / reopen position

1. Given an `OPEN` position, when an HR Manager closes it, then the status is `CLOSED`, an audit entry is written, and adding an application to it returns 409.
2. Given a Recruiter, then the close action is hidden and the API returns 403.

### AC-US08 View applications

1. Given a position, when the Kanban view is opened, then applications appear in stage columns with candidate name, fit score, and "Missing must-have" badge.
2. Given the table view, when filtering by stage, score range, source, or position and sorting by score, then the result matches the filters.
3. Given an Interviewer, then only assigned applications are returned and AI fields are absent from the API response (not just hidden).

### AC-US09 Add application with CV

1. Given an `OPEN` position and a valid PDF/DOCX ≤ 10 MB, when submitted, then an application in `NEW` is created and an AI analysis with status `PENDING` is queued.
2. Given an existing candidate with the same normalized email or phone, then the new application links to that candidate instead of creating a new one.
3. Given the candidate already has an application for the same position, then the API returns 409.
4. Given a wrong file type, a file > 10 MB, or no extractable text, then the API returns 400 (or 413 for size) and nothing is stored.
5. Given no source, a source outside the BR-18 list, or `OTHER` without a note, then the API returns 400 with a field error.

### AC-US10 Edit candidate information

1. Given valid changes, when saved, then the candidate is updated and an audit entry is written.
2. Given the CV is replaced, then a new AI analysis is queued and the previous one is kept as history.

### AC-US11 Move pipeline stage

1. Given an application in `SCREENING`, when moved to `INTERVIEW`, then the stage changes and a stage-history row (from, to, actor, time) and audit entry are written.
2. Given a skip (e.g. `NEW → INTERVIEW`) or a move out of `HIRED`/`REJECTED`, then the API returns 409 with the allowed next stages.
3. Given a Recruiter moving to `OFFER` or `HIRED`, then the API returns 403.
4. Given a move to `REJECTED` without a reason, then the API returns 400.

### AC-US12 Analyze CV with AI

1. Given a queued analysis, when the worker completes, then status is `COMPLETED` with skills, years of experience, education, languages, per-criterion result with quote, fit score 0–100, and model name.
2. Given the request sent to the AI service, then it contains no candidate name, email, phone, date of birth, or address (verified with a fake analyzer in tests).
3. Given an AI quote that does not appear in the CV text, then that criterion is stored as `UNVERIFIED` and counted as 0.
4. Given a must-have criterion not `MET`, then the application shows "Missing must-have" and its stage is unchanged.
5. Given the AI service fails or times out, then status is `FAILED` with a reason, and "Analyze again" queues a new attempt.

### AC-US13 Override AI result

1. Given a completed analysis, when a user changes a criterion result with a reason of at least 10 characters, then the score is recalculated and both AI and human results are shown.
2. Given a shorter reason, then the API returns 400.
3. Given an override, then an `AI_OVERRIDE` audit entry records the old value, new value, reason, and actor.

### AC-US14 Schedule interview

1. Given an application in `INTERVIEW`, when a future time and at least one panel member are saved, then the interview appears on each panel member's dashboard.
2. Given a panel member already has an overlapping interview, then the API returns 409 naming the conflict.
3. Given an application not in `INTERVIEW`, then the API returns 409.

### AC-US15 Submit scorecard

1. Given an assigned interviewer who has not submitted, when requesting the application's scorecards, then the API returns only their own draft, never others' content.
2. Given a scorecard with every criterion rated 1–5 and a recommendation, when submitted, then it becomes read-only and other submitted scorecards become visible to that interviewer.
3. Given a user who is not on the panel, then submitting returns 403.

### AC-US16 Bulk import

1. Given a template-compliant Excel file and a ZIP whose file names match the `cv_file` column, when previewed, then each row shows `VALID` or `INVALID` with reasons; an unknown `source` value makes the row `INVALID` (BR-18).
2. Given any invalid row, when confirming, then zero applications are created.
3. Given all rows valid, when confirming, then all applications are created in one transaction, an import batch is recorded, and an AI analysis is queued for each.
4. Given more than 200 rows or files above size limits, then the API returns 400/413 before processing.

### AC-US17 View reports

1. Given a date range and optional position, then the report shows stage counts and conversion rates, average time-to-hire, applications by source, rejection reasons, AI override rate, and AI–human agreement rate.
2. Given a Recruiter or Interviewer, then the Reports menu is hidden and the API returns 403.

### AC-US18 Export reports

1. Given the current report filters, when exporting PDF or XLSX, then the file contains the same figures as the screen and an `EXPORT` audit entry is written.
2. Given an unsupported format, then the API returns 400.

### AC-US19 View audit log

1. Given filters for action, module, actor, and date range, then matching entries are listed newest first with actor name.
2. Given any role other than Admin or HR Manager, then the API returns 403.

### AC-US20 Manage users

1. Given an Admin, when creating a user with a unique email and valid role, then the user can log in.
2. Given a duplicate email, then the API returns 409.
3. Given an Admin deactivating their own account, then the API returns 409.
4. Given a deactivated user, then login returns 401.

### AC-US21 Personal profile

1. Given valid name, phone, avatar URL, and timezone, when saved, then the profile is updated; the role field is read-only.
2. Given a wrong current password when changing password, then the API returns 400 and the password is unchanged.

## Cross-cutting acceptance constraints

- Only `ADMIN`, `HR_MANAGER`, `RECRUITER`, and `INTERVIEWER` exist.
- The API, not the React client, enforces RBAC, position ownership, panel assignment, and blind scorecards.
- AI output is advisory; no endpoint lets the AI change application stage.
- Every call to the external AI service uses redacted text and is logged with model, duration, and status.
- API errors use RFC 7807 `ProblemDetails` with a stable `errorCode` and `traceId`.
- File type and size are validated server-side before parsing or storage.

**Related:** [Research](01-research-top-down.md) · [Use cases](02-use-cases.md) · [Information architecture](04-information-architecture.md)
