import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, test } from "vitest";
import { server } from "../../test/msw";
import { renderApp, signIn } from "../../test/renderApp";
import type { PositionDetail } from "../positions/api";
import type { ApplicationCreated, CandidateMatch } from "./api";

const position: PositionDetail = {
  id: "0190a1b2-0000-7000-8000-0000000000a1",
  code: "JOB-014",
  title: "Backend Developer (.NET)",
  department: { id: "0190a1b2-0000-7000-8000-0000000000d1", name: "Phòng Công nghệ thông tin" },
  location: "Hà Nội",
  status: "OPEN",
  owner: { id: "0190a1b2-0000-7000-8000-000000000011", fullName: "Nguyễn Minh Anh", role: "RECRUITER" },
  headcount: 2,
  targetHireDate: "2099-11-01",
  applicationCount: 3,
  hiredCount: 0,
  employmentType: "FULL_TIME",
  level: "SENIOR",
  description: "Phát triển API cho hệ thống tuyển dụng.",
  criteriaVersion: 1,
  criteria: [{ id: "c1", name: "C# và .NET", description: null, type: "MUST_HAVE", weight: 5, version: 1 }],
  stageCounts: [{ stage: "NEW", count: 3 }],
  createdAt: "2026-10-01T02:00:00Z",
  closedAt: null,
  rowVersion: "812",
};

const lan = { id: "0190a1b2-0000-7000-8000-0000000000c1", fullName: "Trần Thị Lan", email: "lan.tran@example.test", phone: "0912345678", emailInvalid: false, inTalentPool: false };

function created(candidateMatched: boolean): ApplicationCreated {
  return {
    candidateMatched,
    receiptEmailQueued: false,
    application: {
      id: "0190a1b2-0000-7000-8000-0000000000e1",
      candidate: lan,
      position: { id: position.id, code: position.code, title: position.title },
      stage: "NEW",
      source: "LINKEDIN",
      appliedAt: "2026-11-09T02:00:00Z",
      stageChangedAt: "2026-11-09T02:00:00Z",
      intakeChannel: "MANUAL",
      cvFiles: [],
      allowedStages: ["SCREENING", "REJECTED"],
      rowVersion: "1",
    },
  };
}

function mockPosition(detail: PositionDetail = position) {
  server.use(http.get(`*/api/positions/${detail.id}`, () => HttpResponse.json(detail)));
}

/** Answers matchCandidate and records the queries. */
function mockMatch(result: CandidateMatch) {
  const queries: URLSearchParams[] = [];
  server.use(
    http.get("*/api/candidates/match", ({ request }) => {
      queries.push(new URL(request.url).searchParams);
      return HttpResponse.json(result);
    }),
  );
  return queries;
}

const cvFile = () => new File(["%PDF-1.7 Trần Thị Lan"], "cv-tran-thi-lan.pdf", { type: "application/pdf" });

async function openForm() {
  mockPosition();
  signIn("RECRUITER");
  renderApp(`/jobs/${position.id}`);
  fireEvent.click(await screen.findByRole("button", { name: "Thêm hồ sơ" }));
  return screen.findByRole("dialog", { name: "Thêm hồ sơ ứng tuyển" });
}

function fill(dialog: HTMLElement, values: { name?: string; email?: string; phone?: string; source?: string; file?: File | null }) {
  const set = (label: string, value: string) => fireEvent.change(within(dialog).getByLabelText(label), { target: { value } });
  if (values.name !== undefined) set("Họ và tên *", values.name);
  if (values.email !== undefined) set("Email", values.email);
  if (values.phone !== undefined) set("Số điện thoại", values.phone);
  if (values.source !== undefined) set("Nguồn hồ sơ *", values.source);
  if (values.file !== undefined) {
    fireEvent.change(within(dialog).getByLabelText(/Tệp CV/), { target: { files: values.file ? [values.file] : [] } });
  }
}

