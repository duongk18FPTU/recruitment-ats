import { apiClient, authSession, refreshSession, unwrap, type components } from "@ats/api-client";

type LoginRequest = components["schemas"]["LoginRequest"];
type ChangePasswordRequest = components["schemas"]["ChangePasswordRequest"];

/** `login`: stores the session (token in memory; the refresh cookie is set by the API). */
export async function login(body: LoginRequest) {
  const session = unwrap(await apiClient.POST("/api/auth/login", { body }));
  authSession.set(session);
  return session;
}

/**
 * `changePassword`, then `refreshToken`: the new access token no longer carries the
 * first-login restriction, and the profile has `mustChangePassword: false`.
 */
export async function changePassword(body: ChangePasswordRequest) {
  unwrap(await apiClient.PUT("/api/me/password", { body }));
  return refreshSession();
}

/**
 * `logout` (US02). The local session is always cleared, even when the call fails (offline,
 * expired session): the user must never stay signed in on this client after choosing to leave.
 */
export async function logout() {
  try {
    await apiClient.POST("/api/auth/logout");
  } finally {
    authSession.set(null);
  }
}
