"""Sequence and state diagrams for Recruitment ATS.

Drawn with srsdiagram.py from the ve-diagram-srs skill (module unchanged, SHA-256 prefix
25248a6d0b0f8b67). Run from this folder:

    python ve_ats.py                 # all diagrams
    python ve_ats.py seq_login       # selected diagrams

Output: out/<name>.png, .svg, .drawio. Diagram text is English; screen and button names stay
as on the Vietnamese UI.
"""
import sys

from srsdiagram import STYLE, Seq, StateDiagram, lh, save


def user(key, label):
    return (key, 'actor', label)


def page(key, label):
    return (key, 'boundary', label)


def ctl(key, label):
    return (key, 'control', label)


DB = ('DB', 'entity', ':Database')
FS = ('FS', 'entity', ':CV file store')


# ------------------------------------------------------------------ authentication

def seq_login():
    s = Seq('Login', [user('U', 'User'), page('UI', ':Login page'), ctl('C', ':AuthController'),
                      ctl('S', ':AuthService'), DB])
    s.call('U', 'UI', 'enter email, password, "Đăng nhập"')
    s.call('UI', 'C', 'POST /api/auth/login')
    s.call('C', 'S', 'LoginAsync(request, client)')
    s.call('S', 'DB', 'find user by email')
    s.reply('DB', 'S', 'user or none')
    s.self_('S', 'check active, not locked, password')
    s.frag('alt', 'unknown, inactive, locked or wrong password')
    s.frag('opt', 'user exists')
    s.call('S', 'DB', 'count failure, lock after 5, audit')
    s.reply('DB', 'S', 'saved')
    s.end()
    s.reply('S', 'C', 'INVALID_CREDENTIALS')
    s.reply('C', 'UI', '401 INVALID_CREDENTIALS')
    s.reply('UI', 'U', '"Email hoặc mật khẩu không đúng"')
    s.else_('credentials valid')
    s.self_('S', 'issue JWT (15 min), refresh token')
    s.call('S', 'DB', 'reset failures, save token hash, audit')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'C', 'AuthResult')
    s.reply('C', 'UI', '200 AuthResponse, refresh cookie')
    s.reply('UI', 'U', 'Dashboard of the role')
    s.end()
    return s.build()


def seq_refresh_token():
    s = Seq('Refresh access token', [user('U', 'User'), page('UI', ':Internal app'),
                                     ctl('C', ':AuthController'), ctl('S', ':AuthService'), DB])
    s.call('U', 'UI', 'open a page after 15 minutes')
    s.self_('UI', 'API call returns 401')
    s.call('UI', 'C', 'POST /api/auth/refresh (cookie)')
    s.call('C', 'S', 'RefreshAsync(rawToken, client)')
    s.call('S', 'DB', 'find token by SHA-256 hash')
    s.reply('DB', 'S', 'refresh token or none')
    s.frag('alt', 'unknown or expired')
    s.reply('S', 'C', 'UNAUTHENTICATED')
    s.reply('C', 'UI', '401 UNAUTHENTICATED')
    s.reply('UI', 'U', 'Login page')
    s.else_('already revoked: token reused')
    s.call('S', 'DB', 'revoke whole token family, audit')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'C', 'UNAUTHENTICATED')
    s.reply('C', 'UI', '401 UNAUTHENTICATED')
    s.reply('UI', 'U', 'Login page')
    s.else_('active')
    s.call('S', 'DB', 'revoke old, save new in same family')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'C', 'AuthResult')
    s.reply('C', 'UI', '200 AuthResponse, new cookie')
    s.self_('UI', 'retry the original request once')
    s.reply('UI', 'U', 'page with data')
    s.end()
    return s.build()


# ------------------------------------------------------------------ intake

def seq_intake():
    s = Seq('Intake', [ctl('CL', ':Caller'), ctl('IS', ':IntakeService'), ctl('CF', ':CvFileService'),
                       FS, ctl('CS', ':CandidateService'), DB])
    s.call('CL', 'IS', 'SubmitAsync(IntakeRequest)')
    s.call('IS', 'DB', 'load position')
    s.reply('DB', 'IS', 'position')
    s.frag('break', 'position not OPEN')
    s.reply('IS', 'CL', 'POSITION_NOT_OPEN')
    s.end()
    s.call('IS', 'CF', 'StoreAsync(cv)')
    s.self_('CF', 'check type, ≤ 10 MB')
    s.self_('CF', 'extract text')
    s.frag('break', 'wrong type or no text')
    s.reply('CF', 'IS', 'file error code')
    s.reply('IS', 'CL', 'same error code')
    s.end()
    s.call('CF', 'FS', 'save file under a new key')
    s.reply('FS', 'CF', 'saved')
    s.reply('CF', 'IS', 'CvFile')
    s.call('IS', 'CS', 'FindOrCreateAsync(candidate)')
    s.call('CS', 'DB', 'find by email or phone')
    s.reply('DB', 'CS', 'candidate or none')
    s.reply('CS', 'IS', 'existing or new candidate')
    s.call('IS', 'DB', 'find application for this position')
    s.reply('DB', 'IS', 'none or existing id')
    s.frag('break', 'already applied')
    s.call('IS', 'FS', 'delete stored file')
    s.reply('FS', 'IS', 'deleted')
    s.reply('IS', 'CL', 'DUPLICATE_APPLICATION')
    s.end()
    s.call('IS', 'DB', 'add application NEW, history, consent')
    s.call('IS', 'DB', 'queue analysis, receipt email, audit')
    s.reply('DB', 'IS', 'committed')
    s.reply('IS', 'CL', 'IntakeResult')
    return s.build()


