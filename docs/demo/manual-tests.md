# Manual test script

Steps to try each built story by hand, on the data of `migrate --demo` ([README, Getting started](../../README.md)). Every story adds its section here, and its demo rows to `DemoDataSeeder`.

**Accounts** (password = `Seed__DemoPassword` in `.env`): `thu.ha@demo.test` HR Manager · `minh.anh@demo.test` and `minh.tam@demo.test` Recruiters · Interviewers `van.khoa@demo.test`, `thanh.binh@demo.test` · the first Admin from `Seed__AdminEmail`.

**Start:** `dotnet run --project src/RecruitmentAts.Api` and `npm run dev:internal -w @ats/internal` (in `src/frontend`), then open the address Vite prints.

## US09 Add application with CV

Sample files are in [`docs/demo/cvs`](cvs): `cv-tran-thi-lan.pdf` (has text), `cv-le-van-khoi.docx` (has text), `cv-scan-khong-co-chu.pdf` (a page without text, like a scan), `cv-gia-mao.pdf` (a text file renamed to `.pdf`). Demo candidates have the email `ung.vien.1@demo.test` … `ung.vien.N@demo.test` and the phone `0900 000 001` … with the same number.

Sign in as `minh.anh@demo.test`.

| # | Do | Expect |
|---|---|---|
| 1 | **Vị trí tuyển dụng** → open *Backend Developer (.NET)* (status *Đang tuyển*) | The header has **Thêm hồ sơ**. On a *Nháp* or *Đã đóng* position it is absent |
| 2 | **Thêm hồ sơ**; press **Thêm hồ sơ** in the form without filling it | Errors under the file, name, email ("Nhập email hoặc số điện thoại."), and source; nothing is sent |
| 3 | Choose `cv-tran-thi-lan.pdf`, name *Trần Thị Lan*, email `lan.tran@example.test`, source *LinkedIn* → **Thêm hồ sơ** | Toast "Đã thêm hồ sơ của Trần Thị Lan vào JOB-…"; you are back on the position; *Hồ sơ ứng tuyển* shows one more under *Mới* |
| 4 | Add `cv-le-van-khoi.docx` with name *Lê Văn Khôi*, phone only `0987 654 321`, source *Khác* | "Ghi rõ nguồn" must be filled first; then the application is added |
| 5 | Type `UNG.VIEN.1@demo.test` (or phone `+84 900 000 001`) in the email (phone) field | After a moment a notice says the candidate already exists and the new application will be attached to them |
| 6 | Add an application with that email to the position where that candidate already applied (the demo candidates each applied to one position) | The notice says they already applied; the button is disabled |
| 7 | Choose `cv-scan-khong-co-chu.pdf` | The API refuses: "Không đọc được nội dung văn bản trong CV…"; the form stays open and no application is added |
| 8 | Choose `cv-gia-mao.pdf` | Refused as not a valid PDF/DOCX |
| 9 | Choose an image, or a file over 10 MB | The form refuses it before sending |
| 10 | Sign in as `thu.ha@demo.test` (HR Manager) and add one | Works the same; **Thêm hồ sơ** is absent for `van.khoa@demo.test` (Interviewer cannot even open positions yet) |
| 11 | Press **Huỷ** or Esc in the form | Back on the position page; the browser Back button also returns there |

**Check in the database** (`docker compose exec postgres psql -U ats -d ats`): the application is in `applications` with `stage = 'NEW'`, one row in `stage_history` (no `from_stage`), one `cv_analyses` row with `status = 'PENDING'`, the file in `cv_files` and under `FileStorage__Root` (default `data/cv-files`), and an `audit_logs` row `APPLICATION` / `CREATE`.

**Not yet:** the receipt email (US27), the application list and detail (US08), the analysis result (US12).
