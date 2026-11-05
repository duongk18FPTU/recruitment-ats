import { fireEvent, screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, test } from "vitest";
import { server } from "../../../test/msw";
import { renderApp, signIn } from "../../../test/renderApp";

function fill(current: string, next: string, confirm = next) {
  fireEvent.change(screen.getByLabelText("Mật khẩu hiện tại"), { target: { value: current } });
  fireEvent.change(screen.getByLabelText("Mật khẩu mới"), { target: { value: next } });
  fireEvent.change(screen.getByLabelText("Nhập lại mật khẩu mới"), { target: { value: confirm } });
  fireEvent.click(screen.getByRole("button", { name: "Đổi mật khẩu" }));
}

async function openAsUserWithIssuedPassword() {
  signIn("RECRUITER", "Nguyễn Minh Anh", true);
  renderApp("/change-password");
  await screen.findByRole("heading", { name: "Đổi mật khẩu" });
}

describe("first-login password change", () => {
  test("a user with an issued password cannot open other pages", async () => {
    signIn("RECRUITER", "Nguyễn Minh Anh", true);
    renderApp("/jobs");

    expect(await screen.findByRole("heading", { level: 1, name: "Đổi mật khẩu" })).toBeTruthy();
    expect(screen.queryByRole("navigation", { name: "Điều hướng chính" })).toBeNull();
  });

  test("weak or mismatched passwords are caught before any request", async () => {
    let calls = 0;
    server.use(
      http.put("*/api/me/password", () => {
        calls++;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    await openAsUserWithIssuedPassword();

    fill("Mat-khau-tam-01", "chi-co-chu-thoi");
    expect(await screen.findByText("Mật khẩu mới phải có cả chữ và số.")).toBeTruthy();

    fill("Mat-khau-tam-01", "Mat-khau-moi-2026", "Mat-khau-khac-2026");
    expect(await screen.findByText("Mật khẩu nhập lại không khớp.")).toBeTruthy();
    expect(calls).toBe(0);
  });

  test("a wrong current password from the API is shown under its field", async () => {
    server.use(
      http.put("*/api/me/password", () =>
        HttpResponse.json(
          {
            type: "https://ats.example.vn/errors/validation-failed",
            title: "Validation failed",
            status: 400,
            detail: "Dữ liệu không hợp lệ.",
            errorCode: "VALIDATION_FAILED",
            traceId: "00-2",
            errors: { currentPassword: ["Mật khẩu hiện tại không đúng."] },
          },
          { status: 400 },
        ),
      ),
    );
    await openAsUserWithIssuedPassword();

    fill("Sai-mat-khau-1", "Mat-khau-moi-2026");

    expect(await screen.findByText("Mật khẩu hiện tại không đúng.")).toBeTruthy();
    expect(document.activeElement).toBe(screen.getByLabelText("Mật khẩu hiện tại"));
    expect(screen.queryByRole("alert")).toBeNull();
  });

  test("after the change the session is refreshed and the Dashboard opens", async () => {
    let body: unknown;
    server.use(
      http.put("*/api/me/password", async ({ request }) => {
        body = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
      http.post("*/api/auth/refresh", () =>
        HttpResponse.json({
          accessToken: "fresh",
          expiresAt: "2026-11-05T10:15:00Z",
          user: {
            id: "0190a1b2-0000-7000-8000-000000000001",
            email: "minhanh.nguyen@recruitment-ats.vn",
            fullName: "Nguyễn Minh Anh",
            role: "RECRUITER",
            timezone: "Asia/Ho_Chi_Minh",
            mustChangePassword: false,
          },
        }),
      ),
    );
    await openAsUserWithIssuedPassword();

    fill("Mat-khau-tam-01", "Mat-khau-moi-2026");

    expect(await screen.findByRole("heading", { level: 1, name: "Tổng quan" })).toBeTruthy();
    expect(body).toEqual({ currentPassword: "Mat-khau-tam-01", newPassword: "Mat-khau-moi-2026" });
  });

  test("a user who already changed the password is sent to the Dashboard", async () => {
    signIn("RECRUITER");
    renderApp("/change-password");

    expect(await screen.findByRole("heading", { level: 1, name: "Tổng quan" })).toBeTruthy();
  });
});