def seq_add_application():
    s = Seq('Add application with CV', [user('R', 'Recruiter'), page('UI', ':Add application modal'),
                                       ctl('API', ':API'), ctl('CS', ':CandidateService'),
                                       ctl('IS', ':IntakeService'), DB])
    s.call('R', 'UI', 'choose position, type email, phone')
    s.call('UI', 'API', 'GET …/candidates/match')
    s.call('API', 'CS', 'MatchAsync(email, phone, positionId)')
    s.call('CS', 'DB', 'find by normalized email, phone')
    s.reply('DB', 'CS', 'candidate and applications')
    s.reply('CS', 'API', 'CandidateMatch')
    s.reply('API', 'UI', '200 CandidateMatch')
    s.reply('UI', 'R', 'existing candidate shown if found')
    s.call('R', 'UI', 'attach CV, choose source, "Thêm hồ sơ"')
    s.call('UI', 'API', 'POST …/applications (multipart)')
    s.call('API', 'IS', 'SubmitAsync(IntakeRequest)')
    s.ref('IS', 'DB', 'Intake')
    s.frag('alt', 'POSITION_NOT_OPEN or DUPLICATE_APPLICATION')
    s.reply('IS', 'API', 'error code')
    s.reply('API', 'UI', '409 with error code')
    s.reply('UI', 'R', '"Ứng viên đã ứng tuyển vị trí này"')
    s.else_('file rejected')
    s.reply('IS', 'API', 'error code')
    s.reply('API', 'UI', '400 or 413 with error code')
    s.reply('UI', 'R', 'message under the CV field')
    s.else_('created')
    s.reply('IS', 'API', 'IntakeResult')
    s.reply('API', 'UI', '201 ApplicationCreated')
    s.reply('UI', 'R', 'card in NEW, "Đang phân tích CV…"')
    s.end()
    return s.build()


def seq_apply_online():
    s = Seq('Apply online', [user('C', 'Candidate'), page('UI', ':Career site'),
                             ctl('API', ':PublicJobPostsController'), ctl('PS', ':PublicApplyService'),
                             DB, user('TS', 'Cloudflare Turnstile')])
    s.call('C', 'UI', 'open shared link ?src=linkedin')
    s.call('UI', 'API', 'GET /api/public/job-posts/{slug}')
    s.reply('API', 'UI', '200 PublicJobPost')
    s.reply('UI', 'C', 'job detail, "Ứng tuyển"')
    s.call('C', 'UI', 'fill form, attach CV, tick consent')
    s.call('UI', 'TS', 'run challenge')
    s.reply('TS', 'UI', 'turnstileToken')
    s.reply('UI', 'C', '"Nộp hồ sơ" enabled')
    s.call('C', 'UI', 'click "Nộp hồ sơ"')
    s.call('UI', 'API', 'POST …/{slug}/applications')
    s.self_('API', 'limit 5 per IP per hour')
    s.call('API', 'PS', 'ApplyAsync(slug, command, client)')
    s.call('PS', 'TS', 'POST siteverify {token, ip}')
    s.reply('TS', 'PS', 'success or failure')
    s.call('PS', 'DB', 'count today by email, load post')
    s.reply('DB', 'PS', 'count, job post')
    s.frag('break', 'CAPTCHA failed, limit hit or post expired')
    s.reply('PS', 'API', 'CAPTCHA_FAILED, RATE_LIMITED…')
    s.reply('API', 'UI', '400, 429 or 409 with error code')
    s.reply('UI', 'C', 'message, input kept')
    s.end()
    s.ref('PS', 'DB', 'Intake')
    s.frag('alt', 'created or already applied')
    s.reply('PS', 'API', 'PublicApplyResult')
    s.reply('API', 'UI', '202 PublicApplyResult')
    s.reply('UI', 'C', 'thank-you page, receipt email sent')
    s.else_('file rejected')
    s.reply('PS', 'API', 'error code')
    s.reply('API', 'UI', '400 or 413 with error code')
    s.reply('UI', 'C', 'message under the CV field')
    s.end()
    return s.build()


# ------------------------------------------------------------------ screening

def seq_analyze_cv():
    s = Seq('Analyze CV with AI', [ctl('S', ':ScreeningService'), DB, user('AI', 'AI Service'),
                                   ctl('J', ':AnalysisRunner')])
    s.frag('loop', 'every 2 s')
    s.call('J', 'S', 'AnalyzeNextAsync()')
    s.call('S', 'DB', 'claim next PENDING, 2-min lease')
    s.reply('DB', 'S', 'analysis or none')
    s.frag('break', 'queue empty')
    s.reply('S', 'J', 'false')
    s.end()
    s.call('S', 'DB', 'load CV text, candidate, criteria')
    s.reply('DB', 'S', 'data')
    s.self_('S', 'redact name, contacts, birth date')
    s.call('S', 'AI', 'redacted text, criteria (45 s)')
    s.frag('alt', 'timeout, 429 or 5xx')
    s.reply('AI', 'S', 'error')
    s.call('S', 'DB', 'PENDING again or FAILED, log call')
    s.reply('DB', 'S', 'saved')
    s.else_('valid result')
    s.reply('AI', 'S', 'profile, criterion results, quotes')
    s.self_('S', 'quote not in CV → UNVERIFIED')
    s.self_('S', 'weighted score, must-have flag')
    s.call('S', 'DB', 'COMPLETED, copy score, log call')
    s.reply('DB', 'S', 'saved')
    s.end()
    s.reply('S', 'J', 'true: take next job now')
    s.end()
    return s.build()


