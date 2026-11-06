import { Link, useLocation } from "react-router";
import { pageAt } from "../../app/navigation";
import { useBreadcrumbItemLabel } from "./breadcrumb";

/**
 * Breadcrumb, as in every Figma frame. On a top-level page: its sidebar group, then its title
 * ("Tuyển dụng / Vị trí tuyển dụng"). Below it: the page's title as a link back, then the record the
 * page names with `useBreadcrumbItem` ("Vị trí tuyển dụng / Backend Developer (.NET)",
 * "Ứng viên / Lê Hoàng Nam", "Lịch phỏng vấn / Phiếu đánh giá").
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
                <li aria-current="page">{page.crumb ?? page.title}</li>
              </>
            )}
          </ol>
        </nav>
      )}
    </header>
  );
}
