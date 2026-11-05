import createClient from "openapi-fetch";
import { apiOrigin, createAuthMiddleware } from "./auth";
import type { paths } from "./schema";

/** Typed client for the internal app; calls are same-origin (`/api/*`) and carry the access token. */
// fetch is looked up per call, so test doubles (MSW) installed after import still apply.
export const apiClient = createClient<paths>({ baseUrl: apiOrigin(), fetch: (request) => globalThis.fetch(request) });
apiClient.use(createAuthMiddleware());