def seq_override_ai():
    s = Seq('Override AI result', [user('R', 'Recruiter'), page('UI', ':Criteria evaluation tab'),
                                   ctl('API', ':ScreeningController'), ctl('S', ':ScreeningService'), DB])
    s.call('R', 'UI', 'click "Điều chỉnh" on a criterion')
    s.reply('UI', 'R', 'modal with AI result and quote')
    s.call('R', 'UI', 'choose result, reason, "Lưu"')
    s.frag('break', 'reason shorter than 10 characters')
    s.reply('UI', 'R', 'field error, nothing sent')
    s.end()
    s.call('UI', 'API', 'PUT …/criteria/{criterionId}/override')
    s.call('API', 'S', 'OverrideAsync(analysisId, criterionId)')
    s.call('S', 'DB', 'load analysis, criteria, application')
    s.reply('DB', 'S', 'analysis')
    s.frag('alt', 'analysis not COMPLETED')
    s.reply('S', 'API', 'ANALYSIS_NOT_COMPLETED')
    s.reply('API', 'UI', '409 ANALYSIS_NOT_COMPLETED')
    s.reply('UI', 'R', '"Cần phân tích lại"')
    s.else_('COMPLETED')
    s.self_('S', 'keep AI result, set final result')
    s.self_('S', 'recalculate fit score')
    s.call('S', 'DB', 'save result, scores, AI_OVERRIDE audit')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'API', 'Analysis')
    s.reply('API', 'UI', '200 Analysis')
    s.reply('UI', 'R', '"Đã điều chỉnh", score 89 → 92')
    s.end()
    return s.build()


# ------------------------------------------------------------------ pipeline and email

def seq_move_stage():
    s = Seq('Reject an application', [user('R', 'Recruiter'), page('UI', ':Pipeline board'),
                                      ctl('API', ':ApplicationsController'), ctl('S', ':ApplicationService'),
                                      ctl('ES', ':EmailScheduler'), DB])
    s.call('R', 'UI', 'choose "Từ chối" on a card')
    s.reply('UI', 'R', 'modal, warns an email will be sent')
    s.call('R', 'UI', 'choose reason, "Xác nhận"')
    s.call('UI', 'API', 'POST …/{id}/stage {REJECTED}')
    s.call('API', 'S', 'MoveStageAsync(id, request)')
    s.call('S', 'DB', 'load application with xmin')
    s.reply('DB', 'S', 'application')
    s.self_('S', 'check transition and role')
    s.frag('break', 'move not allowed')
    s.reply('S', 'API', 'INVALID_STAGE_TRANSITION')
    s.reply('API', 'UI', '409 with allowedStages')
    s.reply('UI', 'R', 'allowed next stages')
    s.end()
    s.self_('S', 'set stage, reason, history entry')
    s.call('S', 'ES', 'ScheduleAsync(REJECTION, …)')
    s.self_('ES', 'render template, consent link')
    s.reply('ES', 'S', 'email PENDING, +10 min')
    s.call('S', 'DB', 'save stage, email, audit')
    s.reply('DB', 'S', 'saved or xmin changed')
    s.frag('alt', 'xmin changed')
    s.reply('S', 'API', 'CONCURRENCY_CONFLICT')
    s.reply('API', 'UI', '409 CONCURRENCY_CONFLICT')
    s.reply('UI', 'R', 'card reloaded')
    s.else_('saved')
    s.reply('S', 'API', 'StageMoveResult')
    s.reply('API', 'UI', '200 StageMoveResult')
    s.reply('UI', 'R', 'card in REJECTED, "Xem email"')
    s.end()
    return s.build()


def seq_email_dispatch():
    s = Seq('Cancel and send candidate emails', [user('R', 'Recruiter'), page('UI', ':Emails tab'),
                                                 ctl('API', ':EmailsController'), ctl('S', ':EmailService'),
                                                 DB, user('SM', 'SMTP'), ctl('J', ':EmailDispatcher')])
    s.call('R', 'UI', 'click "Huỷ" on a pending email')
    s.call('UI', 'API', 'POST …/emails/{id}/cancel')
    s.call('API', 'S', 'CancelAsync(id)')
    s.call('S', 'DB', 'CANCELLED where status PENDING')
    s.reply('DB', 'S', 'rows updated')
    s.frag('alt', 'no row: already sending or sent')
    s.reply('S', 'API', 'EMAIL_NOT_PENDING')
    s.reply('API', 'UI', '409 EMAIL_NOT_PENDING')
    s.reply('UI', 'R', '"Email đã được gửi"')
    s.else_('one row')
    s.reply('S', 'API', 'EmailMessage')
    s.reply('API', 'UI', '200 EmailMessage')
    s.reply('UI', 'R', '"Đã huỷ"')
    s.end()
    s.frag('loop', 'every 15 s')
    s.call('J', 'S', 'DispatchDueAsync()')
    s.call('S', 'DB', 'claim due PENDING → SENDING')
    s.reply('DB', 'S', 'claimed emails')
    s.frag('loop', 'each claimed email')
    s.call('S', 'SM', 'send')
    s.frag('alt', 'accepted')
    s.reply('SM', 'S', 'message id')
    s.call('S', 'DB', 'SENT, sent time')
    s.reply('DB', 'S', 'saved')
    s.else_('error')
    s.reply('SM', 'S', 'error')
    s.call('S', 'DB', 'retry +1, +5, +30 min, FAILED')
    s.reply('DB', 'S', 'saved')
    s.end()
    s.end()
    s.reply('S', 'J', 'number sent')
    s.end()
    return s.build()


