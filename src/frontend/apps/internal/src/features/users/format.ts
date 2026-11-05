const DAY_MS = 24 * 60 * 60 * 1000;

/**
 * SCR-70 "Đăng nhập gần nhất" in the viewer's time zone: "Hôm nay 08:12", "Hôm qua 17:45",
 * otherwise "28/10/2026".
 */
export function formatLastLogin(iso: string | null | undefined, now: Date, timeZone: string): string {
  if (!iso) {
    return "Chưa đăng nhập";
  }
  const date = new Date(iso);
  const dayOf = (d: Date) =>
    new Intl.DateTimeFormat("en-CA", { timeZone, year: "numeric", month: "2-digit", day: "2-digit" }).format(d);
  const time = new Intl.DateTimeFormat("vi-VN", { timeZone, hour: "2-digit", minute: "2-digit", hourCycle: "h23" });
  const at = time.format(date);

  if (dayOf(date) === dayOf(now)) {
    return `Hôm nay ${at}`;
  }
  if (dayOf(date) === dayOf(new Date(now.getTime() - DAY_MS))) {
    return `Hôm qua ${at}`;
  }
  return new Intl.DateTimeFormat("vi-VN", { timeZone, day: "2-digit", month: "2-digit", year: "numeric" }).format(date);
}
