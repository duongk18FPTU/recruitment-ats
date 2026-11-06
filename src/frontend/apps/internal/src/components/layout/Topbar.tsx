import { Link, useLocation } from "react-router";
import { pageAt } from "../../app/navigation";
import { useBreadcrumbItemLabel } from "./breadcrumb";

/**
 * Breadcrumb. On a top-level page: its sidebar group, then its title. Below it (a position, a new
 * position): the page's title as a link back to it, then the record the page names (SCR-13 in Figma).
 */
export function Topbar() {
  const { pathname } = useLocation();
  const page = pageAt(pathname);
  const item = useBreadcrumbItemLabel();
  const nested = page !== undefined && pathname !== page.path;

  return (
    <header className="topbar">
      {page && (
        <nav aria-label="Breadcrumb">
          <ol className="breadcrumb">
            {nested ? (
              <>
                <li>
                  <Link to={page.path}>{page.title}</Link>
                </li>
                <li aria-current="page">{item ?? "…"}</li>
              </>
            ) : (
              <>
                <li>{page.group}</li>
                <li aria-current="page">{page.title}</li>
              </>
            )}
          </ol>
        </nav>
      )}
    </header>
  );
}