# ------------------------------------------------------------------ interviews

def seq_schedule_interview():
    s = Seq('Schedule interview', [user('R', 'Recruiter'), page('UI', ':Schedule interview modal'),
                                   ctl('API', ':InterviewsController'), ctl('S', ':InterviewService'),
                                   ctl('ES', ':EmailScheduler'), DB])
    s.call('R', 'UI', 'time, place, panel, "Lưu lịch"')
    s.call('UI', 'API', 'POST …/{id}/interviews')
    s.call('API', 'S', 'ScheduleAsync(applicationId, request)')
    s.call('S', 'DB', 'load application, panel users')
    s.reply('DB', 'S', 'application, users')
    s.frag('break', 'stage is not INTERVIEW')
    s.reply('S', 'API', 'APPLICATION_NOT_IN_INTERVIEW')
    s.reply('API', 'UI', '409 with error code')
    s.reply('UI', 'R', 'message, modal stays open')
    s.end()
    s.call('S', 'DB', 'find overlapping interviews')
    s.reply('DB', 'S', 'overlaps')
    s.frag('break', 'a panel member is busy')
    s.reply('S', 'API', 'INTERVIEW_OVERLAP, conflicts')
    s.reply('API', 'UI', '409 INTERVIEW_OVERLAP')
    s.reply('UI', 'R', 'names the busy member and time')
    s.end()
    s.self_('S', 'create interview, panel slots')
    s.call('S', 'ES', 'ScheduleAsync(INVITATION)')
    s.reply('ES', 'S', 'email PENDING, +10 min')
    s.call('S', 'DB', 'save interview, email, audit')
    s.reply('DB', 'S', 'saved or exclusion violation')
    s.frag('alt', 'exclusion constraint violated')
    s.reply('S', 'API', 'INTERVIEW_OVERLAP')
    s.reply('API', 'UI', '409 INTERVIEW_OVERLAP')
    s.reply('UI', 'R', 'names the busy member')
    s.else_('saved')
    s.reply('S', 'API', 'Interview')
    s.reply('API', 'UI', '201 Interview')
    s.reply('UI', 'R', 'interview listed, "Xem email"')
    s.end()
    return s.build()


def seq_submit_scorecard():
    s = Seq('Submit blind scorecard', [user('I', 'Interviewer'), page('UI', ':Scorecard page'),
                                       ctl('API', ':InterviewsController'), ctl('S', ':ScorecardService'), DB])
    s.call('I', 'UI', 'open from "Scorecards pending"')
    s.call('UI', 'API', 'GET …/interviews/{id}/scorecard')
    s.call('API', 'S', 'GetMineAsync(interviewId)')
    s.call('S', 'DB', 'load interview, panel, my scorecard')
    s.reply('DB', 'S', 'data')
    s.frag('break', 'not on the panel')
    s.reply('S', 'API', 'FORBIDDEN')
    s.reply('API', 'UI', '403 FORBIDDEN')
    s.reply('UI', 'I', '403 page')
    s.end()
    s.reply('S', 'API', 'Scorecard (draft)')
    s.reply('API', 'UI', '200 Scorecard')
    s.reply('UI', 'I', 'criteria to rate, CV, no AI score')
    s.call('I', 'UI', 'rate, recommend, "Gửi đánh giá"')
    s.call('UI', 'API', 'POST …/scorecard/submit')
    s.call('API', 'S', 'SubmitAsync(interviewId, request)')
    s.call('S', 'DB', 'load my scorecard')
    s.reply('DB', 'S', 'scorecard')
    s.frag('alt', 'already SUBMITTED')
    s.reply('S', 'API', 'SCORECARD_ALREADY_SUBMITTED')
    s.reply('API', 'UI', '409 SCORECARD_ALREADY_SUBMITTED')
    s.reply('UI', 'I', 'read-only scorecard')
    s.else_('complete')
    s.call('S', 'DB', 'save SUBMITTED, audit')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'API', 'Scorecard')
    s.reply('API', 'UI', '200 Scorecard')
    s.call('UI', 'API', 'GET …/{applicationId}/scorecards')
    s.reply('API', 'UI', '200 ScorecardsView, others visible')
    s.reply('UI', 'I', 'own and other submitted scorecards')
    s.end()
    return s.build()


# ------------------------------------------------------------------ mailbox and import

def seq_mailbox_intake():
    s = Seq('Collect CVs from the mailbox', [ctl('S', ':MailboxIntakeService'), FS, DB,
                                             user('IM', 'Mailbox (IMAP)'), ctl('J', ':MailboxPoller')])
    s.frag('loop', 'every 5 minutes')
    s.call('J', 'S', 'PollAsync()')
    s.call('S', 'DB', 'load settings, known Message-IDs')
    s.reply('DB', 'S', 'settings')
    s.frag('break', 'mailbox reading paused')
    s.reply('S', 'J', '0')
    s.end()
    s.call('S', 'IM', 'fetch new messages, read-only')
    s.reply('IM', 'S', 'messages with attachments')
    s.frag('loop', 'each new message with PDF or DOCX')
    s.call('S', 'FS', 'store attachments')
    s.reply('FS', 'S', 'storage keys')
    s.self_('S', 'find [JOB-xxx] in subject')
    s.frag('alt', 'code of an OPEN position')
    s.ref('S', 'DB', 'Intake')
    s.call('S', 'DB', 'inbox item CONVERTED or DUPLICATE')
    s.reply('DB', 'S', 'saved')
    s.else_('no valid code')
    s.call('S', 'DB', 'inbox item NEEDS_REVIEW')
    s.reply('DB', 'S', 'saved')
    s.end()
    s.end()
    s.call('S', 'DB', 'save last poll time, status')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'J', 'number of new items')
    s.end()
    return s.build()


