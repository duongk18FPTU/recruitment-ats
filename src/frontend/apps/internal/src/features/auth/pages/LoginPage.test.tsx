import { fireEvent, screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, test } from "vitest";
import { server } from "../../../test/msw";
import { renderApp } from "../../../test/renderApp";

const profile = (mustChangePassword = false) => ({
  id: "0190a1b2-0000-7000-8000-000000000001",
  email: "minhanh.nguyen@recruitment-ats.vn",
  fullName: "Nguyễn Minh Anh",
  role: "RECRUITER",
  timezone: "Asia/Ho_Chi_Minh",
  mustChangePassword,
});

function mockLogin(respond: () => Response) {
  const bodies: unknown[] = [];
  server.use(
    http.post("*/api/auth/login", async ({ request }) => {
      bodies.push(await request.json());
      return respond();
    }),
  );
  return bodies;
}

const ok = (mustChangePassword = false) => () =>
  HttpResponse.json({ accessToken: "token", expiresAt: "2026-11-05T10:15:00Z", user: profile(mustChangePassword) });

function fill(email: string, password: string) {
  fireEvent.change(screen.getByLabelText("Email công ty"), { target: { value: email } });
  fireEvent.change(screen.getByLabelText("Mật khẩu"), { target: { value: password } });
  fireEvent.click(screen.getByRole("button", { name: "Đăng nhập" }));
}

describe("SCR-01 login", () => {
  test("AC-US01 #3: empty fields show errors and send no request", async () => {
    const requests = mockLogin(ok());
    renderApp("/login");

    fill("", "");

    expect(await screen.findByText("Nhập email.")).toBeTruthy();
    expect(screen.getByText("Nhập mật khẩu.")).toBeTruthy();
    expect(document.activeElement).toBe(screen.getByLabelText("Email công ty"));
    expect(requests).toHaveLength(0);
  });

  test("AC-US01 #1: valid credentials open the Dashboard", async () => {
    const requests = mockLogin(ok());
    renderApp("/login");

    fill("  minhanh.nguyen@recruitment-ats.vn ", "Mat-khau-2026");

    expect(await screen.findByRole("heading", { level: 1, name: "Tổng quan" })).toBeTruthy();
    expect(requests).toEqual([{ email: "minhanh.nguyen@recruitment-ats.vn", password: "Mat-khau-2026" }]);
  });

  test("returns to the page that sent the user to Login", async () => {
    mockLogin(ok());
    renderApp("/applications");
    await screen.findByRole("heading", { name: "Đăng nhập" });

    fill("minhanh.nguyen@recruitment-ats.vn", "Mat-khau-2026");

    expect(await screen.findByRole("heading", { level: 1, name: "Ứng viên" })).toBeTruthy();
  });

  test("AC-US01 #2: wrong credentials show the generic message from the API", async () => {
    mockLogin(() =>
      HttpResponse.json(
        {
          type: "https://ats.example.vn/errors/invalid-credentials",
          title: "Invalid credentials",
          status: 401,
          detail: "Email hoặc mật khẩu không đúng.",
          errorCode: "INVALID_CREDENTIALS",
          traceId: "00-1",
        },
        { status: 401 },
      ),
    );
    renderApp("/login");

    fill("minhanh.nguyen@recruitment-ats.vn", "sai");

    expect((await screen.findByRole("alert")).textContent).toBe("Email hoặc mật khẩu không đúng.");
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe("Đăng nhập");
  });

  test("an issued password leads to the password change", async () => {
    mockLogin(ok(true));
    renderApp("/login");

    fill("minhanh.nguyen@recruitment-ats.vn", "Mat-khau-tam-01");

    expect(await screen.findByRole("heading", { level: 1, name: "Đổi mật khẩu" })).toBeTruthy();
  });

  test("the password can be shown", async () => {
    renderApp("/login");
    const input = await screen.findByLabelText("Mật khẩu");

    fireEvent.click(screen.getByRole("button", { name: "Hiện mật khẩu" }));

    expect(input.getAttribute("type")).toBe("text");
    expect(screen.getByRole("button", { name: "Ẩn mật khẩu" }).getAttribute("aria-pressed")).toBe("true");
  });
});
