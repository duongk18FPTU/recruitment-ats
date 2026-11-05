import { describe, expect, test } from "vitest";
import { pageNumbers } from "../../components/Pagination";
import { formatLastLogin } from "./format";
import { generateTemporaryPassword } from "./validation";

describe("formatLastLogin", () => {
  // 2026-11-05 09:30 in Hà Nội (UTC+7).
  const now = new Date("2026-11-05T02:30:00Z");
  const zone = "Asia/Ho_Chi_Minh";

  test.each([
    ["2026-11-05T01:12:00Z", "Hôm nay 08:12"],
    ["2026-11-04T17:30:00Z", "Hôm nay 00:30"], // still yesterday in UTC
    ["2026-11-04T10:45:00Z", "Hôm qua 17:45"],
    ["2026-10-28T03:00:00Z", "28/10/2026"],
  ])("%s → %s", (iso, expected) => {
    expect(formatLastLogin(iso, now, zone)).toBe(expected);
  });

  test("a user who never logged in", () => {
    expect(formatLastLogin(null, now, zone)).toBe("Chưa đăng nhập");
  });
});

describe("pageNumbers", () => {
  test.each([
    [1, 1, [1]],
    [1, 3, [1, 2, 3]],
    [6, 12, [1, null, 5, 6, 7, null, 12]],
    [3, 12, [1, 2, 3, 4, null, 12]],
  ])("page %i of %i", (page, total, expected) => {
    expect(pageNumbers(page, total)).toEqual(expected);
  });
});

describe("generateTemporaryPassword", () => {
  test("always has a letter and a digit, even from unlucky random values", () => {
    const allZero = generateTemporaryPassword((n) => new Uint32Array(n));
    expect(allZero).toMatch(/^(?=.*[a-zA-Z])(?=.*\d).{12}$/);
  });
});
