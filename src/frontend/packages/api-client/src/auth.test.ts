import createClient from "openapi-fetch";
import { afterEach, describe, expect, test, vi } from "vitest";
import { authSession, createAuthMiddleware, refreshSession, type AuthResponse } from "./auth";
import type { paths } from "./schema";

const BASE = "http://ats.test";

function session(token: string): AuthResponse {
  return {
    accessToken: token,
    expiresAt: "2026-11-04T10:15:00Z",
    user: { id: "0190a1b2-0000-7000-8000-000000000001", email: "ha@example.test", fullName: "Phạm Thu Hà", role: "HR_MANAGER", timezone: "Asia/Ho_Chi_Minh", mustChangePassword: false },
  };
}

function json(status: number, body?: unknown): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" },
  });
}

/** A fake API: `/api/auth/refresh` answers with `refresh()`, everything else with `handle()`. */
function fakeApi(handle: (request: Request) => Response, refresh: () => Response) {
  const calls: Request[] = [];
  const fetchImpl = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const request = input instanceof Request ? input : new Request(new URL(String(input), BASE), init);
    calls.push(request);
    return new URL(request.url).pathname === "/api/auth/refresh" ? refresh() : handle(request);
  });
  const client = createClient<paths>({ baseUrl: BASE, fetch: (request) => fetchImpl(request) });
  client.use(createAuthMiddleware(fetchImpl));
  return { client, calls };
}

const bearer = (request: Request) => request.headers.get("Authorization");

afterEach(() => authSession.set(null));

describe("auth middleware", () => {
  test("sends the access token", async () => {
    authSession.set(session("token-1"));
    const api = fakeApi(() => json(200, []), () => json(401));

    await api.client.GET("/api/lookups/departments");

    expect(bearer(api.calls[0]!)).toBe("Bearer token-1");
  });

  test("on 401 refreshes once and retries with the new token", async () => {
    authSession.set(session("expired"));
    const api = fakeApi(
      (request) => (bearer(request) === "Bearer fresh" ? json(200, [{ id: "d1", name: "Phòng Nhân sự" }]) : json(401)),
      () => json(200, session("fresh")),
    );

    const { data, response } = await api.client.GET("/api/lookups/departments");

    expect(response.status).toBe(200);
    expect(data).toEqual([{ id: "d1", name: "Phòng Nhân sự" }]);
    expect(authSession.get()?.accessToken).toBe("fresh");
    expect(api.calls.map((c) => new URL(c.url).pathname)).toEqual([
      "/api/lookups/departments",
      "/api/auth/refresh",
      "/api/lookups/departments",
    ]);
  });

  test("when the refresh is refused, returns the 401 and clears the session", async () => {
    authSession.set(session("expired"));
    const api = fakeApi(() => json(401), () => json(401));

    const { response } = await api.client.GET("/api/lookups/departments");

    expect(response.status).toBe(401);
    expect(authSession.get()).toBeNull();
  });

  test("parallel 401s share one refresh", async () => {
    authSession.set(session("expired"));
    const api = fakeApi(
      (request) => (bearer(request) === "Bearer fresh" ? json(200, []) : json(401)),
      () => json(200, session("fresh")),
    );

    await Promise.all([api.client.GET("/api/lookups/departments"), api.client.GET("/api/lookups/departments")]);

    expect(api.calls.filter((c) => c.url.endsWith("/api/auth/refresh"))).toHaveLength(1);
  });

  test("a wrong password on login is not retried", async () => {
    const api = fakeApi(() => json(401, { errorCode: "INVALID_CREDENTIALS", status: 401 }), () => json(200, session("x")));

    const { response } = await api.client.POST("/api/auth/login", { body: { email: "a@example.test", password: "wrong" } });

    expect(response.status).toBe(401);
    expect(api.calls).toHaveLength(1);
  });
});

describe("refreshSession", () => {
  test("restores the session from the refresh cookie", async () => {
    const fetchImpl = vi.fn(async () => json(200, session("restored")));

    await refreshSession(fetchImpl);

    expect(fetchImpl).toHaveBeenCalledWith("/api/auth/refresh", { method: "POST", credentials: "same-origin" }); // node: no location
    expect(authSession.get()?.user.fullName).toBe("Phạm Thu Hà");
  });

  test("a network error leaves the user signed out", async () => {
    authSession.set(session("old"));

    await refreshSession(async () => {
      throw new TypeError("Failed to fetch");
    });

    expect(authSession.get()).toBeNull();
  });
});
