import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, test } from "vitest";
import { server } from "../../test/msw";
import { renderApp, signIn } from "../../test/renderApp";
import type { User } from "./api";

const ME = "0190a1b2-0000-7000-8000-000000000001"; // the id signIn() uses

const huy: User = {
  id: ME,
  email: "huy.do@recruitment-ats.vn",
  fullName: "Đỗ Quang Huy",
  phone: null,
  role: "ADMIN",
  isActive: true,
  lastLoginAt: null,
  createdAt: "2026-09-01T02:00:00Z",
};
const long: User = {
  id: "0190a1b2-0000-7000-8000-000000000005",
  email: "long.tran@recruitment-ats.vn",
  fullName: "Trần Đức Long",
  phone: "0901234567",
  role: "INTERVIEWER",
  isActive: true,
  lastLoginAt: "2026-10-28T03:00:00Z",
  createdAt: "2026-09-02T02:00:00Z",
};

function page(items: User[]) {
  return { items, page: 1, pageSize: 20, totalItems: items.length, totalPages: 1 };
}

function problem(status: number, errorCode: string, detail: string, errors?: Record<string, string[]>) {
  return HttpResponse.json(
    { type: "https://ats.example.vn/errors/x", title: "x", status, detail, errorCode, traceId: "00-1", errors },
    { status },
  );
}

async function openUsers(items: User[] = [huy, long]) {
  const queries: URLSearchParams[] = [];
  server.use(
    http.get("*/api/users", ({ request }) => {
      queries.push(new URL(request.url).searchParams);
      return HttpResponse.json(page(items));
    }),
  );
  signIn("ADMIN", "Đỗ Quang Huy");
  renderApp("/users");
  await screen.findByText("Trần Đức Long");
  return queries;
}

function rowOf(name: string) {
  return within(screen.getByRole("table")).getByText(name, { exact: false }).closest("tr")!;
}

function fillForm(values: Partial<Record<"Họ tên" | "Email" | "Mật khẩu tạm", string>> & { role?: string }) {
  for (const [label, value] of Object.entries(values)) {
    if (label === "role") {
      fireEvent.change(screen.getByLabelText("Vai trò", { selector: "select[name=role]" }), { target: { value } });
    } else {
      fireEvent.change(screen.getByLabelText(label), { target: { value } });
    }
  }
}

