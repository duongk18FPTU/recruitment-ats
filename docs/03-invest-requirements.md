# INVEST Requirements & Acceptance Criteria — Recruitment ATS

> Design status: TARGET

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
| US22 | As an Admin, HR Manager, or position owner, I want to edit, publish, and unpublish the job post of a position and copy source-tagged share links so that candidates can find it and I know which channel brought them. | Reach + channel tracking | US05 | M | 5 |
| US23 | As a candidate, I want to browse published job posts and read their details without an account so that I can find a suitable job. | Candidate reach | US22 | M | 3 |
| US24 | As a candidate, I want to apply online with my CV and give consent so that my application reaches the hiring team and I get a receipt. | Self-service intake | US23, US09, US27 | M | 8 |
| US25 | As an Admin, HR Manager, or Recruiter, I want CVs sent to the recruitment mailbox to appear in a CV inbox and become applications so that emailed CVs are not lost. | Centralized intake | US09, US29 | M | 8 |
| US26 | As an Admin, HR Manager, or Recruiter, I want to search consented past candidates and add them to a position so that I can reuse talent instead of sourcing from scratch. | Talent reuse | US09, US24 | M | 5 |
| US27 | As an Admin, HR Manager, or Recruiter, I want candidates to receive automatic emails at key events, with a 10-minute window to edit or cancel, so that candidates are informed without risky mistakes. | Candidate experience | US09, US11, US14 | M | 8 |
| US28 | As an Admin or HR Manager, I want to edit email templates with placeholders and a preview so that emails match the company's tone. | Consistent communication | US27 | S | 3 |
| US29 | As an Admin, I want to configure the recruitment mailbox and email sender and test the connection so that intake and sending work securely. | Operability | US01 | M | 3 |
| US30 | As an Admin, HR Manager, or Recruiter, I want to export the filtered candidate list or talent pool to Excel so that I can share or process it offline. | Portable data | US08, US26 | M | 3 |
| US31 | As a candidate, I want to join or leave the talent pool or delete my data from a link in any email so that I control my personal data. | Lawful processing | US24, US27 | M | 3 |

**Total:** 31 stories, 133 points. Must-have stories: 26 (115 points).

## Per-story INVEST review

`PASS` means the design provides enough information for planning and acceptance. It does not claim that implementation or tests exist.

| Story | I | N | V | E | S | T | Notes |
|---|---|---|---|---|---|---|---|
| US01 | PASS | PASS | PASS | PASS | PASS | PASS | Bearer token, 30-minute lifetime, no refresh token. |
| US02 | PASS | PASS | PASS | PASS | PASS | PASS | Client-side discard; no server-side revocation. |
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
| US22 | PASS | PASS | PASS | PASS | PASS | PASS | Public fields (BR-20) and publish rule (BR-05) are explicit; share links are plain URLs. |
| US23 | PASS | PASS | PASS | PASS | PASS | PASS | Read-only public endpoint; closed and expired states defined. |
| US24 | PASS | PASS | PASS | PASS | PASS | PASS | Largest public story (8). Splits into: form + validation, CAPTCHA/rate limit, consent record, intake reuse of US09. |
| US25 | PASS | PASS | PASS | PASS | PASS | PASS | 8 points. Splits into: mailbox reader, inbox list, assign flow, auto-assign by code. Uses a fake `IMailboxReader` in tests. |
| US26 | PASS | PASS | PASS | PASS | PASS | PASS | Pool membership depends only on consent (BR-24). |
| US27 | PASS | PASS | PASS | PASS | PASS | PASS | 8 points. Splits into: event → queue, scheduler, edit/cancel, retry. Uses a fake `IEmailSender`; delays are configurable for tests. |
| US28 | PASS | PASS | PASS | PASS | PASS | PASS | Placeholder list is fixed; consent link is mandatory. |
| US29 | PASS | PASS | PASS | PASS | PASS | PASS | Secrets are never returned by the API after saving. |
| US30 | PASS | PASS | PASS | PASS | PASS | PASS | Columns and row limit are explicit (BR-25). |
| US31 | PASS | PASS | PASS | PASS | PASS | PASS | Signed link replaces a login; deletion effects are listed in BR-24. |

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

1. Given an `OPEN` position, when an HR Manager closes it, then the status is `CLOSED`, an audit entry is written, its job post is unpublished, and adding an application to it returns 409.
2. Given a Recruiter, then the close action is hidden and the API returns 403.

### AC-US08 View applications

1. Given a position, when the Kanban view is opened, then applications appear in stage columns with candidate name, fit score, and "Missing must-have" badge.
2. Given the table view, when filtering by stage, score range, source, or position and sorting by score, then the result matches the filters.
3. Given an Interviewer, then only assigned applications are returned and AI fields are absent from the API response (not just hidden).

### AC-US09 Add application with CV

1. Given an `OPEN` position and a valid PDF/DOCX ≤ 10 MB, when submitted, then an application in `NEW` is created, an AI analysis with status `PENDING` is queued, and a receipt email is sent to the candidate.
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
5. Given a move to `REJECTED` or `OFFER`, then the matching email is created with status `PENDING` and a send time 10 minutes later.

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
4. Given a saved interview, then an invitation email is queued for 10 minutes; rescheduling or cancelling before it is sent replaces or cancels it instead of sending a second email.

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

### AC-US22 Manage job post

