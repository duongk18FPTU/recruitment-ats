import type { ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router";
import { AppShell } from "../components/layout/AppShell";
import { ChangePasswordPage, LoginPage, RequireAuth, RequireRole } from "../features/auth";
import { UsersPage } from "../features/users";
import { ComingSoonPage } from "../pages/ComingSoonPage";
import { ForbiddenPage } from "../pages/ForbiddenPage";
import { pages } from "./navigation";

/** Built pages by path; every other page of the IA shows ComingSoonPage until its story is done. */
const builtPages: Partial<Record<string, ReactNode>> = {
  "/users": <UsersPage />,
};

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
        <Route path="/403" element={<ForbiddenPage />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Route>
    </Routes>
  );
}
