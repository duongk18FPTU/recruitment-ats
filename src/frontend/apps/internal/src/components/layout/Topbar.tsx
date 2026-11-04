import { useLocation } from "react-router";
import { pageAt } from "../../app/navigation";

export function Topbar() {
  const page = pageAt(useLocation().pathname);

  return (
    <header className="topbar">
      {page && (
        <nav aria-label="Breadcrumb">
          <ol className="breadcrumb">
            <li>{page.group}</li>
            <li aria-current="page">{page.title}</li>
          </ol>
        </nav>
      )}
    </header>
  );
}
