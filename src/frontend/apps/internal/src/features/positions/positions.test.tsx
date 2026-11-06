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

describe("SCR-11 create position (US05)", () => {
  async function openForm() {
    const queries = mockPositions([backend]);
    const bodies: unknown[] = [];
    server.use(
      http.post("*/api/positions", async ({ request }) => {
        bodies.push(await request.json());
        return HttpResponse.json({ ...detail, id: "new", code: "JOB-015", title: "Kỹ sư dữ liệu" }, { status: 201 });
      }),
    );
    signIn("RECRUITER", "Nguyễn Minh Anh");
    renderApp("/jobs");
    fireEvent.click(await screen.findByRole("button", { name: "Tạo vị trí" }));
    const dialog = await screen.findByRole("dialog", { name: "Tạo vị trí tuyển dụng" });
    await within(dialog).findByRole("option", { name: "Phòng Marketing" });
    return { dialog, bodies, queries };
  }

  function fillGeneral(dialog: HTMLElement) {
    const set = (label: string, value: string) =>
      fireEvent.change(within(dialog).getByLabelText(label), { target: { value } });
    set("Tên vị trí *", "Kỹ sư dữ liệu");
    set("Phòng ban *", marketing.id);
    set("Địa điểm *", "TP. HCM");
    set("Cấp bậc *", "MIDDLE");
    set("Số lượng *", "2");
    set("Hạn tuyển *", "2099-12-31");
    set("Mô tả công việc *", "Xây dựng pipeline dữ liệu.");
    set("Tiêu chí 1", "Python");
  }

  test("an Interviewer has no create button and cannot open the form", async () => {
    mockPositions([]);
    signIn("INTERVIEWER");
    renderApp("/jobs");
    await screen.findByText("Không có vị trí nào khớp bộ lọc.");
    expect(screen.queryByRole("button", { name: "Tạo vị trí" })).toBeNull();
  });

  test("an Interviewer opening #/jobs/new gets the 403 page", async () => {
    signIn("INTERVIEWER");
    renderApp("/jobs/new");
    expect(await screen.findByRole("heading", { name: "Bạn không có quyền truy cập trang này" })).toBeTruthy();
  });

  test("required fields and a past date are reported under their fields without calling the API", async () => {
    const { dialog, bodies } = await openForm();
    fireEvent.change(within(dialog).getByLabelText("Hạn tuyển *"), { target: { value: "2000-01-01" } });

    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu và mở tuyển" }));

    expect(await within(dialog).findByText("Tên vị trí phải có từ 2 đến 150 ký tự.")).toBeTruthy();
    expect(within(dialog).getByText("Chọn phòng ban.")).toBeTruthy();
    expect(within(dialog).getByText("Chọn cấp bậc.")).toBeTruthy();
    expect(within(dialog).getByText("Hạn tuyển phải sau hôm nay.")).toBeTruthy();
    expect(within(dialog).getByText("Tên tiêu chí phải có từ 2 đến 150 ký tự.")).toBeTruthy();
    expect(within(dialog).getByLabelText("Tên vị trí *").getAttribute("aria-invalid")).toBe("true");
    expect(bodies).toEqual([]);
  });

  test("removing the last criterion asks for at least one", async () => {
    const { dialog, bodies } = await openForm();
    fillGeneral(dialog);
    fireEvent.click(within(dialog).getByRole("button", { name: "Xoá tiêu chí 1" }));

    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu nháp" }));

    expect(await within(dialog).findByText("Thêm ít nhất một tiêu chí đánh giá.")).toBeTruthy();
    expect(bodies).toEqual([]);
  });

  test("saving and opening sends the position with its weighted criteria and closes the form", async () => {
    const { dialog, bodies, queries } = await openForm();
    fillGeneral(dialog);
    fireEvent.click(within(dialog).getByRole("button", { name: "Thêm tiêu chí" }));
    fireEvent.change(within(dialog).getByLabelText("Tiêu chí 2"), { target: { value: "Spark" } });
    fireEvent.change(within(dialog).getByLabelText("Loại tiêu chí 2"), { target: { value: "NICE_TO_HAVE" } });
    fireEvent.change(within(dialog).getByLabelText("Trọng số tiêu chí 2"), { target: { value: "2" } });
    const listCalls = queries.length;

    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu và mở tuyển" }));

    expect(await screen.findByText("Đã tạo và mở tuyển JOB-015 Kỹ sư dữ liệu.")).toBeTruthy();
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(bodies).toEqual([
      {
        title: "Kỹ sư dữ liệu",
        departmentId: marketing.id,
        location: "TP. HCM",
        employmentType: "FULL_TIME",
        level: "MIDDLE",
        headcount: 2,
        targetHireDate: "2099-12-31",
        description: "Xây dựng pipeline dữ liệu.",
        criteria: [
          { name: "Python", type: "MUST_HAVE", weight: 3, description: null },
          { name: "Spark", type: "NICE_TO_HAVE", weight: 2, description: null },
        ],
        status: "OPEN",
      },
    ]);
    await waitFor(() => expect(queries.length).toBeGreaterThan(listCalls)); // the list is refreshed
  });

  test("field errors from the API are shown under the matching criterion", async () => {
    const { dialog } = await openForm();
    server.use(
      http.post("*/api/positions", () =>
        HttpResponse.json(
          {
            type: "x",
            title: "x",
            status: 400,
            detail: "Dữ liệu không hợp lệ.",
            errorCode: "VALIDATION_FAILED",
            traceId: "1",
            errors: { departmentId: ["Phòng ban không tồn tại hoặc đã ngừng hoạt động."], "criteria[0].weight": ["Trọng số phải từ 1 đến 5."] },
          },
          { status: 400 },
        ),
      ),
    );
    fillGeneral(dialog);

    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu nháp" }));

    expect(await within(dialog).findByText("Phòng ban không tồn tại hoặc đã ngừng hoạt động.")).toBeTruthy();
    expect(within(dialog).getByText("Trọng số phải từ 1 đến 5.")).toBeTruthy();
    expect(within(dialog).getByLabelText("Trọng số tiêu chí 1").getAttribute("aria-invalid")).toBe("true");
  });
});

