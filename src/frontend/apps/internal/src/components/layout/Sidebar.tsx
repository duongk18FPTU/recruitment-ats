import type { UserProfile } from "@ats/api-client";
import { Link, useLocation } from "react-router";
import { roleLabels, sidebarFor, type AppPage } from "../../app/navigation";
import { LogoutButton } from "../../features/auth";
import { initials } from "../initials";
import { Logo } from "./Logo";

export function Sidebar({ user }: { user: UserProfile }) {
  const { pathname } = useLocation();
  const isActive = (page: AppPage) => {
    const root = page.section ?? page.path;
    return pathname === root || pathname.startsWith(`${root}/`);
  };

  return (
    <aside className="sidebar">
      <Link to="/dashboard" className="sidebar__brand">
        <Logo />
        <span>
          Recruitment <strong>ATS</strong>
        </span>
      </Link>

      <nav aria-label="Điều hướng chính" className="sidebar__nav">
        {sidebarFor(user.role).map((section) => (
          <div key={section.group} className="sidebar__section">
            <h2 className="sidebar__group">{section.group}</h2>
            <ul>
              {section.items.map((page) => (
                <li key={page.path}>
                  <Link
                    to={page.path}
                    className="sidebar__link"
                    aria-current={isActive(page) ? "page" : undefined}
                  >
                    {page.navLabel}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </nav>

      <div className="sidebar__user">
        <Link to="/profile" className="sidebar__profile" aria-label={`Hồ sơ cá nhân: ${user.fullName}`}>
          <span className="sidebar__avatar" aria-hidden="true">
            {initials(user.fullName)}
          </span>
          <span>
            <span className="sidebar__user-name">{user.fullName}</span>
            <span className="sidebar__user-role">{roleLabels[user.role]}</span>
          </span>
        </Link>
        <LogoutButton variant="icon" className="sidebar__logout" />
      </div>
    </aside>
  );
}
