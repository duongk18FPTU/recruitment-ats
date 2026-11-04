import { describe, expect, test } from "vitest";
import { ApiError, toApiError, unwrap } from "./problem";

describe("ApiError", () => {
  test("keeps the contract fields of a problem", () => {
    const problem = {
      type: "https://ats.example.vn/errors/validation-failed",
      title: "Validation failed",
      status: 400,
      detail: "Dữ liệu không hợp lệ.",
      errorCode: "VALIDATION_FAILED" as const,
      traceId: "00-abc-01",
      errors: { "criteria[0].weight": ["Trọng số phải từ 1 đến 5"] },
    };

    const error = toApiError(new Response(null, { status: 400 }), problem);

    expect(error).toBeInstanceOf(ApiError);
    expect(error.message).toBe("Dữ liệu không hợp lệ.");
    expect(error.errorCode).toBe("VALIDATION_FAILED");
    expect(error.traceId).toBe("00-abc-01");
    expect(error.fieldErrors["criteria[0].weight"]).toEqual(["Trọng số phải từ 1 đến 5"]);
  });

  test.each([
    [403, "FORBIDDEN"],
    [404, "NOT_FOUND"],
    [502, "INTERNAL_ERROR"],
  ])("a %i without a problem body gets a code and a generic message", (status, code) => {
    const error = toApiError(new Response("<html>Bad gateway</html>", { status }), "<html>Bad gateway</html>");

    expect(error.errorCode).toBe(code);
    expect(error.message).toBe("Đã xảy ra lỗi. Vui lòng thử lại sau.");
    expect(error.fieldErrors).toEqual({});
  });
});

describe("unwrap", () => {
  test("returns data of a successful call", () => {
    expect(unwrap({ data: [1, 2], response: new Response(null, { status: 200 }) })).toEqual([1, 2]);
  });

  test("throws the ApiError of a failed call", () => {
    const call = () =>
      unwrap({ error: { status: 409, errorCode: "CONCURRENCY_CONFLICT", detail: "Đã thay đổi" }, response: new Response(null, { status: 409 }) });

    expect(call).toThrow(ApiError);
    expect(call).toThrow("Đã thay đổi");
  });
});