def seq_bulk_import():
    s = Seq('Bulk import', [user('R', 'Recruiter'), page('UI', ':Bulk import wizard'),
                            ctl('API', ':ImportsController'), ctl('S', ':ImportService'), DB])
    s.call('R', 'UI', 'choose position, Excel and ZIP')
    s.call('UI', 'API', 'POST /api/imports/preview')
    s.self_('API', 'Excel ≤ 5 MB, ZIP ≤ 200 MB')
    s.call('API', 'S', 'PreviewAsync(positionId, excel, zip)')
    s.self_('S', 'read rows, list ZIP entries')
    s.frag('break', 'more than 200 rows')
    s.reply('S', 'API', 'VALIDATION_FAILED')
    s.reply('API', 'UI', '400 VALIDATION_FAILED')
    s.reply('UI', 'R', '"Tối đa 200 dòng"')
    s.end()
    s.call('S', 'DB', 'find existing candidates')
    s.reply('DB', 'S', 'matches')
    s.self_('S', 'mark each row VALID or INVALID')
    s.call('S', 'DB', 'save batch PREVIEWED, staging')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'API', 'ImportBatch')
    s.reply('API', 'UI', '200 ImportBatch')
    s.reply('UI', 'R', 'row results with reasons')
    s.call('R', 'UI', 'click "Nhập"')
    s.call('UI', 'API', 'POST …/imports/{batchId}/confirm')
    s.call('API', 'S', 'ConfirmAsync(batchId, sendReceipts)')
    s.call('S', 'DB', 'load batch, re-validate')
    s.reply('DB', 'S', 'batch')
    s.frag('alt', 'invalid rows or preview expired')
    s.reply('S', 'API', 'IMPORT_INVALID_ROWS, IMPORT_EXPIRED')
    s.reply('API', 'UI', '409 with error code')
    s.reply('UI', 'R', 'fix the file and upload again')
    s.else_('all rows valid')
    s.frag('loop', 'each row, one transaction')
    s.ref('S', 'DB', 'Intake')
    s.end()
    s.call('S', 'DB', 'COMPLETED, audit, commit')
    s.reply('DB', 'S', 'committed')
    s.reply('S', 'API', 'ImportBatch')
    s.reply('API', 'UI', '200 ImportBatch')
    s.reply('UI', 'R', 'number of applications created')
    s.end()
    return s.build()


# ------------------------------------------------------------------ public site

def seq_delete_my_data():
    s = Seq('Delete my data', [user('C', 'Candidate'), page('UI', ':Consent page'),
                               ctl('API', ':PublicConsentController'), ctl('CS', ':ConsentService'),
                               ctl('AN', ':AnonymizationService'), DB, FS])
    s.call('C', 'UI', 'open the link in an email')
    s.call('UI', 'API', 'GET /api/public/consent/{token}')
    s.call('API', 'CS', 'GetSummaryAsync(token)')
    s.self_('CS', 'verify link signature')
    s.frag('break', 'invalid or outdated link')
    s.reply('CS', 'API', 'CONSENT_TOKEN_INVALID')
    s.reply('API', 'UI', '404 CONSENT_TOKEN_INVALID')
    s.reply('UI', 'C', 'new link comes with next email')
    s.end()
    s.call('CS', 'DB', 'load candidate, applications')
    s.reply('DB', 'CS', 'data held')
    s.reply('CS', 'API', 'ConsentSummary')
    s.reply('API', 'UI', '200 ConsentSummary')
    s.reply('UI', 'C', 'data held and choices')
    s.call('C', 'UI', '"Xoá toàn bộ dữ liệu", confirm')
    s.call('UI', 'API', 'POST …/{token} DELETE_ALL')
    s.call('API', 'CS', 'ApplyAsync(DELETE_ALL)')
    s.call('CS', 'AN', 'AnonymizeAsync(candidateId)')
    s.call('AN', 'DB', 'reject open, cancel emails')
    s.call('AN', 'DB', 'clear data, ERASED, audit')
    s.reply('DB', 'AN', 'committed')
    s.call('AN', 'FS', 'delete CV files')
    s.reply('FS', 'AN', 'deleted')
    s.reply('AN', 'CS', 'done')
    s.reply('CS', 'API', 'no content')
    s.reply('API', 'UI', '204')
    s.reply('UI', 'C', 'confirmation on screen')
    return s.build()


