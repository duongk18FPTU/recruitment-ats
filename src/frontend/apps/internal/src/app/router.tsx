import type { ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router";
import { AppShell } from "../components/layout/AppShell";
import { ChangePasswordPage, LoginPage, RequireAuth, RequireRole } from "../features/auth";
import { PositionDetailPage, PositionsPage } from "../features/positions";
import { UsersPage } from "../features/users";
import { ComingSoonPage } from "../pages/ComingSoonPage";
import { ForbiddenPage } from "../pages/ForbiddenPage";
import { pages } from "./navigation";

/** Built pages by path; every other page of the IA shows ComingSoonPage until its story is done. */
const builtPages: Partial<Record<string, ReactNode>> = {
  "/jobs": <PositionsPage />,
  "/users": <UsersPage />,
};

/** Pages below a top-level page; they take the roles of the page at their `parent` path. */
const subPages: { path: string; parent: string; element: ReactNode }[] = [
  { path: "/jobs/:positionId", parent: "/jobs", element: <PositionDetailPage /> },
];

/** Hash routes of docs/04-information-architecture.md ("Routes"). */
export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/change-password"
        element={
          <RequireAuth allowPendingPasswordChange>
            <ChangePasswordPage />
          </RequireAuth>
        }
      />
      <Route
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        {pages.map((page) => (
          <Route
            key={page.path}
            path={page.path}
            element={
              <RequireRole roles={page.roles}>
                {builtPages[page.path] ?? <ComingSoonPage title={page.title} />}
              </RequireRole>
            }
          />
        ))}
        {subPages.map((sub) => (
          <Route
            key={sub.path}
            path={sub.path}
            element={<RequireRole roles={pages.find((page) => page.path === sub.parent)!.roles}>{sub.element}</RequireRole>}
          />
        ))}
        <Route path="/403" element={<ForbiddenPage />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Route>
    </Routes>
  );
}
