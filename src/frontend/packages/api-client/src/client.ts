import createClient from "openapi-fetch";
import { createAuthMiddleware } from "./auth";
import type { paths } from "./schema";

/** Typed client for the internal app; calls are same-origin (`/api/*`) and carry the access token. */
export const apiClient = createClient<paths>({ baseUrl: "" });
apiClient.use(createAuthMiddleware());
