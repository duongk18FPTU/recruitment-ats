import { describe, expect, test } from "vitest";
import { formatDate, statusTag, todayIn } from "./format";

describe("positions format", () => {
  test("a calendar date is shown as dd/MM/yyyy without a time zone shift", () => {
    expect(formatDate("2026-11-01")).toBe("01/11/2026");
  });

  test("an open position within 14 days of its target date is due soon", () => {
    expect(statusTag("OPEN", "2026-11-20", "2026-11-06")).toEqual({ label: "Sắp đến hạn", tone: "warning" });
    expect(statusTag("OPEN", "2026-11-21", "2026-11-06")).toEqual({ label: "Đang tuyển", tone: "active" });
    expect(statusTag("CLOSED", "2026-11-07", "2026-11-06")).toEqual({ label: "Đã đóng", tone: "muted" });
    expect(statusTag("DRAFT", "2026-11-07", "2026-11-06")).toEqual({ label: "Nháp", tone: "muted" });
  });

  test("today is the calendar date in the viewer's time zone", () => {
    expect(todayIn("Asia/Ho_Chi_Minh", new Date("2026-11-05T18:30:00Z"))).toBe("2026-11-06");
  });
});
