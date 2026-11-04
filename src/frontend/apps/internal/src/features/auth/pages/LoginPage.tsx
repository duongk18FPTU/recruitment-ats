import { Navigate, useLocation, type Location } from "react-router";
import { useAuth } from "../AuthProvider";

/** SCR-01. The login form arrives with US01; signed-in users go back to where they were. */
export function LoginPage() {
  const auth = useAuth();
  const from = (useLocation().state as { from?: Location } | null)?.from;

  if (auth.status === "signed-in") {
    return <Navigate to={from ?? "/dashboard"} replace />;
  }

  return (
    <main className="login">
      <h1>Đăng nhập</h1>
    </main>
  );
}
