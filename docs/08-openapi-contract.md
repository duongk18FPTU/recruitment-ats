# OpenAPI 3.0 Contract

> Design status: TARGET
>
> Contract: [`api/openapi.yaml`](api/openapi.yaml) (OpenAPI 3.0.3, 85 operations, passes `redocly lint` with the recommended rules). Preview it with `npx @redocly/cli preview-docs docs/api/openapi.yaml` or paste it into [Swagger Editor](https://editor.swagger.io).

The contract is the agreement between the two SPAs and the API. Operation IDs are final and replace the provisional names in the [screen-to-API mapping](04-information-architecture.md#screen-to-api-mapping-target); generated TypeScript clients use them as function names.

## Conventions

| Topic | Rule |
|---|---|
| Base paths | Internal: `https://ats.<domain>/api/*`. Public: `https://careers.<domain>/api/public/*`. Each domain exposes only its own group (AD-05) |
| Format | JSON with `camelCase` properties; enums in `UPPER_SNAKE_CASE` as in the database; dates `YYYY-MM-DD`; timestamps ISO 8601 in UTC |
| Resources and actions | Nouns for resources (`/positions/{id}`); state changes that carry business rules are explicit actions (`/stage`, `/publish`, `/cancel`, `/submit`) rather than generic field updates |
| Authentication | `Authorization: Bearer <access token>` (JWT, 15 minutes). The refresh token lives in the `ats_refresh` cookie (`httpOnly`, `Secure`, `SameSite=Strict`, path `/api/auth`) and is rotated on every refresh (AD-06) |
| Authorization | `x-roles` on each operation lists the roles; descriptions state record-level rules (ownership BR-14, panel assignment BR-08, blind scorecards BR-07). A role that is not listed gets 403; a record the caller may not see gets 404 when its existence must not leak (public API, interviewer access) |
| Field-level hiding | For `INTERVIEWER`, AI fields (`fitScore`, `missingMustHave`, `analysisStatus`) are **absent** from responses, not null (AC-US08) |
| Paging | `page` (from 1) and `pageSize` (default 20, max 100); responses carry `items`, `page`, `pageSize`, `totalItems`, `totalPages` |
| Sorting and filters | `sort=-fitScore` (minus = descending); multi-value filters as comma-separated lists (`stage=NEW,SCREENING`) |
| Concurrency | Mutable resources return `rowVersion`; updates send it back. A stale value → 409 `CONCURRENCY_CONFLICT` |
| Files | Uploads are `multipart/form-data`; the API checks type and size before storing (BR-15). Downloads stream with `Content-Disposition` and `Cache-Control: private, no-store` |
| Asynchronous work | Actions that queue work return immediately: `requestAnalysis` → 202 with `PENDING`; the client polls `getAnalysis` every 5 s. Actions that queue an email return the pending email so the UI can offer "Xem email" |
| Errors | `application/problem+json` (RFC 7807) with `errorCode` and `traceId`; validation errors add `errors` keyed by field path. `detail` is a Vietnamese message safe to display |
| Rate limits | Public apply: 5 per IP per hour, 3 per email per day (BR-21); login: 10 per IP per minute. Exceeding → 429 with `Retry-After` |
| Versioning | Additive changes only within v1; a breaking change would add `/api/v2` |

## Status codes

| Code | When |
|---|---|
| 200 / 201 / 202 / 204 | Read or update / created (with `Location`) / queued / no body |
| 400 | Validation failed or a required business input is missing (e.g. rejection reason, override reason shorter than 10 characters, export over 5,000 rows) |
| 401 | Missing or expired token; wrong credentials (always the same message) |
| 403 | Role or record-level rule denies the action |
| 404 | Not found, or not visible to the caller |
| 409 | Business rule conflict: duplicate, invalid transition, overlap, already submitted, email no longer pending, stale `rowVersion` |
| 413 | File too large |
| 429 | Rate limit reached |

## Error codes

| `errorCode` | Status | Raised by |
|---|---|---|
| `VALIDATION_FAILED` | 400 | Any request with invalid fields |
| `INVALID_CREDENTIALS` | 401 | `login` |
| `UNAUTHENTICATED` | 401 | Missing or expired token, revoked refresh token |
| `FORBIDDEN` | 403 | Any protected operation |
| `NOT_FOUND` | 404 | Any operation with an id or slug |
| `CONCURRENCY_CONFLICT` | 409 | Updates with `rowVersion` |
| `DUPLICATE_EMAIL` | 409 | `createUser`, `updateUser` |
| `DUPLICATE_APPLICATION` | 409 | `createApplication`, `assignInboxItem`, `addCandidateToPosition` (with `existingApplicationId`) |
| `POSITION_NOT_OPEN` | 409 | Intake into a non-`OPEN` position, `publishJobPost` |
| `INVALID_POSITION_STATUS_CHANGE` | 409 | `updatePositionStatus` |
| `INVALID_STAGE_TRANSITION` | 409 | `moveApplicationStage` (with `allowedStages`) |
| `ANALYSIS_IN_PROGRESS` | 409 | `requestAnalysis` |
| `ANALYSIS_NOT_COMPLETED` | 409 | `overrideCriterionResult` |
| `APPLICATION_NOT_IN_INTERVIEW` | 409 | `createInterview` |
| `INTERVIEW_OVERLAP` | 409 | `createInterview`, `updateInterview` (with `conflicts`) |
| `INTERVIEW_CANCELLED` | 409 | Changing or scoring a cancelled interview |
| `SCORECARD_ALREADY_SUBMITTED` | 409 | `saveScorecardDraft`, `submitScorecard` |
| `EMAIL_NOT_PENDING` / `EMAIL_NOT_FAILED` | 409 | `updatePendingEmail`, `cancelPendingEmail` / `resendEmail` |
| `INBOX_ITEM_PROCESSED` | 409 | `assignInboxItem`, `dismissInboxItem` on a processed item |
| `IMPORT_INVALID_ROWS` / `IMPORT_EXPIRED` | 409 | `confirmImport` |
| `EXPORT_TOO_LARGE` | 400 | `exportApplications`, `exportTalentPool` |
| `UNSUPPORTED_FILE_TYPE` / `NO_EXTRACTABLE_TEXT` | 400 | CV uploads |
| `FILE_TOO_LARGE` | 413 | CV and import uploads |
| `SELF_DEACTIVATION` | 409 | `updateUserStatus` |
| `JOB_POST_EXPIRED` | 409 | `submitPublicApplication` after the closing date |
| `CAPTCHA_FAILED` | 400 | `submitPublicApplication` |
| `RATE_LIMITED` | 429 | Public endpoints, `login` |
| `CONSENT_TOKEN_INVALID` | 404 | `getConsent`, `updateConsent` |
| `INTERNAL_ERROR` | 500 | Unexpected errors (details only in logs, linked by `traceId`) |

Example:

```json
{
  "type": "https://ats.example.vn/errors/invalid-stage-transition",
  "title": "Invalid stage transition",
  "status": 409,
  "detail": "Không thể chuyển từ NEW sang INTERVIEW",
  "errorCode": "INVALID_STAGE_TRANSITION",
  "allowedStages": ["SCREENING", "REJECTED"],
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

## Endpoints

Roles: **A** Admin · **H** HR Manager · **R** Recruiter · **I** Interviewer. Record-level rules are in each operation's description in the YAML.

### Auth

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `POST` | `/api/auth/login` | `login` | Public | Log in (US01) |
| `POST` | `/api/auth/refresh` | `refreshToken` | Refresh cookie | Rotate the refresh token and issue a new access token |
| `POST` | `/api/auth/logout` | `logout` | Any signed-in | Log out (US02) |

### Profile

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/me` | `getProfile` | Any signed-in | Get my profile (US21) |
| `PUT` | `/api/me` | `updateProfile` | Any signed-in | Update my profile (US21) |
| `PUT` | `/api/me/password` | `changePassword` | Any signed-in | Change my password (US21) |

### Dashboard

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/dashboard` | `getDashboard` | A H R I | Dashboard for a date range (US03) |

### Lookups

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/lookups/departments` | `listDepartments` | A H R I | Active departments for forms and filters |
| `GET` | `/api/lookups/users` | `listUserOptions` | A H R | Active users for pickers (panel members, position owner filter) |

### Positions

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/positions` | `listPositions` | A H R I | Search positions (US04) |
| `POST` | `/api/positions` | `createPosition` | A H R | Create a position with criteria (US05) |
| `GET` | `/api/positions/{positionId}` | `getPosition` | A H R I | Position detail with current criteria (US04) |
| `PUT` | `/api/positions/{positionId}` | `updatePosition` | A H R | Edit a position (US06) |
| `PUT` | `/api/positions/{positionId}/status` | `updatePositionStatus` | A H R | Open, close, or reopen a position (US07) |

### Applications

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/positions/{positionId}/pipeline` | `getPipeline` | A H R I | Kanban board of one position (US08) |
| `GET` | `/api/applications` | `listApplications` | A H R I | Applications table across positions (US08) |
| `POST` | `/api/applications` | `createApplication` | A H R | Add an application with a CV (US09) |
| `GET` | `/api/applications/export` | `exportApplications` | A H R | Export the filtered candidate table to Excel (US30, BR-25) |
| `GET` | `/api/applications/{applicationId}` | `getApplication` | A H R I | Application detail (US08) |
| `PUT` | `/api/applications/{applicationId}` | `updateApplication` | A H R | Edit candidate information and source (US10) |
| `POST` | `/api/applications/{applicationId}/cv` | `replaceCv` | A H R | Replace the CV (US10) |
| `POST` | `/api/applications/{applicationId}/stage` | `moveApplicationStage` | A H R | Move to the next stage or reject (US11) |
| `GET` | `/api/applications/{applicationId}/stage-history` | `getStageHistory` | A H R | Stage history (BR-04) |
| `GET` | `/api/cv-files/{cvFileId}/content` | `getCvFileContent` | A H R I | Stream a CV file for preview or download |

### Job posts

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/positions/{positionId}/job-post` | `getJobPost` | A H R | Job post of a position (US22) |
| `PUT` | `/api/positions/{positionId}/job-post` | `updateJobPost` | A H R | Edit public content (US22, BR-20) |
| `POST` | `/api/positions/{positionId}/job-post/publish` | `publishJobPost` | A H R | Publish the job post (US22) |
| `POST` | `/api/positions/{positionId}/job-post/unpublish` | `unpublishJobPost` | A H R | Remove the job post from the career site (US22) |
| `GET` | `/api/positions/{positionId}/job-post/share-links` | `getShareLinks` | A H R | Source-tagged links to the job post (US22, BR-18) |

### Candidates

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/candidates/match` | `matchCandidate` | A H R | Find an existing candidate by email or phone before adding (BR-06) |
| `GET` | `/api/candidates/{candidateId}` | `getCandidate` | A H R | Candidate with CVs, applications, and consent state (US26) |

### AI screening

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/applications/{applicationId}/analyses` | `listAnalyses` | A H R | Analysis history, newest first (US12) |
| `POST` | `/api/applications/{applicationId}/analyses` | `requestAnalysis` | A H R | Analyze again (US12) |
| `GET` | `/api/applications/{applicationId}/analysis` | `getAnalysis` | A H R | Current analysis (polled every 5 s while pending) |
| `PUT` | `/api/analyses/{analysisId}/criteria/{criterionId}/override` | `overrideCriterionResult` | A H R | Override one criterion result (US13, BR-13) |

### Interviews

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/interviews` | `listInterviews` | A H R I | Interviews in a period (US03, US15) |
| `POST` | `/api/applications/{applicationId}/interviews` | `createInterview` | A H R | Schedule an interview (US14) |
| `GET` | `/api/interviews/{interviewId}` | `getInterview` | A H R I | Interview detail |
| `PUT` | `/api/interviews/{interviewId}` | `updateInterview` | A H R | Reschedule or change the panel (US14) |
| `POST` | `/api/interviews/{interviewId}/cancel` | `cancelInterview` | A H R | Cancel an interview (US14) |
| `GET` | `/api/interviews/{interviewId}/scorecard` | `getMyScorecard` | H I | My scorecard for this interview (draft or submitted) |
| `PUT` | `/api/interviews/{interviewId}/scorecard` | `saveScorecardDraft` | H I | Save my scorecard as a draft |
| `POST` | `/api/interviews/{interviewId}/scorecard/submit` | `submitScorecard` | H I | Submit my scorecard (US15) |
| `GET` | `/api/applications/{applicationId}/scorecards` | `getScorecards` | A H R I | Scorecards of an application (BR-07) |

### Emails

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/emails` | `listEmails` | A H R | Candidate emails across applications (e.g. failed ones for "Cần xử lý") |
| `GET` | `/api/applications/{applicationId}/emails` | `listApplicationEmails` | A H R | Email history of an application (US27) |
| `PUT` | `/api/emails/{emailId}` | `updatePendingEmail` | A H R | Edit a pending email (US27) |
| `POST` | `/api/emails/{emailId}/cancel` | `cancelPendingEmail` | A H R | Cancel a pending email (US27) |
| `POST` | `/api/emails/{emailId}/resend` | `resendEmail` | A H R | Send a failed email again |

### CV inbox

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/inbox-items` | `listInboxItems` | A H R | CV inbox (US25) |
| `GET` | `/api/inbox-items/{inboxItemId}` | `getInboxItem` | A H R | Inbox item with attachments and prefilled candidate data |
| `GET` | `/api/inbox-items/{inboxItemId}/attachments/{attachmentId}/content` | `getInboxAttachmentContent` | A H R | Stream an attachment for preview |
| `POST` | `/api/inbox-items/{inboxItemId}/assign` | `assignInboxItem` | A H R | Turn an inbox item into an application (US25) |
| `POST` | `/api/inbox-items/{inboxItemId}/dismiss` | `dismissInboxItem` | A H R | Dismiss an item (deleted after 7 days, BR-22) |

### Talent pool

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/talent-pool` | `searchTalentPool` | A H R | Search consented candidates (US26, BR-24) |
| `GET` | `/api/talent-pool/export` | `exportTalentPool` | A H R | Export the filtered talent pool to Excel (US30, BR-25) |
| `POST` | `/api/talent-pool/{candidateId}/applications` | `addCandidateToPosition` | A H R | Add a pool candidate to an open position (US26) |

### Imports

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/imports/template` | `downloadImportTemplate` | A H R | Excel template for bulk import (US16) |
| `POST` | `/api/imports/preview` | `previewImport` | A H R | Upload Excel + ZIP and validate every row (US16) |
| `GET` | `/api/imports` | `listImports` | A H R | Import history |
| `GET` | `/api/imports/{batchId}` | `getImport` | A H R | Import batch with row results |
| `POST` | `/api/imports/{batchId}/confirm` | `confirmImport` | A H R | Create all applications atomically (US16, BR-16) |

### Reports

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/reports` | `getReport` | A H | Recruitment report (US17) |
| `GET` | `/api/reports/export` | `exportReport` | A H | Export the report to PDF or XLSX (US18) |

### Audit

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/audit-logs` | `listAuditLogs` | A H | Audit log, newest first (US19) |

### Users

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/users` | `listUsers` | A | Users (US20) |
| `POST` | `/api/users` | `createUser` | A | Create a user with a temporary password (US20) |
| `GET` | `/api/users/{userId}` | `getUser` | A | User detail |
| `PUT` | `/api/users/{userId}` | `updateUser` | A | Edit a user (US20) |
| `PUT` | `/api/users/{userId}/status` | `updateUserStatus` | A | Activate or deactivate a user (US20) |

### Settings

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/settings/email-templates` | `listEmailTemplates` | A H | Email templates (US28) |
| `PUT` | `/api/settings/email-templates/{event}` | `updateEmailTemplate` | A H | Edit a template (US28) |
| `POST` | `/api/settings/email-templates/{event}/preview` | `previewEmailTemplate` | A H | Render unsaved template content with sample data |
| `POST` | `/api/settings/email-templates/{event}/test` | `sendTestEmail` | A H | Send the rendered template to the caller's own address |
| `GET` | `/api/settings/mail` | `getMailSettings` | A | Mailbox and sender settings without passwords (US29) |
| `PUT` | `/api/settings/mail` | `updateMailSettings` | A | Save mailbox and sender settings (US29) |
| `POST` | `/api/settings/mail/test` | `testMailSettings` | A | Test IMAP and SMTP connections (US29) |

### Public career site

| Method | Path | Operation | Roles | Summary |
|---|---|---|---|---|
| `GET` | `/api/public/job-posts` | `listPublicJobPosts` | Public | Published job posts of open positions (US23) |
| `GET` | `/api/public/job-posts/filters` | `getPublicJobPostFilters` | Public | Filter options with counts (departments, locations, types, levels) |
| `GET` | `/api/public/job-posts/{slug}` | `getPublicJobPost` | Public | Job post detail (US23) |
| `POST` | `/api/public/job-posts/{slug}/applications` | `submitPublicApplication` | Public | Apply online (US24) |
| `GET` | `/api/public/privacy-policy` | `getPrivacyPolicy` | Public | Current privacy policy and its version (BR-19) |
| `GET` | `/api/public/consent/{token}` | `getConsent` | Public | What data is held and the pool status (US31) |
| `POST` | `/api/public/consent/{token}` | `updateConsent` | Public | Join or leave the talent pool, or delete all data (US31, BR-24) |
| `GET` | `/api/public/share/jobs/{slug}` | `getJobPostSharePage` | Public | Open Graph HTML for link-preview crawlers (AD-04) |

## Requirement coverage

| Story | Operations |
|---|---|
| US01–02 | `login`, `refreshToken`, `logout` |
| US03 | `getDashboard` |
| US04–07 | `listPositions`, `getPosition`, `createPosition`, `updatePosition`, `updatePositionStatus` |
| US08 | `listApplications`, `getPipeline`, `getApplication`, `getCvFileContent` |
| US09 | `matchCandidate`, `createApplication` |
| US10 | `updateApplication`, `replaceCv` |
| US11 | `moveApplicationStage`, `getStageHistory` |
| US12–13 | `getAnalysis`, `listAnalyses`, `requestAnalysis`, `overrideCriterionResult` |
| US14–15 | `createInterview`, `updateInterview`, `cancelInterview`, `listInterviews`, `getInterview`, `getMyScorecard`, `saveScorecardDraft`, `submitScorecard`, `getScorecards` |
| US16 | `downloadImportTemplate`, `previewImport`, `confirmImport`, `listImports`, `getImport` |
| US17–18 | `getReport`, `exportReport` |
| US19 | `listAuditLogs` |
| US20 | `listUsers`, `getUser`, `createUser`, `updateUser`, `updateUserStatus` |
| US21 | `getProfile`, `updateProfile`, `changePassword` |
| US22 | `getJobPost`, `updateJobPost`, `publishJobPost`, `unpublishJobPost`, `getShareLinks`, `getJobPostSharePage` |
| US23 | `listPublicJobPosts`, `getPublicJobPostFilters`, `getPublicJobPost` |
| US24 | `getPrivacyPolicy`, `submitPublicApplication` |
| US25 | `listInboxItems`, `getInboxItem`, `getInboxAttachmentContent`, `assignInboxItem`, `dismissInboxItem` |
| US26 | `searchTalentPool`, `getCandidate`, `addCandidateToPosition` |
| US27 | `listApplicationEmails`, `listEmails`, `updatePendingEmail`, `cancelPendingEmail`, `resendEmail` |
| US28 | `listEmailTemplates`, `updateEmailTemplate`, `previewEmailTemplate`, `sendTestEmail` |
| US29 | `getMailSettings`, `updateMailSettings`, `testMailSettings` |
| US30 | `exportApplications`, `exportTalentPool` |
| US31 | `getConsent`, `updateConsent` |
| Supporting forms and filters | `listDepartments`, `listUserOptions` |

Work done by the Worker (AI analysis, sending emails, mailbox polling, retention) has no endpoint; its results appear through the operations above.

**Related:** [Database design](07-database-design.md) · [C4 architecture](06-c4-architecture.md) · [Information architecture](04-information-architecture.md)