def seq_publish_job_post():
    s = Seq('Publish job post and share', [user('R', 'Recruiter'), page('UI', ':Job post tab'),
                                           ctl('API', ':JobPostsController'), ctl('S', ':JobPostService'), DB])
    s.call('R', 'UI', 'edit content, "Xem trước"')
    s.reply('UI', 'R', 'post as candidates see it')
    s.call('R', 'UI', 'click "Đăng tin"')
    s.call('UI', 'API', 'POST …/job-post/publish')
    s.call('API', 'S', 'PublishAsync(positionId)')
    s.call('S', 'DB', 'load position, job post')
    s.reply('DB', 'S', 'position, job post')
    s.self_('S', 'owner, HR Manager or Admin')
    s.frag('alt', 'not allowed')
    s.reply('S', 'API', 'FORBIDDEN')
    s.reply('API', 'UI', '403 FORBIDDEN')
    s.reply('UI', 'R', 'permission message')
    s.else_('position not OPEN')
    s.reply('S', 'API', 'POSITION_NOT_OPEN')
    s.reply('API', 'UI', '409 POSITION_NOT_OPEN')
    s.reply('UI', 'R', '"Vị trí phải đang mở"')
    s.else_('allowed and OPEN')
    s.call('S', 'DB', 'PUBLISHED, time, audit')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'API', 'JobPost')
    s.reply('API', 'UI', '200 JobPost')
    s.reply('UI', 'R', 'status "Đang đăng"')
    s.end()
    s.call('R', 'UI', '"Sao chép link" for LinkedIn')
    s.call('UI', 'API', 'GET …/job-post/share-links')
    s.call('API', 'S', 'GetShareLinksAsync(positionId)')
    s.reply('S', 'API', 'one link per source')
    s.reply('API', 'UI', '200 ShareLink[]')
    s.reply('UI', 'R', 'link with ?src=linkedin copied')
    s.note('R', 'R', 'Recruiter posts the link', 170)
    return s.build()


def seq_link_preview():
    s = Seq('Link preview for a shared job post', [user('CR', 'LinkedIn crawler'), page('PX', ':Caddy proxy'),
                                                   ctl('API', ':SharePageController'),
                                                   ctl('S', ':PublicJobPostService'), DB])
    s.call('CR', 'PX', 'GET careers…/jobs/{slug}')
    s.self_('PX', 'user agent is a crawler')
    s.call('PX', 'API', 'GET /api/public/share/jobs/{slug}')
    s.call('API', 'S', 'RenderSharePageAsync(slug)')
    s.call('S', 'DB', 'load published post')
    s.reply('DB', 'S', 'job post or none')
    s.frag('alt', 'not published')
    s.reply('S', 'API', 'none')
    s.reply('API', 'PX', '404 text/html')
    s.reply('PX', 'CR', '404')
    s.else_('published')
    s.self_('S', 'og tags from public fields')
    s.reply('S', 'API', 'HTML')
    s.reply('API', 'PX', '200 text/html')
    s.reply('PX', 'CR', 'title, description, image')
    s.end()
    return s.build()


# ------------------------------------------------------------------ AI assistance

def seq_suggest_criteria():
    s = Seq('Suggest criteria from a job description', [
        user('R', 'Recruiter'), page('UI', ':Position form'), ctl('API', ':PositionsController'),
        ctl('S', ':CriteriaSuggestionService'), DB, user('AI', 'AI Service')])
    s.call('R', 'UI', 'title, description, "Gợi ý tiêu chí"')
    s.call('UI', 'API', 'POST …/criteria-suggestions')
    s.call('API', 'S', 'SuggestAsync(request)')
    s.call('S', 'DB', 'count my AI calls, last hour')
    s.reply('DB', 'S', 'count')
    s.frag('break', '30 calls reached')
    s.reply('S', 'API', 'RATE_LIMITED')
    s.reply('API', 'UI', '429 RATE_LIMITED')
    s.reply('UI', 'R', '"Thử lại sau", input kept')
    s.end()
    s.call('S', 'AI', 'title, level, description (30 s)')
    s.frag('alt', 'error, timeout or invalid JSON')
    s.reply('AI', 'S', 'error')
    s.call('S', 'DB', 'log call')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'API', 'AI_UNAVAILABLE')
    s.reply('API', 'UI', '502 AI_UNAVAILABLE')
    s.reply('UI', 'R', 'message, input kept')
    s.else_('valid result')
    s.reply('AI', 'S', 'criteria, type, weight, rationale')
    s.self_('S', 'check ≤ 12 items, weight 1–5')
    s.call('S', 'DB', 'log call')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'API', 'CriteriaSuggestionResult')
    s.reply('API', 'UI', '200 CriteriaSuggestionResult')
    s.reply('UI', 'R', 'drafts "Gợi ý bởi AI — chưa lưu"')
    s.end()
    s.call('R', 'UI', 'accept, edit, "Lưu"')
    s.call('UI', 'API', 'POST /api/positions')
    s.reply('API', 'UI', '201 PositionDetail')
    s.reply('UI', 'R', 'position with chosen criteria')
    return s.build()


def seq_embed_candidates():
    s = Seq('Embed pool candidates', [ctl('S', ':CandidateEmbeddingService'), DB,
                                      user('AI', 'Embedding API'), ctl('J', ':EmbeddingRunner')])
    s.frag('loop', 'every minute')
    s.call('J', 'S', 'EmbedPendingAsync()')
    s.call('S', 'DB', 'pool candidates with new CV')
    s.reply('DB', 'S', 'pending batch')
    s.frag('loop', 'each pending candidate')
    s.self_('S', 'redact name, contacts, birth date')
    s.call('S', 'AI', 'redacted CV text')
    s.frag('alt', 'success')
    s.reply('AI', 'S', 'vector, 1024 dimensions')
    s.call('S', 'DB', 'upsert vector, log call')
    s.reply('DB', 'S', 'saved')
    s.else_('error')
    s.reply('AI', 'S', 'error')
    s.call('S', 'DB', 'log call, retry next run')
    s.reply('DB', 'S', 'saved')
    s.end()
    s.end()
    s.reply('S', 'J', 'number embedded')
    s.call('J', 'S', 'DeleteStaleAsync()')
    s.call('S', 'DB', 'delete vectors outside the pool')
    s.reply('DB', 'S', 'rows deleted')
    s.reply('S', 'J', 'number deleted')
    s.end()
    return s.build()


