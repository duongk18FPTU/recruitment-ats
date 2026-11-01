# Sequence and State Diagrams

> Design status: TARGET
>
> Drawn with the team skill **ve-diagram-srs** (`srsdiagram.py`, unchanged, SHA-256 prefix `25248a6d0b0f8b67`). Source: [`diagrams/ve_ats.py`](diagrams/ve_ats.py). Each diagram exists as PNG (shown here), SVG, and editable draw.io file in [`diagrams/out/`](diagrams/out/).

Regenerate after editing the declarations (Python 3, Pillow, Playwright with Chromium, Arial or Liberation Sans):

```bash
cd docs/diagrams
python ve_ats.py                    # all diagrams
python ve_ats.py seq_login st_application   # selected ones
```

Lifelines use the class names of the [class design](10-class-design.md), so every message can be found in code. Diagram text is English; screen names, buttons, and messages shown to users stay in Vietnamese as on the UI.

## Notation

| Symbol | Meaning |
|---|---|
| User, Candidate, AI Service, SMTP, Mailbox, Cloudflare Turnstile | Actor: a person or an external system |
| `:Login page`, `:Career site`, … | Boundary: the screen the user works on |
| `:…Controller`, `:…Service`, `:…Runner` | Control: API controller, Application service, or Worker job |
| `:Database`, `:CV file store` | Entity: PostgreSQL and the CV file volume |
| Arrows | Solid with filled head: call. Dashed with open head: return. A bar on a lifeline: the object is working |
| `alt`, `opt`, `loop`, `break` | `alt` takes one branch by the condition in [ ]; `opt` runs only when the condition holds; `loop` repeats; `break` runs its content and ends the flow |
| `ref` | Reuses another diagram, drawn in its own section (here: [Intake](#seq-intake)) |
| API paths | `…` replaces the common prefix: `/api/` for internal calls (e.g. `…/applications` = `/api/applications`), `/api/public/job-posts` on the career site, `/api/positions/{id}` for job-post calls |
| Writes to `:Database` | Several writes in one flow are one transaction (`IUnitOfWork`) unless the text says otherwise |

## Sequence diagrams

| Diagram | Flow | Stories / rules |
|---|---|---|
| [seq_login](#seq-login) | Login with lockout and generic error | US01 |
| [seq_refresh_token](#seq-refresh-token) | Refresh token rotation and reuse detection | US01, AD-06 |
| [seq_intake](#seq-intake) | Shared intake used by every channel | BR-05, BR-06, BR-15, BR-19 |
| [seq_add_application](#seq-add-application) | Recruiter adds an application with a CV | US09 |
| [seq_apply_online](#seq-apply-online) | Candidate applies on the career site | US24, BR-21 |
| [seq_analyze_cv](#seq-analyze-cv) | Worker analyzes a CV with the AI service | US12, BR-09–12 |
| [seq_override_ai](#seq-override-ai) | Recruiter overrides one criterion result | US13, BR-13 |
| [seq_move_stage](#seq-move-stage) | Reject an application, queue the email | US11, BR-01–04, BR-23 |
| [seq_email_dispatch](#seq-email-dispatch) | Cancel a pending email; Worker sends due emails | US27, BR-23 |
| [seq_schedule_interview](#seq-schedule-interview) | Schedule an interview, prevent double booking | US14 |
| [seq_submit_scorecard](#seq-submit-scorecard) | Blind scorecard submission | US15, BR-07 |
| [seq_mailbox_intake](#seq-mailbox-intake) | Worker collects CVs from the mailbox | US25, BR-22 |
| [seq_bulk_import](#seq-bulk-import) | Preview and atomic import | US16, BR-16 |
| [seq_delete_my_data](#seq-delete-my-data) | Candidate deletes all data from an email link | US31, BR-24 |
| [seq_publish_job_post](#seq-publish-job-post) | Publish a job post, copy a share link | US22, BR-05 |
| [seq_link_preview](#seq-link-preview) | LinkedIn crawler gets the Open Graph page | US22, AD-04 |

<a id="seq-login"></a>
### Login

![Login](diagrams/out/seq_login.png)

Unknown email, inactive account, locked account, and wrong password all return the same `401 INVALID_CREDENTIALS` (AC-US01). Five failures lock the account for 15 minutes; the API still answers with the same message.

<a id="seq-refresh-token"></a>
### Refresh access token

![Refresh access token](diagrams/out/seq_refresh_token.png)

The internal app keeps the access token in memory; the refresh token is an `httpOnly` cookie. Presenting a token that was already rotated means it was copied, so the whole token family is revoked and the user must log in again.

<a id="seq-intake"></a>
### Intake (shared)

![Intake](diagrams/out/seq_intake.png)

`:Caller` is any intake channel: `ApplicationsController` (manual), `PublicApplyService` (career site), `MailboxIntakeService` and `InboxService` (mailbox), `ImportService` (bulk), `TalentPoolService` (pool). Messages 22–24 are one transaction: the application, its first stage-history entry, the consent record, the `PENDING` analysis, the receipt email, and the audit entry are saved together or not at all. A file stored before a failure is deleted, as in the *already applied* branch.

<a id="seq-add-application"></a>
### Add application with CV

![Add application with CV](diagrams/out/seq_add_application.png)

`:API` stands for `CandidatesController` (match) and `ApplicationsController` (create). The match call runs while the recruiter types, so a duplicate is shown before the upload (BR-06).

<a id="seq-apply-online"></a>
### Apply online

![Apply online](diagrams/out/seq_apply_online.png)

Checks run cheapest first: per-IP limit in middleware, Turnstile, per-email daily limit and closing date, then the file inside Intake. A repeat application returns the same `202` as a new one and creates nothing (BR-21), so the form never reveals whether an email is known.

<a id="seq-analyze-cv"></a>
### Analyze CV with AI

![Analyze CV with AI](diagrams/out/seq_analyze_cv.png)

Only redacted text leaves the system (BR-10). The score and the *Missing must-have* flag are computed by code (BR-12); the application's stage is never touched (BR-09). The internal app polls `getAnalysis` every 5 s while the status is `PENDING` or `PROCESSING`. A Worker crash leaves the job `PROCESSING` with an expired lease, and the next claim picks it up again.

<a id="seq-override-ai"></a>
### Override AI result

![Override AI result](diagrams/out/seq_override_ai.png)

The AI result is kept next to the human result; the audit entry stores old value, new value, reason, and actor (AC-US13).

<a id="seq-move-stage"></a>
### Reject an application

![Reject an application](diagrams/out/seq_move_stage.png)

Moving to `OFFER` follows the same flow with the `OFFER` email and requires HR Manager or Admin (BR-03); other forward moves have no email. The stage change, history, pending email, and audit entry are one transaction, so the email exists only if the move was saved.

<a id="seq-email-dispatch"></a>
### Cancel and send candidate emails

![Cancel and send candidate emails](diagrams/out/seq_email_dispatch.png)

Cancel (`UPDATE … WHERE status = 'PENDING'`) and claim (`PENDING → SENDING`) touch the same row, so exactly one of them wins; the recruiter is told when the email has already gone. Editing a pending email (`updatePendingEmail`) uses the same condition.

<a id="seq-schedule-interview"></a>
### Schedule interview

![Schedule interview](diagrams/out/seq_schedule_interview.png)

The overlap query gives a readable error; the exclusion constraint `ex_panelist_overlap` catches two requests that pass the query at the same time. Rescheduling replaces a still-pending invitation instead of sending a second email.

<a id="seq-submit-scorecard"></a>
### Submit blind scorecard

![Submit blind scorecard](diagrams/out/seq_submit_scorecard.png)

Before submitting, `getScorecards` returns only the interviewer's own draft with `othersHidden = true` (BR-07). The AI score is absent from every response to an Interviewer (BR-08).

<a id="seq-mailbox-intake"></a>
### Collect CVs from the mailbox

![Collect CVs from the mailbox](diagrams/out/seq_mailbox_intake.png)

The mailbox is opened read-only and messages are recognized by Message-ID, so a message is never processed twice (BR-22). Items left in `NEEDS_REVIEW` are assigned later by a recruiter through `assignInboxItem`, which runs the same [Intake](#seq-intake).

<a id="seq-bulk-import"></a>
### Bulk import

![Bulk import](diagrams/out/seq_bulk_import.png)

The `loop` with `ref Intake` runs inside one transaction: one failing row rolls back every row (BR-16). Previews expire after 24 hours.

<a id="seq-delete-my-data"></a>
### Delete my data

![Delete my data](diagrams/out/seq_delete_my_data.png)

Files are deleted after the database commit; a file whose deletion fails stays marked `deleted_at` and is removed by the next `RetentionJob` run. No confirmation email is sent after full deletion, because the address no longer exists in the system.

<a id="seq-publish-job-post"></a>
### Publish job post and share

![Publish job post and share](diagrams/out/seq_publish_job_post.png)

Share links are built from the slug and the source list (BR-18), e.g. `https://careers.example.vn/jobs/backend-dotnet?src=linkedin`.

<a id="seq-link-preview"></a>
### Link preview for a shared job post

![Link preview for a shared job post](diagrams/out/seq_link_preview.png)

Only crawler user agents are routed to the API; browsers on the same URL receive the career-site SPA (AD-04). The page contains public fields only (BR-20).

## State diagrams

<a id="st-application"></a>
### Application stage

![Application stage](diagrams/out/st_application.png)

*In progress* is a composite state: the reject transition leaves from any of its stages (BR-01, BR-02). It is drawn with the skill's basic shapes because the module has no composite state.

| State | Meaning |
|---|---|
| NEW | Created by any intake channel; AI analysis queued |
| SCREENING | Recruiter is reviewing the CV and AI result |
| INTERVIEW | Interviews can be scheduled |
| OFFER | Offer decided by HR Manager or Admin; offer email queued |
| HIRED | Final; candidate hired |
| REJECTED | Final; reason recorded; rejection email queued (none after data deletion) |

<a id="st-cv-analysis"></a>
### CV analysis status

![CV analysis status](diagrams/out/st_cv_analysis.png)

| State | Meaning |
|---|---|
| PENDING | Waiting in the queue |
| PROCESSING | Claimed by the Worker with a 2-minute lease |
| COMPLETED | Score, criterion results, and quotes saved; may be overridden by a person |
| FAILED | AI error after the retry; "Phân tích lại" creates a new analysis |
| OUTDATED | Criteria changed after this analysis; still readable |

<a id="st-email-message"></a>
### Candidate email status

![Candidate email status](diagrams/out/st_email_message.png)

| State | Meaning |
|---|---|
| PENDING | Waiting for its send time (immediately for receipts, 10 minutes for the rest) or for a retry; can be edited or cancelled |
| SENDING | Claimed by the dispatcher; can no longer be cancelled |
| SENT | Accepted by SMTP |
| CANCELLED | Cancelled by a user or because the triggering action was undone |
| FAILED | Rejected four times; listed under *Cần xử lý* |

<a id="st-position"></a>
### Position status

![Position status](diagrams/out/st_position.png)

| State | Meaning |
|---|---|
| DRAFT | Being prepared; no applications |
| OPEN | Accepts applications; its job post can be published |
| CLOSED | No new applications; job post unpublished (BR-05) |

<a id="st-job-post"></a>
### Job post status

![Job post status](diagrams/out/st_job_post.png)

| State | Meaning |
|---|---|
| DRAFT | Created with the position, not public |
| PUBLISHED | Visible on the career site; after the closing date it stays visible as "Đã hết hạn nhận hồ sơ" |
| UNPUBLISHED | Removed from the career site; reopening a position does not republish it |

<a id="st-inbox-item"></a>
### CV inbox item status

![CV inbox item status](diagrams/out/st_inbox_item.png)

| State | Meaning |
|---|---|
| NEEDS_REVIEW | Waiting for a recruiter to assign a position or dismiss |
| CONVERTED | Became an application |
| DUPLICATE | Candidate had already applied to that position; linked to the existing application |
| DISMISSED | Not a CV; item and attachments deleted after 7 days (BR-22) |

**Related:** [Class design](10-class-design.md) · [Database design](07-database-design.md) · [OpenAPI contract](08-openapi-contract.md) · [Use cases](02-use-cases.md)
