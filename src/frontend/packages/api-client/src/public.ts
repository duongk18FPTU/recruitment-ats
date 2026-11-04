import createClient from "openapi-fetch";
import type { paths } from "./schema";

/** Only the anonymous `/api/public/*` operations — the career site cannot reach internal endpoints. */
export type PublicPaths = Pick<paths, Extract<keyof paths, `/api/public/${string}`>>;

export const publicApiClient = createClient<PublicPaths>({ baseUrl: "" });