def seq_pool_matches():
    s = Seq('Match talent pool to a position', [
        user('R', 'Recruiter'), page('UI', ':Pool matches tab'), ctl('API', ':TalentPoolController'),
        ctl('S', ':PoolMatchingService'), DB, user('AI', 'Embedding API')])
    s.call('R', 'UI', 'open "Gợi ý từ kho ứng viên"')
    s.call('UI', 'API', 'GET …/{id}/pool-matches')
    s.call('API', 'S', 'ListMatchesAsync(positionId, 20)')
    s.call('S', 'DB', 'position, criteria, cached vector')
    s.reply('DB', 'S', 'position, vector or none')
    s.frag('opt', 'no vector for this criteria version')
    s.call('S', 'AI', 'title, description, criteria')
    s.reply('AI', 'S', 'position vector')
    s.call('S', 'DB', 'cache vector, log call')
    s.reply('DB', 'S', 'saved')
    s.end()
    s.call('S', 'DB', 'nearest pool candidates, not applied')
    s.reply('DB', 'S', 'top 20 with distance')
    s.self_('S', 'skills that overlap the criteria')
    s.reply('S', 'API', 'PoolMatchList')
    s.reply('API', 'UI', '200 PoolMatchList')
    s.reply('UI', 'R', 'ranked candidates, similarity, skills')
    s.call('R', 'UI', '"Thêm vào vị trí" on one candidate')
    s.call('UI', 'API', 'POST …/talent-pool/{id}/applications')
    s.ref('API', 'DB', 'Intake')
    s.reply('API', 'UI', '201 ApplicationCreated')
    s.reply('UI', 'R', 'card in NEW, analysis queued')
    return s.build()


def seq_interview_questions():
    s = Seq('Suggest interview questions', [
        user('R', 'Recruiter'), page('UI', ':Schedule interview modal'), ctl('API', ':InterviewsController'),
        ctl('S', ':InterviewQuestionService'), DB, user('AI', 'AI Service')])
    s.call('R', 'UI', 'click "Gợi ý câu hỏi"')
    s.call('UI', 'API', 'POST …/interview-question-suggestions')
    s.call('API', 'S', 'SuggestAsync(applicationId)')
    s.call('S', 'DB', 'call count, criteria, analysis, quotes')
    s.reply('DB', 'S', 'data')
    s.frag('break', 'not INTERVIEW or limit reached')
    s.reply('S', 'API', 'error code')
    s.reply('API', 'UI', '409 or 429 with error code')
    s.reply('UI', 'R', 'message')
    s.end()
    s.self_('S', 'UNVERIFIED, NOT_MET, PARTIAL first')
    s.call('S', 'AI', 'criteria, results, quotes (30 s)')
    s.reply('AI', 'S', 'up to 10 questions')
    s.call('S', 'DB', 'log call')
    s.reply('DB', 'S', 'saved')
    s.reply('S', 'API', 'InterviewQuestionSuggestions')
    s.reply('API', 'UI', '200 InterviewQuestionSuggestions')
    s.reply('UI', 'R', 'drafts grouped by criterion')
    s.call('R', 'UI', 'edit, keep ≤ 8, "Lưu lịch"')
    s.call('UI', 'API', 'POST …/interviews {questions}')
    s.ref('API', 'DB', 'Schedule interview')
    s.reply('API', 'UI', '201 Interview')
    s.reply('UI', 'R', 'interview with its questions')
    return s.build()


# ------------------------------------------------------------------ state diagrams

def st_application():
    s = StateDiagram()
    s.state('new', 'NEW', 0, 0)
    s.state('scr', 'SCREENING', 1, 0)
    s.state('int', 'INTERVIEW', 2, 0)
    s.state('off', 'OFFER', 3, 0)
    s.state('hir', 'HIRED', 3, 1.6)
    s.state('rej', 'REJECTED', 1.5, 1.6)
    s.initial('new', 'application created')
    s.go('new', 'scr', 'move to next stage')
    s.go('scr', 'int', 'move to next stage')
    s.go('int', 'off', 'make offer [HR, Admin]')
    s.go('off', 'hir', 'mark hired [HR Manager, Admin]')
    s.final('hir')
    s.final('rej')
    d = s.build()
    # Composite state "In progress" around NEW…OFFER; one transition leaves it to REJECTED.
    # The module has no composite state, so it is drawn with Doc primitives (skill section 8).
    x0, y0 = StateDiagram.X0 - 24, StateDiagram.Y0 - 44
    x1 = StateDiagram.X0 + 3 * StateDiagram.COLW + StateDiagram.SW + 24
    y1 = StateDiagram.Y0 + StateDiagram.SH + 24
    d.rect(x0, y0, x1 - x0, y1 - y0, fill='none', stroke='#000000', sw=1.3, r=12)
    d.text(x0 + 12, y0 + 6, ['In progress'], STYLE['state_fs'], bold=True)
    rx = StateDiagram.X0 + 1.5 * StateDiagram.COLW + StateDiagram.SW / 2
    ry = StateDiagram.Y0 + 1.6 * StateDiagram.ROWH
    d.line([(rx, y1), (rx, ry)], end='open', sw=1.2)
    d.text(rx + 8, (y1 + ry) / 2 - lh(STYLE['label_fs']) / 2, ['reject [reason], candidate deletes data'],
           STYLE['label_fs'], bg='#ffffff')
    return d