describe("SCR-11 edit position (US06)", () => {
  const ME = "0190a1b2-0000-7000-8000-000000000001"; // the id signIn() uses
  const mine: PositionDetail = { ...detail, owner: { id: ME, fullName: "Phạm Thu Hà", role: "RECRUITER" } };

  function mockDetail(position: PositionDetail) {
    const bodies: unknown[] = [];
    let respond: () => Response = () => HttpResponse.json({ ...position, title: "Backend Developer (.NET 10)", rowVersion: "900" });
    server.use(
      http.get(`*/api/positions/${position.id}`, () => HttpResponse.json(position)),
      http.put(`*/api/positions/${position.id}`, async ({ request }) => {
        bodies.push(await request.json());
        return respond();
      }),
      http.get("*/api/lookups/departments", () => HttpResponse.json([kyThuat, marketing])),
      http.get("*/api/lookups/users", () =>
        HttpResponse.json([anh, { id: ME, fullName: "Phạm Thu Hà", role: "RECRUITER" }]),
      ),
    );
    return { bodies, respondWith: (r: () => Response) => (respond = r) };
  }

  test("the row menu offers Sửa only on positions the Recruiter owns", async () => {
    const other = { ...backend, id: "other", code: "JOB-016", title: "Kế toán tổng hợp" };
    mockPositions([{ ...backend, owner: mine.owner }, other]);
    signIn("RECRUITER");
    renderApp("/jobs");

    await screen.findByText("Kế toán tổng hợp");
    expect(screen.getByRole("button", { name: "Thao tác với JOB-014 Backend Developer (.NET)" })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Thao tác với JOB-016 Kế toán tổng hợp" })).toBeNull();
  });

  test("the owner saves changes with the row version and the kept criterion ids", async () => {
    const { bodies } = mockDetail(mine);
    signIn("RECRUITER");
    renderApp(`/jobs/${mine.id}`);

    fireEvent.click(await screen.findByRole("button", { name: "Sửa vị trí" }));
    const dialog = await screen.findByRole("dialog", { name: "Sửa vị trí JOB-014" });
    expect((within(dialog).getByLabelText("Tên vị trí *") as HTMLInputElement).value).toBe("Backend Developer (.NET)");
    expect(within(dialog).queryByLabelText("Người phụ trách")).toBeNull();
    fireEvent.change(within(dialog).getByLabelText("Tên vị trí *"), { target: { value: "Backend Developer (.NET 10)" } });

    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu thay đổi" }));

    expect(await screen.findByText("Đã lưu thay đổi cho JOB-014.")).toBeTruthy();
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(bodies).toEqual([
      {
        title: "Backend Developer (.NET 10)",
        departmentId: kyThuat.id,
        location: "Hà Nội",
        employmentType: "FULL_TIME",
        level: "SENIOR",
        headcount: 2,
        targetHireDate: "2099-11-01",
        description: "Phát triển API cho hệ thống tuyển dụng.",
        criteria: [
          { id: "c1", name: "C# và .NET", description: "Ít nhất 3 năm", type: "MUST_HAVE", weight: 5 },
          { id: "c2", name: "Kubernetes", description: null, type: "NICE_TO_HAVE", weight: 2 },
        ],
        rowVersion: "812",
      },
    ]);
  });

  test("a Recruiter who does not own the position has no edit button", async () => {
    mockDetail(detail);
    signIn("RECRUITER");
    renderApp(`/jobs/${detail.id}/edit`);

    await screen.findByText("Phát triển API cho hệ thống tuyển dụng.");
    expect(screen.queryByRole("button", { name: "Sửa vị trí" })).toBeNull();
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  test("an HR Manager reassigns the owner", async () => {
    const { bodies } = mockDetail(detail);
    signIn("HR_MANAGER");
    renderApp(`/jobs/${detail.id}/edit`);

    const dialog = await screen.findByRole("dialog", { name: "Sửa vị trí JOB-014" });
    const owner = within(dialog).getByLabelText("Người phụ trách");
    await within(owner).findByRole("option", { name: "Phạm Thu Hà" });
    fireEvent.change(owner, { target: { value: ME } });
    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu thay đổi" }));

    await waitFor(() => expect(bodies).toHaveLength(1));
    expect(bodies[0]).toMatchObject({ ownerId: ME, rowVersion: "812" });
  });

  test("changed criteria announce the new version", async () => {
    const { respondWith } = mockDetail(mine);
    respondWith(() => HttpResponse.json({ ...mine, criteriaVersion: 3, rowVersion: "901" }));
    signIn("RECRUITER");
    renderApp(`/jobs/${mine.id}/edit`);

    const dialog = await screen.findByRole("dialog", { name: "Sửa vị trí JOB-014" });
    fireEvent.change(within(dialog).getByLabelText("Trọng số tiêu chí 2"), { target: { value: "3" } });
    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu thay đổi" }));

    expect(
      await screen.findByText("Đã lưu JOB-014. Tiêu chí chuyển sang phiên bản 3; kết quả AI cũ được đánh dấu lỗi thời."),
    ).toBeTruthy();
  });

  test("a conflicting save keeps the form open and explains it", async () => {
    const { respondWith } = mockDetail(mine);
    respondWith(() =>
      HttpResponse.json(
        {
          type: "x",
          title: "x",
          status: 409,
          detail: "Vị trí vừa được người khác thay đổi. Tải lại để xem bản mới nhất rồi sửa lại.",
          errorCode: "CONCURRENCY_CONFLICT",
          traceId: "1",
        },
        { status: 409 },
      ),
    );
    signIn("RECRUITER");
    renderApp(`/jobs/${mine.id}/edit`);

    const dialog = await screen.findByRole("dialog", { name: "Sửa vị trí JOB-014" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Lưu thay đổi" }));

    expect(
      await within(dialog).findByText("Vị trí vừa được người khác thay đổi. Tải lại để xem bản mới nhất rồi sửa lại."),
    ).toBeTruthy();
  });
});
