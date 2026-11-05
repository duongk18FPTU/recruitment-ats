import type { Middleware } from "openapi-fetch";
import type { components } from "./schema";

export type AuthResponse = components["schemas"]["AuthResponse"];
export type UserProfile = components["schemas"]["UserProfile"];
export type Role = components["schemas"]["Role"];

type Listener = (session: AuthResponse | null) => void;

let current: AuthResponse | null = null;
let refreshing: Promise<AuthResponse | null> | null = null;
const listeners = new Set<Listener>();

/**
 * The signed-in session. The access token lives in memory only (AD-06); the refresh token is
 * the `httpOnly` cookie, so a reload restores the session with {@link refreshSession}.
 */
export const authSession = {
  get: (): AuthResponse | null => current,
  set(next: AuthResponse | null): void {
    current = next;
    listeners.forEach((listener) => listener(next));
  },
  subscribe(listener: Listener): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
};

/**
 * Calls `refreshToken` (cookie) and stores the new session, or clears it when the refresh is
 * refused. Concurrent callers share one request, because each refresh rotates the cookie.
 */
export function refreshSession(fetchImpl: typeof fetch = defaultFetch): Promise<AuthResponse | null> {
  refreshing ??= (async () => {
    try {
      const response = await fetchImpl(`${apiOrigin()}/api/auth/refresh`, { method: "POST", credentials: "same-origin" });
      const next = response.ok ? ((await response.json()) as AuthResponse) : null;
      authSession.set(next);
      return next;
    } catch {
      authSession.set(null);
      return null;
    } finally {
      refreshing = null;
    }
  })();
  return refreshing;
}

/**
 * Adds the bearer token to every request. On a 401 it refreshes once and retries the request;
 * if the refresh fails the session is cleared and the 401 is returned (the app goes to Login).
 */
export function createAuthMiddleware(fetchImpl: typeof fetch = defaultFetch): Middleware {
  const retryable = new Map<string, Request>();

  return {
    onRequest({ request, id }) {
      const token = current?.accessToken;
      if (token) {
        request.headers.set("Authorization", `Bearer ${token}`);
      }
      if (!isTokenEndpoint(request.url)) {
        retryable.set(id, request.clone());
      }
      return request;
    },
    async onResponse({ response, id }) {
      const original = retryable.get(id);
      retryable.delete(id);
      if (response.status !== 401 || !original) {
        return response;
      }

      const session = await refreshSession(fetchImpl);
      if (!session) {
        return response;
      }
      original.headers.set("Authorization", `Bearer ${session.accessToken}`);
      return fetchImpl(original);
    },
    onError({ id }) {
      retryable.delete(id);
    },
  };
}

// A wrong password on login or a refused refresh is a real 401, not an expired token.
function isTokenEndpoint(url: string): boolean {
  return /\/api\/auth\/(login|refresh)$/.test(new URL(url, "http://localhost").pathname);
}

/** The page's own origin: the API is same-origin (AD-05); absolute URLs also work outside a browser. */
export function apiOrigin(): string {
  return typeof location === "undefined" ? "" : location.origin;
}

function defaultFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  return globalThis.fetch(input, init);
}
