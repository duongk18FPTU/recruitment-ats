import { Outlet } from "react-router";
import { useAuth } from "../../features/auth";
import { BreadcrumbProvider } from "./breadcrumb";
import { Sidebar } from "./Sidebar";
import { Topbar } from "./Topbar";
import "./layout.css";

/** Sidebar + topbar around every signed-in page (rendered inside RequireAuth). */
export function AppShell() {
  const auth = useAuth();
  if (auth.status !== "signed-in") {
    return null;
  }

  return (
    <BreadcrumbProvider>
      <div className="shell">
        <Sidebar user={auth.user} />
        <div className="shell__main">
          <Topbar />
          <main className="shell__content">
            <Outlet />
          </main>
        </div>
      </div>
    </BreadcrumbProvider>
  );
}
