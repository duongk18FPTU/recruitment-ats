import { authSession } from "@ats/api-client";
import { fireEvent, screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { describe, expect, test } from "vitest";
import { server } from "../../test/msw";
import { renderApp, signIn } from "../../test/renderApp";

function mockLogout(respond: () => Response) {
  const calls: { authorization: string | null }[] = [];
  server.use(
    http.post("*/api/auth/logout", ({ request }) => {
      calls.push({ authorization: request.headers.get("Authorization") });
      return respond();
    }),
  );
  return calls;
}

describe("US02 logout", () => {
  test("AC-US02 #1: logging out calls the API, clears the session, and shows Login", async () => {
    const calls = mockLogout(() => new HttpResponse(null, { status: 204 }));
    signIn("RECRUITER");
    renderApp("/dashboard");
    await screen.findByRole("heading", { level: 1, name: "Tổng quan" });

    fireEvent.click(screen.getByRole("button", { name: "Đăng xuất" }));

    expect(await screen.findByRole("heading", { level: 1, name: "Đăng nhập" })).toBeTruthy();
    expect(calls).toEqual([{ authorization: "Bearer test-token" }]);
    expect(authSession.get()).toBeNull();
  });

  test("the client signs out even when the API call fails", async () => {
    mockLogout(() => HttpResponse.error());
    signIn("RECRUITER");
    renderApp("/applications");
    await screen.findByRole("heading", { level: 1, name: "Ứng viên" });

    fireEvent.click(screen.getByRole("button", { name: "Đăng xuất" }));

    expect(await screen.findByRole("heading", { level: 1, name: "Đăng nhập" })).toBeTruthy();
    expect(authSession.get()).toBeNull();
  });

  test("a user who must change the password can log out instead", async () => {
    mockLogout(() => new HttpResponse(null, { status: 204 }));
    signIn("RECRUITER", "Nguyễn Minh Anh", true);
    renderApp("/change-password");
    await screen.findByRole("heading", { level: 1, name: "Đổi mật khẩu" });

    fireEvent.click(screen.getByRole("button", { name: "Đăng xuất" }));

    expect(await screen.findByRole("heading", { level: 1, name: "Đăng nhập" })).toBeTruthy();
  });

  test("AC-US02 #2: after logout, an internal URL leads to Login", async () => {
    renderApp("/jobs");

    expect(await screen.findByRole("heading", { level: 1, name: "Đăng nhập" })).toBeTruthy();
  });
});
