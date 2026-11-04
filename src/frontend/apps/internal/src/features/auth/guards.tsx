import type { Role } from "@ats/api-client";
import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router";
import { useAuth } from "./AuthProvider";

/** Signed-in users only; others go to Login and come back afterwards. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const location = useLocation();

  if (auth.status === "restoring") {
    return (
      <p role="status" className="page-status">
        Đang tải…
      </p>
    );
  }
  if (auth.status === "anonymous") {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }
  return children;
}

/** A role that may not open the page is sent to #/403 (it stays signed in). */
export function RequireRole({ roles, children }: { roles: readonly Role[]; children: ReactNode }) {
  const auth = useAuth();

  if (auth.status !== "signed-in" || !roles.includes(auth.user.role)) {
    return <Navigate to="/403" replace />;
  }
  return children;
}
