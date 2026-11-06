import type { components } from "@ats/api-client";
import type { PositionStatus } from "./api";

type EmploymentType = components["schemas"]["EmploymentType"];
type Level = components["schemas"]["Level"];
type CriterionType = components["schemas"]["CriterionType"];
type Stage = components["schemas"]["Stage"];

export const statusLabels: Record<PositionStatus, string> = {
  DRAFT: "Nháp",
  OPEN: "Đang tuyển",
  CLOSED: "Đã đóng",
};

export const employmentTypeLabels: Record<EmploymentType, string> = {
  FULL_TIME: "Toàn thời gian",
  PART_TIME: "Bán thời gian",
  CONTRACT: "Hợp đồng",
  INTERNSHIP: "Thực tập",
};

export const levelLabels: Record<Level, string> = {
  INTERN: "Thực tập sinh",
  FRESHER: "Fresher",
  JUNIOR: "Junior",
  MIDDLE: "Middle",
  SENIOR: "Senior",
  LEAD: "Lead",
  MANAGER: "Quản lý",
};

export const criterionTypeLabels: Record<CriterionType, string> = {
  MUST_HAVE: "Bắt buộc",
  NICE_TO_HAVE: "Cộng điểm",
};

/** Pipeline order (docs/11 "Application stage"); REJECTED last. */
export const pipelineStages: readonly Stage[] = ["NEW", "SCREENING", "INTERVIEW", "OFFER", "HIRED", "REJECTED"];

export const stageLabels: Record<Stage, string> = {
  NEW: "Mới",
  SCREENING: "Sàng lọc",
  INTERVIEW: "Phỏng vấn",
  OFFER: "Offer",
  HIRED: "Đã tuyển",
  REJECTED: "Từ chối",
};

/** An open position whose target hire date is this close shows "Sắp đến hạn" (display only). */
export const DUE_SOON_DAYS = 14;

/** `2026-11-01` → `01/11/2026` (a calendar date: no time zone conversion). */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.slice(0, 10).split("-");
  return `${day}/${month}/${year}`;
}

/** A point in time as dd/MM/yyyy in the viewer's time zone. */
export function formatDateTime(iso: string, timeZone: string): string {
  return new Intl.DateTimeFormat("vi-VN", { timeZone, day: "2-digit", month: "2-digit", year: "numeric" }).format(
    new Date(iso),
  );
}

/** Status tag of SCR-10: an open position near its target date is "Sắp đến hạn". */
export function statusTag(
  status: PositionStatus,
  targetHireDate: string,
  today: string,
): { label: string; tone: "active" | "warning" | "muted" } {
  if (status !== "OPEN") {
    return { label: statusLabels[status], tone: "muted" };
  }
  const daysLeft = (Date.parse(targetHireDate) - Date.parse(today)) / (24 * 60 * 60 * 1000);
  return daysLeft <= DUE_SOON_DAYS
    ? { label: "Sắp đến hạn", tone: "warning" }
    : { label: statusLabels.OPEN, tone: "active" };
}

/** Today's calendar date (yyyy-MM-dd) in a time zone. */
export function todayIn(timeZone: string, now = new Date()): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone, year: "numeric", month: "2-digit", day: "2-digit" }).format(now);
}
