import createClient from "openapi-fetch";
import type { paths } from "./schema";

/** Typed client for the internal app; calls are same-origin (`/api/*`). */
export const apiClient = createClient<paths>({ baseUrl: "" });
