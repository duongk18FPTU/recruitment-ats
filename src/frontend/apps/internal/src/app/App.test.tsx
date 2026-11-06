import { ApiError, authSession } from "@ats/api-client";
import { act, screen, within } from "@testing-library/react";
import { describe, expect, test, vi } from "vitest";
import { renderApp, signIn } from "../test/renderApp";
import { createQueryClient } from "./providers";

const heading = () => screen.findByRole("heading", { level: 1 });
const sidebar = () => screen.getByRole("navigation", { name: "Điều hướng chính" });

describe("authentication", () => {
  test("shows a loading state while the session is restored", async () => {
    renderApp("/jobs", () => new Promise(() => {}));

    expect(await screen.findByRole("status")).toHaveProperty("textContent", "Đang tải…");
  });

  test("an anonymous visitor is sent to Login", async () => {
    renderApp("/jobs");

    expect((await heading()).textContent).toBe("Đăng nhập");
  });

  test("a session restored from the refresh cookie opens the requested page", async () => {
    renderApp("/jobs", async () => signIn("RECRUITER"));

    expect((await heading()).textContent).toBe("Vị trí tuyển dụng");
  });

  test("a signed-in user opening Login goes to the Dashboard", async () => {
    signIn("RECRUITER");
    renderApp("/login");

    expect((await heading()).textContent).toBe("Tổng quan");
  });

  test("losing the session (refused refresh) returns to Login", async () => {
    signIn("RECRUITER");
    renderApp("/applications");
    await heading();

    act(() => authSession.set(null));

    expect((await heading()).textContent).toBe("Đăng nhập");
  });
});

describe("app shell", () => {
  test("shows the sidebar for the role, the active page, the breadcrumb, and the user", async () => {
    signIn("HR_MANAGER");
    renderApp("/");

    expect((await heading()).textContent).toBe("Tổng quan");
    const nav = sidebar();
    expect(within(nav).getByRole("link", { name: "Tổng quan" }).getAttribute("aria-current")).toBe("page");
    expect(within(nav).getByRole("link", { name: "Cài đặt" }).getAttribute("href")).toBe("/settings/email-templates");
    expect(within(nav).queryByRole("link", { name: "Người dùng" })).toBeNull();
    expect(screen.getByRole("navigation", { name: "Breadcrumb" }).textContent).toBe("Tuyển dụngTổng quan");
    expect(screen.getByRole("link", { name: "Hồ sơ cá nhân: Phạm Thu Hà" }).textContent).toContain("Trưởng tuyển dụng");
  });

  test("settings pages use the Figma breadcrumb Quản trị / Cài đặt", async () => {
    signIn("ADMIN");
    renderApp("/settings/mailbox");
    await heading();

    expect(screen.getByRole("navigation", { name: "Breadcrumb" }).textContent).toBe("Quản trịCài đặt");
  });

  test("the user block shows initials of the last two words", async () => {
    signIn("RECRUITER", "Trần Đức Long");
    renderApp("/dashboard");
    await heading();

    expect(screen.getByRole("link", { name: /Hồ sơ cá nhân/ }).textContent).toContain("ĐL");
  });
});

describe("authorization", () => {
  test.each([
    ["INTERVIEWER", "/inbox"],
    ["RECRUITER", "/reports"],
    ["HR_MANAGER", "/users"],
    ["HR_MANAGER", "/settings/mailbox"],
  ] as const)("%s opening %s by URL sees 403 and stays signed in", async (role, path) => {
    signIn(role);
    renderApp(path);

    expect((await heading()).textContent).toBe("Bạn không có quyền truy cập trang này");
    expect(screen.getByRole("link", { name: "Về trang Tổng quan" }).getAttribute("href")).toBe("/dashboard");
    expect(sidebar()).toBeTruthy();
  });

  test("an Admin may open the mailbox settings", async () => {
    signIn("ADMIN");
    renderApp("/settings/mailbox");

    expect((await heading()).textContent).toBe("Hộp thư & người gửi");
    expect(within(sidebar()).getByRole("link", { name: "Cài đặt" }).getAttribute("aria-current")).toBe("page");
  });
});

describe("query client", () => {
  test("a 403 from the API opens the forbidden page", async () => {
    const onForbidden = vi.fn();
    const client = createQueryClient(onForbidden);

    await client
      .fetchQuery({ queryKey: ["positions"], queryFn: () => Promise.reject(new ApiError(403)) })
      .catch(() => undefined);

    expect(onForbidden).toHaveBeenCalledTimes(1);
  });

  test("client errors are not retried, server errors are", () => {
    const retry = createQueryClient(() => {}).getDefaultOptions().queries?.retry as (count: number, error: Error) => boolean;

    expect(retry(0, new ApiError(404))).toBe(false);
    expect(retry(0, new ApiError(503))).toBe(true);
    expect(retry(2, new ApiError(503))).toBe(false);
  });
});
