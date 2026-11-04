// Compile-time checks for public.ts; run by `npm run typecheck`.
import type { PublicPaths } from "./public";

export const publicRoute: keyof PublicPaths = "/api/public/job-posts";

// @ts-expect-error internal endpoints are not part of the public client
export const internalRoute: keyof PublicPaths = "/api/auth/login";