describe("SCR-21 add application (US09)", () => {
  test("the position page opens the form and Huỷ leads back to the position", async () => {
    const dialog = await openForm();

    expect(within(dialog).getByText("JOB-014 Backend Developer (.NET)")).toBeTruthy();
    fireEvent.click(within(dialog).getByRole("button", { name: "Huỷ" }));

    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(screen.getByRole("heading", { name: /Backend Developer \(\.NET\)/ })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Thêm hồ sơ" })).toBeTruthy();
  });

  test("sends the CV and the candidate's details as a multipart form, then confirms and returns to the position", async () => {
    const contentTypes: (string | null)[] = [];
    server.use(
      http.post("*/api/applications", ({ request }) => {
        contentTypes.push(request.headers.get("content-type"));
        return HttpResponse.json(created(false), { status: 201 });
      }),
    );
    mockMatch({ matched: false });
    const dialog = await openForm();

    fill(dialog, { name: " Trần Thị Lan ", email: "lan.tran@example.test", phone: "0912 345 678", source: "LINKEDIN", file: cvFile() });
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }));

    expect(await screen.findByText("Đã thêm hồ sơ của Trần Thị Lan vào JOB-014.")).toBeTruthy();
    expect(contentTypes[0]).toMatch(/^multipart\/form-data; boundary=/);
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
  });

  test("says when the application was attached to a candidate who was already known", async () => {
    server.use(http.post("*/api/applications", () => HttpResponse.json(created(true), { status: 201 })));
    mockMatch({ matched: false });
    const dialog = await openForm();

    fill(dialog, { name: "Trần Thị Lan", email: "lan.tran@example.test", source: "TOPCV", file: cvFile() });
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }));

    expect(await screen.findByText(/ứng viên đã có trong hệ thống/)).toBeTruthy();
  });

  test("shows what is missing or wrong before anything is sent (AC-US09 #4, #5)", async () => {
    const dialog = await openForm();

    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }));

    expect(await within(dialog).findByText("Chọn tệp CV (PDF hoặc DOCX).")).toBeTruthy();
    expect(within(dialog).getByText("Họ tên phải có từ 2 đến 120 ký tự.")).toBeTruthy();
    expect(within(dialog).getByText("Nhập email hoặc số điện thoại.")).toBeTruthy();
    expect(within(dialog).getByText("Chọn nguồn hồ sơ.")).toBeTruthy();

    fill(dialog, { email: "khong-phai-email", file: new File(["x"], "anh.png", { type: "image/png" }) });
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }));
    expect(await within(dialog).findByText("Email không hợp lệ.")).toBeTruthy();
    expect(within(dialog).getByText("Chỉ chấp nhận tệp PDF hoặc DOCX.")).toBeTruthy();

    const big = new File([new Uint8Array(10 * 1024 * 1024 + 1)], "cv.pdf", { type: "application/pdf" });
    fill(dialog, { file: big });
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }));
    expect(await within(dialog).findByText("Tệp CV vượt quá 10 MB.")).toBeTruthy();
  });

  test("the source note appears and is required only for Khác", async () => {
    const dialog = await openForm();
    expect(within(dialog).queryByLabelText("Ghi rõ nguồn *")).toBeNull();

    fill(dialog, { name: "Trần Thị Lan", email: "lan.tran@example.test", source: "OTHER", file: cvFile() });
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }));

    expect(await within(dialog).findByText("Ghi rõ nguồn khi chọn \"Khác\".")).toBeTruthy();
  });

  test("shows a known candidate while the recruiter types (BR-06)", async () => {
    const queries = mockMatch({ matched: true, matchedBy: "EMAIL", candidate: lan, alreadyApplied: false, existingApplicationId: null });
    const dialog = await openForm();

    fill(dialog, { email: "lan.tran@example.test" });

    const notice = await within(dialog).findByRole("status");
    expect(notice.textContent).toContain("Ứng viên đã có trong hệ thống: Trần Thị Lan");
    expect(notice.textContent).toContain("Hồ sơ mới sẽ gắn vào ứng viên này.");
    expect(queries.at(-1)?.get("email")).toBe("lan.tran@example.test");
    expect(queries.at(-1)?.get("positionId")).toBe(position.id);
    expect(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }).hasAttribute("disabled")).toBe(false);
  });

  test("a candidate who already applied to this position cannot be added again", async () => {
    mockMatch({ matched: true, matchedBy: "PHONE", candidate: lan, alreadyApplied: true, existingApplicationId: "0190a1b2-0000-7000-8000-0000000000e0" });
    const dialog = await openForm();

    fill(dialog, { phone: "0912345678" });

    expect((await within(dialog).findByRole("status")).textContent).toContain("đã ứng tuyển vị trí này");
    expect(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }).hasAttribute("disabled")).toBe(true);
  });

  test("shows an error from the API above the form and keeps the form open", async () => {
    server.use(
      http.post("*/api/applications", () =>
        HttpResponse.json(
          {
            type: "x",
            title: "x",
            status: 400,
            detail: "Không đọc được nội dung văn bản trong CV.",
            errorCode: "NO_EXTRACTABLE_TEXT",
            traceId: "1",
          },
          { status: 400 },
        ),
      ),
    );
    mockMatch({ matched: false });
    const dialog = await openForm();

    fill(dialog, { name: "Trần Thị Lan", email: "lan.tran@example.test", source: "EMAIL", file: cvFile() });
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm hồ sơ" }));

    expect((await within(dialog).findByRole("alert")).textContent).toContain("Không đọc được nội dung văn bản trong CV.");
    expect(screen.getByRole("dialog", { name: "Thêm hồ sơ ứng tuyển" })).toBeTruthy();
  });

  test("a closed position offers no way to add an application", async () => {
    mockPosition({ ...position, status: "CLOSED", closedAt: "2026-11-01T02:00:00Z" });
    signIn("HR_MANAGER");
    renderApp(`/jobs/${position.id}`);

    expect(await screen.findByRole("button", { name: "Mở lại" })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Thêm hồ sơ" })).toBeNull();
  });

  test("the form route opens only for a position that takes applications", async () => {
    mockPosition({ ...position, status: "DRAFT" });
    signIn("RECRUITER");
    renderApp(`/jobs/${position.id}/applications/new`);

    expect(await screen.findByRole("heading", { name: /Backend Developer \(\.NET\)/ })).toBeTruthy();
    expect(screen.queryByRole("dialog")).toBeNull();
  });
});
