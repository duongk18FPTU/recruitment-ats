import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router";
import plusIcon from "../../../assets/icons/plus.svg";
import { useBreadcrumbItem } from "../../../components/layout/breadcrumb";
import { Pagination } from "../../../components/Pagination";
import { RowMenu, type RowAction } from "../../../components/RowMenu";
import { useAuth } from "../../auth";
import { useDepartments, useUserOptions } from "../../lookups";
import type { PositionQuery, PositionStatus, PositionSummary } from "../api";
import { formatDate, statusTag, todayIn } from "../format";
import { PositionFormModal } from "../components/PositionFormModal";
import { PositionStatusModal } from "../components/PositionStatusModal";
import { canCreatePosition, canEditPosition, canPickOwner, statusActionLabel, statusTargets } from "../permissions";
import { usePositions, useStatusCounts } from "../queries";
import "../positions.css";

const SEARCH_DELAY_MS = 300;
const OWNER_ROLES = ["ADMIN", "HR_MANAGER", "RECRUITER"] as const;

const tabs: { status: PositionStatus | undefined; label: string; key: "OPEN" | "DRAFT" | "CLOSED" | "ALL" }[] = [
  { status: "OPEN", label: "Đang tuyển", key: "OPEN" },
  { status: "DRAFT", label: "Nháp", key: "DRAFT" },
  { status: "CLOSED", label: "Đã đóng", key: "CLOSED" },
  { status: undefined, label: "Tất cả", key: "ALL" },
];

/**
 * SCR-10 Vị trí tuyển dụng (US04): status tabs with counts, search, department and owner filters, paging.
 * At `#/jobs/new` the SCR-11 create form opens over it (US05).
 */