def st_cv_analysis():
    s = StateDiagram()
    s.state('pen', 'PENDING', 0, 0)
    s.state('pro', 'PROCESSING', 1, 0)
    s.state('com', 'COMPLETED', 2, 0)
    s.state('fai', 'FAILED', 1, 1)
    s.state('out', 'OUTDATED', 2, 1)
    s.initial('pen', 'CV added, "Phân tích lại"')
    s.go('pen', 'pro', 'worker claims job')
    s.go('pro', 'com', 'valid result')
    s.go('pro', 'pen', 'transient error [attempt left]')
    s.go('pro', 'fai', 'error [no attempt left]')
    s.go('com', 'out', 'position criteria changed')
    s.final('com')
    s.final('fai')
    s.final('out')
    return s.build()


def st_email_message():
    s = StateDiagram()
    s.state('can', 'CANCELLED', 0, 0)
    s.state('pen', 'PENDING', 0, 1)
    s.state('snd', 'SENDING', 1, 1)
    s.state('sent', 'SENT', 2, 1)
    s.state('fai', 'FAILED', 1, 2)
    s.initial('pen', 'event queues email')
    s.go('pen', 'snd', 'when (send time reached)')
    s.go('snd', 'pen', 'send error [attempt left]')
    s.go('snd', 'sent', 'SMTP accepted')
    s.go('pen', 'can', 'cancel, action undone')
    s.go('snd', 'fai', 'send error [4th attempt]')
    s.go('fai', 'pen', '"Gửi lại"', route='hv')
    s.final('sent')
    s.final('can')
    return s.build()


def st_position():
    s = StateDiagram()
    s.choice('c', 0, 0)
    s.state('dra', 'DRAFT', 0.85, 0)
    s.state('opn', 'OPEN', 1.7, 1)
    s.state('clo', 'CLOSED', 2.7, 1)
    s.initial('c', 'create position')
    s.go('c', 'dra', '[save as draft]')
    s.go('c', 'opn', '[save as open]')
    s.go('dra', 'opn', 'open position', route='hv')
    s.go('opn', 'clo', 'close position')
    s.go('clo', 'opn', 'reopen position')
    return s.build()


def st_job_post():
    s = StateDiagram()
    s.state('dra', 'DRAFT', 0, 0)
    s.state('pub', 'PUBLISHED', 1, 0)
    s.state('unp', 'UNPUBLISHED', 1, 1)
    s.initial('dra', 'position created')
    s.go('dra', 'pub', 'publish [position OPEN]')
    s.go('pub', 'unp', 'unpublish, position closed')
    s.go('unp', 'pub', 'publish [position OPEN]')
    return s.build()


def st_inbox_item():
    s = StateDiagram()
    s.choice('c', 0, 1)
    s.state('rev', 'NEEDS_REVIEW', 1, 1)
    s.state('con', 'CONVERTED', 1, 0)
    s.state('dup', 'DUPLICATE', 2, 1)
    s.state('dis', 'DISMISSED', 1, 2)
    s.initial('c', 'message with CV read')
    s.go('c', 'con', '[OPEN position code]', route='vh')
    s.go('c', 'rev', '[no valid code]')
    s.go('rev', 'con', 'assign to position')
    s.go('rev', 'dup', 'assign [already applied]')
    s.go('rev', 'dis', 'dismiss')
    s.final('con')
    s.final('dup')
    s.final('dis')
    return s.build()


ALL = {
    'seq_login': (seq_login, 'Login'),
    'seq_refresh_token': (seq_refresh_token, 'Refresh access token'),
    'seq_intake': (seq_intake, 'Intake'),
    'seq_add_application': (seq_add_application, 'Add application'),
    'seq_apply_online': (seq_apply_online, 'Apply online'),
    'seq_analyze_cv': (seq_analyze_cv, 'Analyze CV with AI'),
    'seq_override_ai': (seq_override_ai, 'Override AI result'),
    'seq_move_stage': (seq_move_stage, 'Reject an application'),
    'seq_email_dispatch': (seq_email_dispatch, 'Cancel and send emails'),
    'seq_schedule_interview': (seq_schedule_interview, 'Schedule interview'),
    'seq_submit_scorecard': (seq_submit_scorecard, 'Submit scorecard'),
    'seq_mailbox_intake': (seq_mailbox_intake, 'Mailbox intake'),
    'seq_bulk_import': (seq_bulk_import, 'Bulk import'),
    'seq_delete_my_data': (seq_delete_my_data, 'Delete my data'),
    'seq_publish_job_post': (seq_publish_job_post, 'Publish job post'),
    'seq_link_preview': (seq_link_preview, 'Link preview'),
    'seq_suggest_criteria': (seq_suggest_criteria, 'Suggest criteria'),
    'seq_embed_candidates': (seq_embed_candidates, 'Embed pool candidates'),
    'seq_pool_matches': (seq_pool_matches, 'Pool matches'),
    'seq_interview_questions': (seq_interview_questions, 'Interview questions'),
    'st_application': (st_application, 'Application stage'),
    'st_cv_analysis': (st_cv_analysis, 'CV analysis status'),
    'st_email_message': (st_email_message, 'Email status'),
    'st_position': (st_position, 'Position status'),
    'st_job_post': (st_job_post, 'Job post status'),
    'st_inbox_item': (st_inbox_item, 'Inbox item status'),
}

if __name__ == '__main__':
    names = sys.argv[1:] or list(ALL)
    print(save({n: (ALL[n][0](), ALL[n][1]) for n in names}, 'out'))
