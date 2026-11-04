import { authSession, type Role } from "@ats/api-client";
import { render } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { AppProviders } from "../app/providers";
import { AppRoutes } from "../app/router";

/** Realistic signed-in user (docs/05-ui-ux.md: no placeholder names). */
export function signIn(role: Role, fullName = "Phạm Thu Hà") {
  authSession.set({
    accessToken: "test-token",
    expiresAt: "2026-11-04T10:15:00Z",
    user: { id: "0190a1b2-0000-7000-8000-000000000001", email: "ha.pham@example.test", fullName, role, timezone: "Asia/Ho_Chi_Minh" },
  });
}

/** The whole app at `path`, with the session restore replaced (no network). */
export function renderApp(path: string, restoreSession: () => Promise<unknown> = async () => null) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AppProviders restoreSession={restoreSession}>
        <AppRoutes />
      </AppProviders>
    </MemoryRouter>,
  );
}