describe("SCR-70 users (US20)", () => {
  test("lists users with role, status, and last login", async () => {
    await openUsers();

    const row = rowOf("Trần Đức Long");
    expect(within(row).getByText("long.tran@recruitment-ats.vn")).toBeTruthy();
    expect(within(row).getByText("Người phỏng vấn")).toBeTruthy();
    expect(within(row).getByText("Hoạt động")).toBeTruthy();
    expect(within(row).getByText("28/10/2026")).toBeTruthy();
    expect(within(rowOf("Đỗ Quang Huy")).getByText("Chưa đăng nhập")).toBeTruthy();
    expect(screen.getByText("1–2 trong 2 người dùng")).toBeTruthy();
  });

  test("filters are sent to listUsers and restart at page 1", async () => {
    const queries = await openUsers();

    fireEvent.change(screen.getByLabelText("Vai trò"), { target: { value: "INTERVIEWER" } });
    fireEvent.change(screen.getByLabelText("Trạng thái"), { target: { value: "false" } });
    fireEvent.change(screen.getByLabelText("Tìm theo tên hoặc email"), { target: { value: " long " } });

    await waitFor(() => expect(queries.at(-1)?.get("q")).toBe("long"));
    const last = queries.at(-1)!;
    expect(last.get("role")).toBe("INTERVIEWER");
    expect(last.get("isActive")).toBe("false");
    expect(last.get("page")).toBe("1");
  });

  test("creating a user sends the temporary password and confirms with a toast", async () => {
    await openUsers();
    let body: unknown;
    server.use(
      http.post("*/api/users", async ({ request }) => {
        body = await request.json();
        const created = { ...long, id: "new", fullName: "Lê Thị An", email: "an.le@recruitment-ats.vn" };
        return HttpResponse.json(created, { status: 201 });
      }),
    );

    fireEvent.click(screen.getByRole("button", { name: "+ Thêm người dùng" }));
    const dialog = screen.getByRole("dialog", { name: "Thêm người dùng" });
    fillForm({
      "Họ tên": " Lê Thị An ",
      Email: "an.le@recruitment-ats.vn",
      role: "RECRUITER",
      "Mật khẩu tạm": "Mat-khau-tam-2026",
    });
    fireEvent.click(within(dialog).getByRole("button", { name: "Tạo tài khoản" }));

    expect(await screen.findByText("Đã tạo tài khoản cho Lê Thị An.")).toBeTruthy();
    expect(screen.queryByRole("dialog")).toBeNull();
    expect(body).toEqual({
      fullName: "Lê Thị An",
      email: "an.le@recruitment-ats.vn",
      phone: null,
      role: "RECRUITER",
      temporaryPassword: "Mat-khau-tam-2026",
    });
  });

  test("missing fields are caught before any request; a generated password is valid", async () => {
    await openUsers();
    let calls = 0;
    server.use(
      http.post("*/api/users", () => {
        calls++;
        return HttpResponse.json(long, { status: 201 });
      }),
    );

    fireEvent.click(screen.getByRole("button", { name: "+ Thêm người dùng" }));
    fireEvent.click(screen.getByRole("button", { name: "Tạo tài khoản" }));

    expect(await screen.findByText("Nhập họ tên.")).toBeTruthy();
    expect(screen.getByText("Chọn vai trò.")).toBeTruthy();
    expect(screen.getByText("Nhập mật khẩu tạm.")).toBeTruthy();
    expect(document.activeElement).toBe(screen.getByLabelText("Họ tên"));

    fireEvent.click(screen.getByRole("button", { name: "Tạo ngẫu nhiên" }));
    const generated = (screen.getByLabelText("Mật khẩu tạm") as HTMLInputElement).value;
    expect(generated).toMatch(/^(?=.*[a-zA-Z])(?=.*\d).{12}$/);
    expect(calls).toBe(0);
  });

  test("a taken email (409 DUPLICATE_EMAIL) is shown under the email field", async () => {
    await openUsers();
    server.use(
      http.post("*/api/users", () => problem(409, "DUPLICATE_EMAIL", "Email này đã được dùng cho một người dùng khác.")),
    );

    fireEvent.click(screen.getByRole("button", { name: "+ Thêm người dùng" }));
    fillForm({ "Họ tên": "Lê Thị An", Email: long.email, role: "RECRUITER", "Mật khẩu tạm": "Mat-khau-2026" });
    fireEvent.click(screen.getByRole("button", { name: "Tạo tài khoản" }));

    expect(await screen.findByText("Email này đã được dùng cho một người dùng khác.")).toBeTruthy();
    expect(document.activeElement).toBe(screen.getByLabelText("Email"));
    expect(screen.getByRole("dialog")).toBeTruthy();
  });

  test("editing can issue a new temporary password", async () => {
    await openUsers();
    let body: unknown;
    server.use(
      http.put("*/api/users/:userId", async ({ request }) => {
        body = await request.json();
        return HttpResponse.json(long);
      }),
    );

    fireEvent.click(within(rowOf("Trần Đức Long")).getByRole("button", { name: "Thao tác với Trần Đức Long" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Sửa" }));
    expect((screen.getByLabelText("Email") as HTMLInputElement).value).toBe(long.email);
    expect(screen.queryByLabelText("Mật khẩu tạm")).toBeNull();
    fireEvent.click(screen.getByLabelText("Cấp mật khẩu tạm mới (người dùng quên mật khẩu)"));
    fillForm({ "Mật khẩu tạm": "Mat-khau-moi-2026" });
    fireEvent.click(screen.getByRole("button", { name: "Lưu thay đổi" }));

    expect(await screen.findByText("Đã lưu và cấp mật khẩu tạm mới.")).toBeTruthy();
    expect(body).toMatchObject({
      email: long.email,
      role: "INTERVIEWER",
      phone: "0901234567",
      temporaryPassword: "Mat-khau-moi-2026",
    });
  });

  test("locking another user asks for confirmation; one's own account cannot be locked", async () => {
    await openUsers();
    let body: unknown;
    server.use(
      http.put("*/api/users/:userId/status", async ({ request, params }) => {
        body = { ...(await request.json() as object), userId: params.userId };
        return HttpResponse.json({ ...long, isActive: false });
      }),
    );

    fireEvent.click(screen.getByRole("button", { name: "Thao tác với Đỗ Quang Huy" }));
    expect(screen.queryByRole("menuitem", { name: "Khoá tài khoản" })).toBeNull();
    fireEvent.keyDown(document, { key: "Escape" });

    fireEvent.click(screen.getByRole("button", { name: "Thao tác với Trần Đức Long" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Khoá tài khoản" }));
    const dialog = screen.getByRole("dialog", { name: "Khoá tài khoản?" });
    fireEvent.click(within(dialog).getByRole("button", { name: "Khoá tài khoản" }));

    expect(await screen.findByText("Đã khoá tài khoản Trần Đức Long.")).toBeTruthy();
    expect(body).toEqual({ isActive: false, userId: long.id });
  });

  test("an empty list has its own message", async () => {
    server.use(http.get("*/api/users", () => HttpResponse.json(page([]))));
    signIn("ADMIN", "Đỗ Quang Huy");
    renderApp("/users");
    expect(await screen.findByText("Chưa có người dùng nào.")).toBeTruthy();
  });

  test("other roles do not see the page", async () => {
    signIn("HR_MANAGER");
    renderApp("/users");

    expect(await screen.findByRole("heading", { name: "Bạn không có quyền truy cập trang này" })).toBeTruthy();
    expect(screen.queryByRole("link", { name: "Người dùng" })).toBeNull();
  });
});
