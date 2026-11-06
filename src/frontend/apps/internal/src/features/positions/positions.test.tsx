import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, test } from "vitest";
import { server } from "../../test/msw";
import { renderApp, signIn } from "../../test/renderApp";
import type { PositionDetail, PositionSummary } from "./api";

const kyThuat = { id: "0190a1b2-0000-7000-8000-0000000000d1", name: "Phòng Công nghệ thông tin" };
const marketing = { id: "0190a1b2-0000-7000-8000-0000000000d2", name: "Phòng Marketing" };
const anh = { id: "0190a1b2-0000-7000-8000-000000000011", fullName: "Nguyễn Minh Anh", role: "RECRUITER" as const };

const backend: PositionSummary = {
  id: "0190a1b2-0000-7000-8000-0000000000a1",
  code: "JOB-014",
  title: "Backend Developer (.NET)",
  department: kyThuat,
  location: "Hà Nội",
  status: "OPEN",
  owner: anh,
  headcount: 2,
  targetHireDate: "2099-11-01",
  applicationCount: 42,
  hiredCount: 1,
};

const detail: PositionDetail = {
  ...backend,
  employmentType: "FULL_TIME",
  level: "SENIOR",
  description: "Phát triển API cho hệ thống tuyển dụng.",
  criteriaVersion: 2,
  criteria: [
    { id: "c1", name: "C# và .NET", description: "Ít nhất 3 năm", type: "MUST_HAVE", weight: 5, version: 2 },
    { id: "c2", name: "Kubernetes", description: null, type: "NICE_TO_HAVE", weight: 2, version: 2 },
  ],
  stageCounts: [
    { stage: "NEW", count: 30 },
    { stage: "HIRED", count: 1 },
  ],
  createdAt: "2026-10-01T02:00:00Z",
  closedAt: null,
  rowVersion: "812",
};

function page(items: PositionSummary[], totalItems = items.length) {
  return { items, page: 1, pageSize: 20, totalItems, totalPages: 1 };
}

/** Answers listPositions with `items` (and tab counts by status), and records every query sent. */
function mockPositions(items: PositionSummary[]) {
  const queries: URLSearchParams[] = [];
  server.use(
    http.get("*/api/positions", ({ request }) => {
      const params = new URL(request.url).searchParams;
      queries.push(params);
      const status = params.get("status");
      const matching = status ? items.filter((p) => p.status === status) : items;
      return HttpResponse.json(page(params.get("pageSize") === "1" ? matching.slice(0, 1) : matching, matching.length));
    }),
    http.get("*/api/lookups/departments", () => HttpResponse.json([kyThuat, marketing])),
    http.get("*/api/lookups/users", () => HttpResponse.json([anh])),
  );
  return queries;
}

const listQueries = (queries: URLSearchParams[]) => queries.filter((q) => q.get("pageSize") !== "1");

