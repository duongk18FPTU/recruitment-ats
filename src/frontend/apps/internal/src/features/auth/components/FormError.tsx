import { ApiError } from "@ats/api-client";

/** The API's Vietnamese detail; unexpected errors also show the traceId for support (UI state contract). */
export function FormError({ error }: { error: Error }) {
  const apiError = error instanceof ApiError ? error : undefined;
  return (
    <div role="alert" className="form-error">
      <p>{error.message}</p>
      {apiError && apiError.status >= 500 && apiError.traceId && (
        <p className="form-error__trace">Mã lỗi: {apiError.traceId}</p>
      )}
    </div>
  );
}
