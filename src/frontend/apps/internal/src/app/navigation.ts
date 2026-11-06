import type { Role } from "@ats/api-client";

export type NavGroup = "Tuyển dụng" | "Nguồn hồ sơ" | "Quản trị" | "Tài khoản";

export interface AppPage {
  path: string;
  /** Page title, and the breadcrumb's unless `crumb` is set. */
  title: string;
  /** Breadcrumb title when Figma names the page differently (both settings pages: "Cài đặt"). */
  crumb?: string;
  group: NavGroup;
  /** Roles that may open the page (IA "Navigation by role"; mirrors `x-roles`, never authoritative). */
  roles: readonly Role[];
  /** Sidebar label, when the page has its own sidebar item. */
  navLabel?: string;
  /** Sidebar item stays active for every path under this prefix. */
  section?: string;
}

const ALL: readonly Role[] = ["ADMIN", "HR_MANAGER", "RECRUITER", "INTERVIEWER"];
const HIRING_TEAM: readonly Role[] = ["ADMIN", "HR_MANAGER", "RECRUITER"];
const MANAGERS: readonly Role[] = ["ADMIN", "HR_MANAGER"];

/** Top-level pages of docs/04-information-architecture.md, in sidebar order. */
export const pages: readonly AppPage[] = [
  { path: "/dashboard", title: "Tổng quan", group: "Tuyển dụng", roles: ALL, navLabel: "Tổng quan" },
  { path: "/jobs", title: "Vị trí tuyển dụng", group: "Tuyển dụng", roles: ALL, navLabel: "Vị trí tuyển dụng" },
  { path: "/applications", title: "Ứng viên", group: "Tuyển dụng", roles: ALL, navLabel: "Ứng viên" },
  { path: "/interviews", title: "Lịch phỏng vấn", group: "Tuyển dụng", roles: ALL, navLabel: "Lịch phỏng vấn" },
  { path: "/inbox", title: "Hộp thư CV", group: "Nguồn hồ sơ", roles: HIRING_TEAM, navLabel: "Hộp thư CV" },
  { path: "/talent-pool", title: "Kho ứng viên", group: "Nguồn hồ sơ", roles: HIRING_TEAM, navLabel: "Kho ứng viên" },
  { path: "/import", title: "Nhập hàng loạt", group: "Nguồn hồ sơ", roles: HIRING_TEAM, navLabel: "Nhập hàng loạt" },
  { path: "/reports", title: "Báo cáo", group: "Quản trị", roles: MANAGERS, navLabel: "Báo cáo" },
  { path: "/audit", title: "Nhật ký hoạt động", group: "Quản trị", roles: MANAGERS, navLabel: "Nhật ký hoạt động" },
  { path: "/users", title: "Người dùng", group: "Quản trị", roles: ["ADMIN"], navLabel: "Người dùng" },
  {
    path: "/settings/email-templates",
    title: "Mẫu email",
    group: "Quản trị",
    roles: MANAGERS,
    navLabel: "Cài đặt",
    section: "/settings",
    crumb: "Cài đặt",
  },
  { path: "/settings/mailbox", title: "Hộp thư & người gửi", group: "Quản trị", roles: ["ADMIN"], crumb: "Cài đặt" },
  { path: "/profile", title: "Hồ sơ cá nhân", group: "Tài khoản", roles: ALL },
];

export const roleLabels: Record<Role, string> = {
  ADMIN: "Quản trị viên",
  HR_MANAGER: "Trưởng tuyển dụng",
  RECRUITER: "Chuyên viên tuyển dụng",
  INTERVIEWER: "Người phỏng vấn",
};

export interface SidebarSection {
  group: NavGroup;
  items: AppPage[];
}

/** Sidebar for a role: items the role may not open are not rendered at all. */
export function sidebarFor(role: Role): SidebarSection[] {
  const sections: SidebarSection[] = [];
  for (const page of pages) {
    if (!page.navLabel || !page.roles.includes(role)) {
      continue;
    }
    const section = sections.find((s) => s.group === page.group);
    if (section) {
      section.items.push(page);
    } else {
      sections.push({ group: page.group, items: [page] });
    }
  }
  return sections;
}

export function pageAt(pathname: string): AppPage | undefined {
  return pages.find((page) => pathname === page.path || pathname.startsWith(`${page.path}/`));
}