describe("SCR-10 positions (US04)", () => {
  test("lists open positions first, with counts per status tab", async () => {
    const draft = { ...backend, id: "d", code: "JOB-015", title: "Chuyên viên Marketing", status: "DRAFT" as const };
    mockPositions([backend, draft]);
    signIn("RECRUITER", "Nguyễn Minh Anh");
    renderApp("/jobs");

    const row = (await screen.findByText("Backend Developer (.NET)")).closest("tr")!;
    expect(within(row).getByText("JOB-014")).toBeTruthy();
    expect(within(row).getByText("Phòng Công nghệ thông tin · Hà Nội")).toBeTruthy();
    expect(within(row).getByText("42")).toBeTruthy();
    expect(within(row).getByText("1 / 2")).toBeTruthy();
    expect(within(row).getByText("01/11/2099")).toBeTruthy();
    expect(within(row).getByText("Nguyễn Minh Anh")).toBeTruthy();
    expect(within(row).getByText("Đang tuyển")).toBeTruthy();
    expect(screen.queryByText("Chuyên viên Marketing")).toBeNull();

    const tabs = screen.getByRole("group", { name: "Trạng thái vị trí" });
    await waitFor(() => expect(within(tabs).getByRole("button", { name: "Tất cả · 2" })).toBeTruthy());
    expect(within(tabs).getByRole("button", { name: "Đang tuyển · 1", pressed: true })).toBeTruthy();
    expect(within(tabs).getByRole("button", { name: "Nháp · 1" })).toBeTruthy();
    expect(within(tabs).getByRole("button", { name: "Đã đóng · 0" })).toBeTruthy();

    fireEvent.click(within(tabs).getByRole("button", { name: "Nháp · 1" }));
    expect(await screen.findByText("Chuyên viên Marketing")).toBeTruthy();
  });

  test("search, department, and owner filters are sent to the API and start again at page 1", async () => {
    const queries = mockPositions([backend]);
    signIn("HR_MANAGER");
    renderApp("/jobs");
    await screen.findByText("Backend Developer (.NET)");

    fireEvent.change(screen.getByRole("searchbox", { name: "Tìm theo tên hoặc mã vị trí" }), {
      target: { value: " job-014 " },
    });
    await waitFor(() => expect(listQueries(queries).at(-1)?.get("q")).toBe("job-014"));

    const department = screen.getByRole("combobox", { name: "Phòng ban" });
    await within(department).findByText("Phòng Marketing");
    fireEvent.change(department, { target: { value: marketing.id } });
    await waitFor(() => expect(listQueries(queries).at(-1)?.get("departmentId")).toBe(marketing.id));

    const owner = screen.getByRole("combobox", { name: "Người phụ trách" });
    await within(owner).findByText("Nguyễn Minh Anh");
    fireEvent.change(owner, { target: { value: anh.id } });
    await waitFor(() => expect(listQueries(queries).at(-1)?.get("ownerId")).toBe(anh.id));

    const last = listQueries(queries).at(-1)!;
    expect(last.get("status")).toBe("OPEN");
    expect(last.get("page")).toBe("1");
  });

  test("an Interviewer has no owner filter (listUserOptions is not open to them)", async () => {
    mockPositions([]);
    signIn("INTERVIEWER");
    renderApp("/jobs");

    expect(await screen.findByText("Không có vị trí nào khớp bộ lọc.")).toBeTruthy();
    expect(screen.queryByRole("combobox", { name: "Người phụ trách" })).toBeNull();
  });

  test("the empty state tells no data apart from filtered out", async () => {
    mockPositions([]);
    signIn("ADMIN");
    renderApp("/jobs");
    await screen.findByText("Không có vị trí nào khớp bộ lọc.");

    fireEvent.click(screen.getByRole("button", { name: /^Tất cả/ }));

    expect(await screen.findByText("Chưa có vị trí tuyển dụng nào.")).toBeTruthy();
  });

  test("the title opens the position detail", async () => {
    mockPositions([backend]);
    server.use(http.get("*/api/positions/:id", () => HttpResponse.json(detail)));
    signIn("RECRUITER");
    renderApp("/jobs");

    fireEvent.click(await screen.findByRole("link", { name: "Backend Developer (.NET)" }));

    expect(await screen.findByRole("heading", { name: /Backend Developer \(\.NET\)/ })).toBeTruthy();
  });
});

describe("SCR-12 position detail (US04)", () => {
  test("shows the overview and the current criteria", async () => {
    server.use(http.get(`*/api/positions/${detail.id}`, () => HttpResponse.json(detail)));
    signIn("HR_MANAGER");
    renderApp(`/jobs/${detail.id}`);

    expect(await screen.findByText(/JOB-014/)).toBeTruthy();
    expect(screen.getByText("Toàn thời gian")).toBeTruthy();
    expect(screen.getByText("Senior")).toBeTruthy();
    expect(screen.getByText("Mới 30 · Đã tuyển 1")).toBeTruthy();
    expect(screen.getByText("Phát triển API cho hệ thống tuyển dụng.")).toBeTruthy();

    fireEvent.click(screen.getByRole("tab", { name: "Tiêu chí · 2" }));

    expect(screen.getByText("Phiên bản tiêu chí 2")).toBeTruthy();
    const row = screen.getByText("C# và .NET").closest("tr")!;
    expect(within(row).getByText("Bắt buộc")).toBeTruthy();
    expect(within(row).getByText("5")).toBeTruthy();
    expect(within(screen.getByText("Kubernetes").closest("tr")!).getByText("Cộng điểm")).toBeTruthy();
  });

  test("an unknown or hidden position says it was not found", async () => {
    server.use(
      http.get("*/api/positions/:id", () =>
        HttpResponse.json(
          { type: "x", title: "x", status: 404, detail: "Không tìm thấy vị trí tuyển dụng.", errorCode: "NOT_FOUND", traceId: "1" },
          { status: 404 },
        ),
      ),
    );
    signIn("INTERVIEWER");
    renderApp(`/jobs/${detail.id}`);

    expect(await screen.findByText("Không tìm thấy vị trí tuyển dụng.")).toBeTruthy();
    expect(screen.getByRole("link", { name: "Về danh sách vị trí" })).toBeTruthy();
  });
});
