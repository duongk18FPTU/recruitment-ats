import type { components } from "./schema";

export type ErrorCode = components["schemas"]["ErrorCode"];
export type ProblemDetails = components["schemas"]["ValidationProblemDetails"];

const GENERIC_DETAIL = "Đã xảy ra lỗi. Vui lòng thử lại sau.";

/** A failed API call as the UI needs it (docs/08-openapi-contract.md, "Errors"). */
export class ApiError extends Error {
  readonly status: number;
  readonly errorCode: ErrorCode;
  /** Field path (camelCase) → messages, for `VALIDATION_FAILED`. */
  readonly fieldErrors: Record<string, string[]>;
  readonly traceId: string | undefined;
  /** The full problem, for extensions such as `allowedStages` or `existingApplicationId`. */
  readonly problem: ProblemDetails | undefined;

  constructor(status: number, problem?: ProblemDetails) {
    super(problem?.detail || GENERIC_DETAIL);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
    this.errorCode = problem?.errorCode ?? codeOf(status);
    this.fieldErrors = problem?.errors ?? {};
    this.traceId = problem?.traceId;
  }
}

/** Builds an {@link ApiError} from a non-2xx response and its parsed body (if any). */
export function toApiError(response: Response, body: unknown): ApiError {
  return new ApiError(response.status, isProblem(body) ? body : undefined);
}

/** Returns the data of an openapi-fetch result, or throws its {@link ApiError}. */
export function unwrap<T>(result: { data?: T; error?: unknown; response: Response }): T {
  if (result.response.ok) {
    return result.data as T;
  }
  throw toApiError(result.response, result.error);
}

function isProblem(body: unknown): body is ProblemDetails {
  return typeof body === "object" && body !== null && "errorCode" in body && "status" in body;
}

// Same fallback as the API's ProblemDetailsMapper, for responses without a problem body (e.g. a proxy 502).
function codeOf(status: number): ErrorCode {
  switch (status) {
    case 401:
      return "UNAUTHENTICATED";
    case 403:
      return "FORBIDDEN";
    case 404:
      return "NOT_FOUND";
    case 413:
      return "FILE_TOO_LARGE";
    case 429:
      return "RATE_LIMITED";
    default:
      return status >= 400 && status < 500 ? "VALIDATION_FAILED" : "INTERNAL_ERROR";
  }
}