1. Given an `OPEN` position, when the owner publishes its job post, then the post appears on the career site with only public fields (BR-20) and a `PUBLISH` audit entry is written.
2. Given a `DRAFT` or `CLOSED` position, when publishing, then the API returns 409.
3. Given a published post, when copying share links, then each link points to the post and carries `src` for one source in the BR-18 list.
4. Given a Recruiter who does not own the position, then publish returns 403.

### AC-US23 Browse job posts

1. Given published posts, when an anonymous visitor opens the career site, then only `PUBLISHED` posts of `OPEN` positions are listed, with search and filters.
2. Given a post past its closing date, then it shows "Đã hết hạn nhận hồ sơ" and the apply button is disabled.
3. Given an unpublished or unknown post URL, then a "Tin không còn tồn tại" page is shown; the public API returns 404 without internal details.

### AC-US24 Apply online

1. Given a published post and valid input with privacy consent and CAPTCHA, when submitted, then an application in `NEW` is created with the `src` source (default `CAREER_SITE`), the consent version and time are stored, an AI analysis is queued, a receipt email is sent, and the thank-you page is shown.
2. Given privacy consent not ticked, then the API returns 400 and nothing is stored.
3. Given talent-pool consent ticked, then the candidate appears in the talent pool; unticked, they do not.
4. Given the same email already has an application for that position, then the thank-you page is shown and no new application or email is created.
5. Given more than 5 submissions from one IP within an hour, then the API returns 429.

### AC-US25 Process CV inbox

1. Given a new email with a PDF attachment and subject `[JOB-014] …` for an `OPEN` position, when the reader runs, then an application with source `EMAIL` is created automatically and a receipt email is sent.
2. Given an email with a CV but no valid position code, then a `NEEDS_REVIEW` inbox item appears; when a recruiter assigns a position and confirms the candidate, then an application is created.
3. Given an email without a PDF/DOCX attachment, then no inbox item is created.
4. Given the same message is read twice, then only one inbox item exists (Message-ID).
5. Given a dismissed item, then it and its attachments are deleted after 7 days.

### AC-US26 Use talent pool

1. Given candidates with and without active pool consent, when searching the pool, then only consented candidates are returned.
2. Given a pool candidate and an `OPEN` position, when added, then an application in `NEW` is created, AI analysis is queued, and a receipt email is sent; adding to a position they already applied to returns 409.

### AC-US27 Automatic candidate emails

1. Given an application is created from any channel, then the receipt email is sent immediately and logged as `SENT`.
2. Given an interview is scheduled or an application moves to `REJECTED` or `OFFER`, then an email is logged as `PENDING` with send time +10 minutes and is visible on the application's *Email* tab.
3. Given a `PENDING` email, when a recruiter cancels it, then it becomes `CANCELLED` and is never sent; when edited, then the edited content is sent.
4. Given the email service fails, then the email is retried 3 times and then marked `FAILED` and listed under *Cần xử lý*.
5. Given a candidate without a valid email, then no email is queued and the application shows a warning.
6. Given any sent email, then it contains the candidate's signed consent link.

### AC-US28 Manage email templates

1. Given a template with known placeholders, when saved, then the next emails for that event use it; preview renders sample data.
2. Given an unknown placeholder or a body without `{{link_dong_y}}`, then the API returns 400.
3. Given a Recruiter or Interviewer, then the API returns 403.

### AC-US29 Configure mailbox and sender

1. Given valid IMAP and SMTP settings, when *Test connection* is run, then both connections report success or a specific error.
2. Given saved settings, when they are read back, then passwords are not returned.
3. Given a non-Admin, then the API returns 403.

### AC-US30 Export candidates

1. Given current filters on the candidate table or talent pool, when exporting, then the XLSX contains exactly the visible rows (maximum 5,000) and the listed columns, and an `EXPORT` audit entry stores the filters.
2. Given more than 5,000 matching rows, then the API returns 400 asking to narrow the filters.
3. Given an Interviewer, then the API returns 403.

### AC-US31 Manage data consent

1. Given a valid signed link, when the candidate leaves the pool, then they no longer appear in the talent pool and receive a confirmation email.
2. Given a valid signed link, when the candidate deletes all data, then CVs are deleted, the candidate is anonymized, open applications become `REJECTED` with reason "Ứng viên rút hồ sơ", and an audit entry is written.
3. Given an invalid or expired link, then the page explains that a new link arrives with the next email or can be requested by replying to the recruitment mailbox.

## Cross-cutting acceptance constraints

- Only `ADMIN`, `HR_MANAGER`, `RECRUITER`, and `INTERVIEWER` have accounts. Candidates use public endpoints without an account; these expose only published job posts, the apply action, and the signed consent action.
- The API, not the React client, enforces RBAC, position ownership, panel assignment, and blind scorecards.
- AI output is advisory; no endpoint lets the AI change application stage.
- Every call to the external AI service uses redacted text and is logged with model, duration, and status.
- API errors use RFC 7807 `ProblemDetails` with a stable `errorCode` and `traceId`.
- File type and size are validated server-side before parsing or storage.
- External services (AI, email sender, mailbox reader) sit behind interfaces with fakes, so every story is testable without real accounts.
- Personal data is processed only with a recorded consent or basis (BR-19); deletions follow BR-17 and BR-24.

**Related:** [Research](01-research-top-down.md) · [Use cases](02-use-cases.md) · [Information architecture](04-information-architecture.md)
