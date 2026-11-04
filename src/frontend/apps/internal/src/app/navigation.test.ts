import type { Role } from "@ats/api-client";
import { describe, expect, test } from "vitest";
import { pageAt, sidebarFor } from "./navigation";

// docs/04-information-architecture.md, "Navigation by role" (profile is the user block, not a menu item).
const expected: Record<Role, string[]> = {
  ADMIN: ["Tổng quan", "Vị trí tuyển dụng", "Ứng viên", "Lịch phỏng vấn", "Hộp thư CV", "Kho ứng viên", "Nhập hàng loạt", "Báo cáo", "Nhật ký hoạt động", "Người dùng", "Cài đặt"],
  HR_MANAGER: ["Tổng quan", "Vị trí tuyển dụng", "Ứng viên", "Lịch phỏng vấn", "Hộp thư CV", "Kho ứng viên", "Nhập hàng loạt", "Báo cáo", "Nhật ký hoạt động", "Cài đặt"],
  RECRUITER: ["Tổng quan", "Vị trí tuyển dụng", "Ứng viên", "Lịch phỏng vấn", "Hộp thư CV", "Kho ứng viên", "Nhập hàng loạt"],
  INTERVIEWER: ["Tổng quan", "Vị trí tuyển dụng", "Ứng viên", "Lịch phỏng vấn"],
};

describe("sidebarFor", () => {
  test.each(Object.entries(expected))("%s sees exactly the menus of the IA table", (role, labels) => {
    const items = sidebarFor(role as Role).flatMap((section) => section.items.map((page) => page.navLabel));

    expect(items).toEqual(labels);
  });

  test("groups follow the Figma sidebar", () => {
    expect(sidebarFor("ADMIN").map((section) => section.group)).toEqual(["Tuyển dụng", "Nguồn hồ sơ", "Quản trị"]);
  });
});

describe("pageAt", () => {
  test("finds the page of nested paths", () => {
    expect(pageAt("/jobs/0190a1b2/pipeline")?.title).toBe("Vị trí tuyển dụng");
    expect(pageAt("/settings/mailbox")?.title).toBe("Hộp thư & người gửi");
    expect(pageAt("/jobsite")).toBeUndefined();
  });
});
