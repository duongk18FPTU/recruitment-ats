# UI/UX Design

> Design status: TARGET — high-fidelity prototype
>
> Figma file: [Recruitment ATS — UI](https://www.figma.com/design/0TYozUq5jB0KJcdo8vjVjQ/Recruitment-ATS?node-id=0-1)

Two products share one design system:

- **Internal app** — a working tool for the hiring team: dense tables, a fixed sidebar, forms in modals.
- **Career site** — a separate public site for candidates. Its structure follows the Vingroup Career Portal (`tuyendung.vingroup.net/jobs`): white page, filter panel on the left, job list with salary, location, deadline and type, an "Ứng tuyển" button on every job, and the apply form in a modal.

## Design direction (anti AI-slop)

| Avoid | Do instead |
|---|---|
| Default Tailwind blue or purple, gradients, glassmorphism | One brand colour (deep pine `#1F5C46`) on a warm paper canvas; flat fills |
| A card with a drop shadow for every number | KPIs share one bordered strip with dividers; tables use 1 px lines. Shadows only on modals and on the career site's filter and list panels |
| Pill badges with a coloured dot on a pastel fill | Outlined tags: white fill, 1 px border in the tone colour, tone-coloured text |
| Explanatory captions under every title ("Số hồ sơ đang ở mỗi giai đoạn") | Titles only; captions only for data (dates, counts, codes) |
| Uppercase labels, emoji, decorative icons | Sentence case; icons only where they carry meaning |
| Placeholder content ("Lorem", "John Doe", "Status") | Realistic Vietnamese names, positions, salaries, codes, and email addresses |

## Figma structure

| Page | Content |
|---|---|
| `00 Foundations` | Variables, text and effect styles, 47 icons, and all components |
| `01 Screens` | 18 internal screens (1440 × 900) in rows by flow |
| `02 Trang tuyển dụng` | 6 career-site pages (1440 wide) plus the mobile apply form (390 wide) |
| `03 Mind map` | Reserved for the product mind map (`docs/mindmap.png`) |

## Foundations

### Brand

The logo is a rounded pine square with three bars of decreasing length — candidates narrowing through the pipeline — and a light-green dot for the hire. The wordmark is "Recruitment **ATS**"; the career site uses the same name as the company name.

![Brand and navigation](images/screens/00-brand-navigation.png)

### Typography

| Style | Font | Weight | Size / line height | Usage |
|---|---|---|---|---|
| `UI/Title L` · `M` · `S` | Be Vietnam Pro | SemiBold | 22/28 · 17/24 · 14/20 | Page, modal, and panel titles |
| `UI/Body` · `Body Strong` | Be Vietnam Pro | Regular · Medium | 14/20 | Body text, table cells, buttons |
| `UI/Small` · `Small Strong` | Be Vietnam Pro | Regular · Medium | 13/18 | Secondary text, filters |
| `UI/Caption` · `UI/Label` | Be Vietnam Pro | Regular · Medium | 12/16 | Meta, form labels, table headers (sentence case) |
| `UI/Numeric` | Be Vietnam Pro | SemiBold | 26/32 | KPIs, fit score |
| `Mono/Code` | JetBrains Mono | Medium | 12/16 | Record codes (`JOB-014`, `APP-0153`), times, placeholders |
| `Public/H2` | Source Serif 4 | SemiBold | 30/38 | Long-form career-site headings (privacy policy) |
| `Public/H3` · `Lead` · `Body` · `Small` | Be Vietnam Pro | — | 19/26 · 18/28 · 16/26 · 14/22 | Career-site text; body is 16 px for reading on phones |

Be Vietnam Pro is designed for Vietnamese diacritics.

### Colour tokens (Figma variables, collection `ATS Tokens`)

| Token | Value | Usage | Contrast on white |
|---|---|---|---|
| `color/ink` · `ink-2` · `ink-3` | `#17191E` · `#535965` · `#666B75` | Primary, secondary, and placeholder text | 17.6 · 7.0 · 5.4 (4.9 on canvas) |
| `color/canvas` · `surface` · `surface-sunken` | `#F5F4F1` · `#FFFFFF` · `#FAF9F7` | App background, panels, table headers and footers | — |
| `color/line` · `line-strong` | `#E4E2DD` · `#BDB9B1` | Dividers; input and button borders | — |
| `color/brand` · `brand-hover` · `brand-soft` · `brand-accent` | `#1F5C46` · `#184A38` · `#E7F0EC` · `#8CCBB3` | Primary actions, active navigation, links, bars; selected rows; logo dot | 7.8 (text and white-on-brand) |
| `color/success` · `-soft` | `#2E7D4F` · `#E6F2EA` | Valid, published, criterion met | 5.0 |
| `color/warning` · `-soft` | `#9A5B0B` · `#FBF0DD` | Near deadline, partial, pending email | 5.4 |
| `color/danger` · `-soft` | `#B3261E` · `#FBE9E7` | Errors, rejected, failed email | 6.5 |
| `color/info` · `-soft` | `#2D5B8A` · `#E8EFF7` | Screening stage | 7.1 |
| `color/highlight` | `#FFF1B8` | CV passages quoted by the AI | — |

Every text colour meets WCAG 2.1 AA (≥ 4.5:1). Status colour is never the only signal: tags carry text, criterion results carry an icon and a word.

Spacing variables follow a 4 px grid (`space/4` … `space/48`); radii are `radius/sm` 4, `radius/md` 6, `radius/lg` 10. Effect style `Elevation/Card` is used only on the career site.

### Components

| Component | Variants / properties |
|---|---|
| `Logo` | `Type` = Full, Mark |
| `Button` | `Type` = Primary, Secondary, Ghost, Danger · `Size` = M, S · `Label` · `Show icon` · `Icon` |
| `Badge` | `Tone` = Neutral, Brand, Success, Warning, Danger, Info · `Label` (outlined tag) |
| `Field` | `State` = Default, Focus, Error, Disabled · `Label` · `Value` · `Helper` · `Show helper` · `Trailing icon` (select chevron, calendar, search, show password) |
| `Checkbox` | `Checked` · `Label` |
| `Tab` | `State` = Default, Active · `Label` |
| `Pagination` | `Style` = Table (summary, page size, pages), Centered (pages only) · `Summary` |
| `AI Score` | `State` = Scored, Pending, Failed · `Score` |
| `Avatar` | `Initials` |
| `NavItem` · `Sidebar` · `Topbar` | Sidebar `Role` = Admin, HR Manager, Recruiter, Interviewer (menu per the role matrix); NavItem `State`, `Icon`, `Count` |
| `Candidate Card` · `Stat` | Kanban card (`Name`, `Meta`, `Missing must-have`); KPI |
| `Career Header` · `Career Footer` · `Public Job Item` · `Public Job Mini` · `Chip` · `CV Upload` | Career-site header (`Active` link), footer, job row (`Title`, `Salary`, `Location`, `Deadline`, `Type`), compact job row (`State` = Default, Selected), department chip, CV drop zone (`State` = Empty, Selected, Error) |

![Base components](images/screens/00-components-base.png)

![Shell components](images/screens/00-components-shell.png)

## Screens — career site

| Screen | Figma frame | Key decisions |
|---|---|---|
| `PUB-01` Job list | PUB-01 Danh sách việc làm | Vingroup layout: banner, filter panel (title, location, department, type, level), "8 công việc đang chờ bạn", department chips, job items with salary · location · deadline · type and "Ứng tuyển", pagination |
| `PUB-02` Job detail | PUB-02 Chi tiết tin tuyển dụng | Other jobs on the left, current one highlighted; detail card with key facts, summary box, Mô tả công việc / Yêu cầu / Quyền lợi, apply button at top and bottom |
| `PUB-03` Apply | PUB-03 Ứng tuyển (desktop, modal) · PUB-03m (mobile) | Modal on desktop, full page with a fixed submit bar on mobile; only name, email, phone, CV, optional message; consent required, talent pool optional and unticked; CAPTCHA |
| `PUB-04` Thank-you | PUB-04 Đã nhận hồ sơ | Confirms the position and the email address; states the response time |
| `PUB-05` Privacy policy | PUB-05 Chính sách dữ liệu ứng viên | Five short sections with a table of contents |
| `PUB-06` Consent | PUB-06 Quản lý dữ liệu của bạn | Masked email, applications held, pool expiry; keep / leave pool / delete all, with the destructive option in red |

![PUB-01 Job list](images/screens/pub-01-jobs.png)

![PUB-02 Job detail](images/screens/pub-02-job-detail.png)

![PUB-03 Apply (desktop)](images/screens/pub-03-apply.png)

<img src="images/screens/pub-03m-apply-mobile.png" alt="PUB-03 Apply (mobile)" width="320">

![PUB-04 Thank-you](images/screens/pub-04-thank-you.png)

![PUB-05 Privacy policy](images/screens/pub-05-privacy.png)

![PUB-06 Consent](images/screens/pub-06-consent.png)

## Screens — internal app

| Screen | Figma frame | Role shown | Key decisions |
|---|---|---|---|
| `SCR-01` Login | SCR-01 Đăng nhập | — | Single centred form under the logo; show-password icon |
| `SCR-02` Dashboard | SCR-02 Tổng quan | HR Manager | KPI strip; pipeline per position; today's interviews; *Cần xử lý* list; new applications by source |
| `SCR-10` Positions | SCR-10 Vị trí tuyển dụng | Recruiter | Status tabs with counts; job post status next to position status; pagination |
| `SCR-11` Create position | SCR-11 Tạo vị trí tuyển dụng (modal) | Recruiter | Grouped sections; criteria as editable rows; "Lưu nháp" and "Lưu và mở tuyển" |
| `SCR-14` Job post | SCR-14 Chi tiết vị trí — Tin tuyển dụng | Recruiter | Public content editor; status with publish / unpublish; share links per source with applications per link and a copy button |
| `SCR-13` Pipeline | SCR-13 Pipeline ứng viên | Recruiter | Kanban with counts; AI score chip; warning line only when a must-have is missing |
| `SCR-20` Candidates | SCR-20 Ứng viên | Recruiter | Filters, stage tags, AI score, source; Excel export; pagination |
| `SCR-23` Criteria evaluation | SCR-23 Chi tiết hồ sơ — Đánh giá tiêu chí | Recruiter | Stage stepper; criteria with result icon and CV quote; CV preview with quoted passages highlighted and personal data hidden |
| `SCR-24` Override | SCR-24 Điều chỉnh kết quả tiêu chí (modal) | Recruiter | AI result next to the human result; mandatory reason; score preview 89 → 92 |
| `SCR-27` Emails | SCR-27 Chi tiết hồ sơ — Email | Recruiter | Pending email with countdown, edit and cancel; failed email with retry; rendered content |
| `SCR-32` Scorecard | SCR-32 Phiếu đánh giá phỏng vấn | Interviewer | 1–5 segmented rating; unscored row marked; submit disabled until complete; panel status without content |
| `SCR-41` CV inbox | SCR-41 Hộp thư CV | Recruiter | Message list and detail side by side; create-application form prefilled from the sender; required position shown as an error |
| `SCR-45` Talent pool | SCR-45 Kho ứng viên | Recruiter | Skill search; latest application and result; consent expiry, soon-to-expire in amber; "Thêm vào vị trí"; Excel export |
| `SCR-40` Bulk import | SCR-40 Nhập hàng loạt | Recruiter | Stepper; valid and error counts; error rows explained; import disabled while errors exist |
| `SCR-50` Reports | SCR-50 Báo cáo | HR Manager | KPI strip; funnel, sources, rejection reasons as single-hue bars; per-position table; PDF and Excel export |
| `SCR-70` Users | SCR-70 Người dùng | Admin | Role as plain text, status tag, last login; pagination |
| `SCR-75` Email templates | SCR-75 Cài đặt — Mẫu email | HR Manager | Events with their send delay; placeholders in mono and insertable; preview and test send |
| `SCR-76` Mailbox & sender | SCR-76 Cài đặt — Hộp thư & người gửi | Admin | IMAP and SMTP side by side; connection test result; masked passwords |

Remaining screens from the [screen inventory](04-information-architecture.md#screen-inventory) (position overview and criteria tabs, add-application modal, move / reject modal, edit candidate, schedule interview, my interviews, assign inbox item, add pool candidate, audit log, profile, 403) reuse the same shell and components.

![SCR-01 Login](images/screens/scr-01-login.png)

![SCR-02 Dashboard](images/screens/scr-02-dashboard.png)

![SCR-10 Positions](images/screens/scr-10-positions.png)

![SCR-11 Create position](images/screens/scr-11-position-form.png)

![SCR-14 Job post](images/screens/scr-14-job-post.png)

![SCR-13 Pipeline](images/screens/scr-13-pipeline.png)

![SCR-20 Candidates](images/screens/scr-20-candidates.png)

![SCR-23 Criteria evaluation](images/screens/scr-23-evaluation.png)

![SCR-24 Override](images/screens/scr-24-override.png)

![SCR-27 Emails](images/screens/scr-27-emails.png)

![SCR-32 Scorecard](images/screens/scr-32-scorecard.png)

![SCR-41 CV inbox](images/screens/scr-41-cv-inbox.png)

![SCR-45 Talent pool](images/screens/scr-45-talent-pool.png)

![SCR-40 Bulk import](images/screens/scr-40-bulk-import.png)

![SCR-50 Reports](images/screens/scr-50-reports.png)

![SCR-70 Users](images/screens/scr-70-users.png)

![SCR-75 Email templates](images/screens/scr-75-email-templates.png)

![SCR-76 Mailbox & sender](images/screens/scr-76-mailbox.png)

## UX principles applied

1. **Evidence over decoration.** An AI score is a number next to the quotes that justify it; the quoted passages are highlighted in the CV so a recruiter can check them in seconds.
2. **Human decisions stay explicit.** Stage changes are buttons the user presses; adjusting an AI result requires a reason and keeps the original.
3. **Independent evaluation.** Interviewers see who has submitted, not what they wrote, until their own scorecard is in; the AI score is not shown to interviewers.
4. **Role-shaped navigation.** The sidebar shows only permitted items (Interviewer 4, Recruiter 7, HR Manager 10, Admin 11; the profile opens from the user block).
5. **Recoverable errors.** Validation keeps input and states the rule where it applies (import errors per row, unscored criterion, missing position in the CV inbox).
6. **No surprise emails.** Any action that emails a candidate says so; apart from the receipt, emails wait 10 minutes with a countdown and a cancel button.
7. **Short path for candidates.** The career site asks only for what screening needs, works on a phone, and explains data use next to the consent checkboxes.
8. **Lists stay navigable.** Every list that can grow has pagination with a result summary and page size.
9. **Accessible by default.** Visible labels, WCAG AA contrast for all text tokens, status never by colour alone, keyboard alternative for drag-and-drop.

**Related:** [Information architecture](04-information-architecture.md) · [Use cases](02-use-cases.md)