export function PositionsPage({ creating = false }: { creating?: boolean }) {
  const navigate = useNavigate();
  useBreadcrumbItem(creating ? "Tạo vị trí" : undefined);
  const auth = useAuth();
  const me = auth.status === "signed-in" ? auth.user : undefined;
  const showOwnerFilter = me ? canPickOwner(me.role) : false;
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState<PositionQuery>({ status: "OPEN", page: 1, pageSize: 20 });
  const positions = usePositions(query);
  const [statusChange, setStatusChange] = useState<{ position: PositionSummary; target: PositionStatus } | null>(null);
  const counts = useStatusCounts({ q: query.q, departmentId: query.departmentId, ownerId: query.ownerId });
  const departments = useDepartments();
  const owners = useUserOptions({ roles: [...OWNER_ROLES] }, showOwnerFilter);

  // Typing filters after a short pause; every filter change starts again at page 1.
  useEffect(() => {
    const q = search.trim() || undefined;
    const apply = () => setQuery((current) => (current.q === q ? current : { ...current, q, page: 1 }));
    const timer = setTimeout(apply, SEARCH_DELAY_MS);
    return () => clearTimeout(timer);
  }, [search]);

  const filter = (change: Partial<PositionQuery>) => setQuery((current) => ({ ...current, ...change, page: 1 }));
  const today = todayIn(me?.timezone ?? "Asia/Ho_Chi_Minh");
  const filtered = Boolean(query.q || query.departmentId || query.ownerId || query.status);

  // Actions the viewer may take on a row; the API checks them again.
  const actionsFor = (position: PositionSummary): RowAction[] =>
    me
      ? [
          ...(canEditPosition(me, position)
            ? [{ label: "Sửa", onSelect: () => navigate(`/jobs/${position.id}/edit`) }]
            : []),
          ...statusTargets(me, position).map((target) => ({
            label: statusActionLabel(position.status, target),
            onSelect: () => setStatusChange({ position, target }),
          })),
        ]
      : [];

  return (
    <section className="positions">
      <header className="positions__header">
        <h1>Vị trí tuyển dụng</h1>
        {me && canCreatePosition(me.role) && (
          <button type="button" className="button button--primary button--icon" onClick={() => navigate("/jobs/new")}>
            <img src={plusIcon} width={16} height={16} alt="" />
            Tạo vị trí
          </button>
        )}
      </header>

      <div className="positions__card">
        <div className="positions__tabs" role="group" aria-label="Trạng thái vị trí">
          {tabs.map((tab) => (
            <button
              key={tab.key}
              type="button"
              className="positions__tab"
              aria-pressed={query.status === tab.status}
              onClick={() => filter({ status: tab.status })}
            >
              {tab.label}
              {counts[tab.key] !== undefined && ` · ${counts[tab.key]}`}
            </button>
          ))}
        </div>

        <div className="positions__filters" role="search">
          <input
            type="search"
            className="positions__search"
            placeholder="Tìm theo tên hoặc mã vị trí"
            aria-label="Tìm theo tên hoặc mã vị trí"
            maxLength={100}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <select
            className="select"
            aria-label="Phòng ban"
            value={query.departmentId ?? ""}
            onChange={(event) => filter({ departmentId: event.target.value || undefined })}
          >
            <option value="">Mọi phòng ban</option>
            {departments.data?.map((department) => (
              <option key={department.id} value={department.id}>
                {department.name}
              </option>
            ))}
          </select>
          {showOwnerFilter && (
            <select
              className="select"
              aria-label="Người phụ trách"
              value={query.ownerId ?? ""}
              onChange={(event) => filter({ ownerId: event.target.value || undefined })}
            >
              <option value="">Mọi người phụ trách</option>
              {owners.data?.map((owner) => (
                <option key={owner.id} value={owner.id}>
                  {owner.fullName}
                </option>
              ))}
            </select>
          )}
        </div>

        {positions.isPending ? (
          <p className="page-status">Đang tải danh sách vị trí…</p>
        ) : positions.isError ? (
          <div role="alert" className="page-status">
            <p>{positions.error.message}</p>
            <button type="button" className="button button--secondary" onClick={() => void positions.refetch()}>
              Thử lại
            </button>
          </div>
        ) : positions.data.items.length === 0 ? (
          <p className="page-status">
            {filtered ? "Không có vị trí nào khớp bộ lọc." : "Chưa có vị trí tuyển dụng nào."}
          </p>
        ) : (
          <>
            <table className="positions__table" aria-busy={positions.isPlaceholderData || undefined}>
              <thead>
                <tr>
                  <th scope="col">Mã</th>
                  <th scope="col">Vị trí</th>
                  <th scope="col" className="positions__number">
                    Hồ sơ
                  </th>
                  <th scope="col" className="positions__number">
                    Đã tuyển
                  </th>
                  <th scope="col">Hạn tuyển</th>
                  <th scope="col">Phụ trách</th>
                  <th scope="col">Trạng thái</th>
                  <th scope="col">
                    <span className="visually-hidden">Thao tác</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {positions.data.items.map((position) => {
                  const tag = statusTag(position.status, position.targetHireDate, today);
                  const actions = actionsFor(position);
                  return (
                    <tr key={position.id}>
                      <td className="positions__code">{position.code}</td>
                      <td>
                        <Link className="positions__title" to={`/jobs/${position.id}`}>
                          {position.title}
                        </Link>
                        <span className="positions__meta">
                          {position.department.name} · {position.location}
                        </span>
                      </td>
                      <td className="positions__number">{position.applicationCount}</td>
                      <td className="positions__number">
                        {position.hiredCount} / {position.headcount}
                      </td>
                      <td>{formatDate(position.targetHireDate)}</td>
                      <td>{position.owner.fullName}</td>
                      <td>
                        <span className={`tag tag--${tag.tone}`}>{tag.label}</span>
                      </td>
                      <td className="positions__actions">
                        {actions.length > 0 && (
                          <RowMenu label={`Thao tác với ${position.code} ${position.title}`} actions={actions} />
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
            <Pagination
              page={positions.data.page}
              pageSize={positions.data.pageSize}
              totalPages={positions.data.totalPages}
              onPageChange={(page) => setQuery((current) => ({ ...current, page }))}
              onPageSizeChange={(pageSize) => filter({ pageSize })}
            />
          </>
        )}
      </div>

      {creating && <PositionFormModal today={today} onClose={() => navigate("/jobs")} />}
      {statusChange && (
        <PositionStatusModal
          position={statusChange.position}
          target={statusChange.target}
          onClose={() => setStatusChange(null)}
        />
      )}
    </section>
  );
}
