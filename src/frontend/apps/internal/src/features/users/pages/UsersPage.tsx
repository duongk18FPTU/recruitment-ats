import type { Role } from "@ats/api-client";
import { useEffect, useState } from "react";
import { roleLabels } from "../../../app/navigation";
import plusIcon from "../../../assets/icons/plus.svg";
import { initials } from "../../../components/initials";
import { Pagination } from "../../../components/Pagination";
import { RowMenu } from "../../../components/RowMenu";
import { useAuth } from "../../auth";
import type { User, UserQuery } from "../api";
import { StatusModal } from "../components/StatusModal";
import { UserFormModal } from "../components/UserFormModal";
import { formatLastLogin } from "../format";
import { useUsers } from "../queries";
import "../users.css";

const SEARCH_DELAY_MS = 300;
const ROLES = Object.keys(roleLabels) as Role[];

type Dialog = { kind: "create" } | { kind: "edit"; user: User } | { kind: "status"; user: User } | null;

/** SCR-70 Người dùng (US20, Admin): list with search and filters; create, edit, lock / unlock in modals. */
export function UsersPage() {
  const auth = useAuth();
  const me = auth.status === "signed-in" ? auth.user : undefined;
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState<UserQuery>({ page: 1, pageSize: 20 });
  const [dialog, setDialog] = useState<Dialog>(null);
  const users = useUsers(query);

  // Typing filters after a short pause; every filter change starts again at page 1.
  useEffect(() => {
    const q = search.trim() || undefined;
    const apply = () => setQuery((current) => (current.q === q ? current : { ...current, q, page: 1 }));
    const timer = setTimeout(apply, SEARCH_DELAY_MS);
    return () => clearTimeout(timer);
  }, [search]);

  const filter = (change: Partial<UserQuery>) => setQuery((current) => ({ ...current, ...change, page: 1 }));
  const now = new Date();
  const timeZone = me?.timezone ?? "Asia/Ho_Chi_Minh";

  return (
    <section className="users">
      <header className="users__header">
        <h1>Người dùng</h1>
        <button
          type="button"
          className="button button--primary button--icon"
          onClick={() => setDialog({ kind: "create" })}
        >
          <img src={plusIcon} width={16} height={16} alt="" />
          Thêm người dùng
        </button>
      </header>

      <div className="users__card">
        <div className="users__filters" role="search">
          <input
            type="search"
            className="users__search"
            placeholder="Tên hoặc email"
            aria-label="Tìm theo tên hoặc email"
            maxLength={100}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          <select
            className="select"
            aria-label="Vai trò"
            value={query.role ?? ""}
            onChange={(event) => filter({ role: (event.target.value || undefined) as Role | undefined })}
          >
            <option value="">Mọi vai trò</option>
            {ROLES.map((role) => (
              <option key={role} value={role}>
                {roleLabels[role]}
              </option>
            ))}
          </select>
          <select
            className="select"
            aria-label="Trạng thái"
            value={query.isActive === undefined ? "" : String(query.isActive)}
            onChange={(event) =>
              filter({ isActive: event.target.value === "" ? undefined : event.target.value === "true" })
            }
          >
            <option value="">Mọi trạng thái</option>
            <option value="true">Hoạt động</option>
            <option value="false">Đã khoá</option>
          </select>
        </div>

        {users.isPending ? (
          <p className="page-status">Đang tải danh sách người dùng…</p>
        ) : users.isError ? (
          <div role="alert" className="page-status">
            <p>{users.error.message}</p>
            <button type="button" className="button button--secondary" onClick={() => void users.refetch()}>
              Thử lại
            </button>
          </div>
        ) : users.data.items.length === 0 ? (
          <p className="page-status">
            {query.q || query.role || query.isActive !== undefined
              ? "Không có người dùng nào khớp bộ lọc."
              : "Chưa có người dùng nào."}
          </p>
        ) : (
          <>
            <table className="users__table" aria-busy={users.isPlaceholderData || undefined}>
              <thead>
                <tr>
                  <th scope="col">Người dùng</th>
                  <th scope="col">Vai trò</th>
                  <th scope="col">Trạng thái</th>
                  <th scope="col">Đăng nhập gần nhất</th>
                  <th scope="col">
                    <span className="visually-hidden">Thao tác</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {users.data.items.map((user) => (
                  <tr key={user.id}>
                    <td>
                      <div className="users__person">
                        <span className="users__avatar" aria-hidden="true">
                          {initials(user.fullName)}
                        </span>
                        <span>
                          <span className="users__name">
                            {user.fullName}
                            {user.id === me?.id && <span className="users__me"> (bạn)</span>}
                          </span>
                          <span className="users__email">{user.email}</span>
                        </span>
                      </div>
                    </td>
                    <td>{roleLabels[user.role]}</td>
                    <td>
                      <span className={`tag ${user.isActive ? "tag--active" : "tag--muted"}`}>
                        {user.isActive ? "Hoạt động" : "Đã khoá"}
                      </span>
                    </td>
                    <td>{formatLastLogin(user.lastLoginAt, now, timeZone)}</td>
                    <td className="users__actions">
                      <RowMenu
                        label={`Thao tác với ${user.fullName}`}
                        actions={[
                          { label: "Sửa", onSelect: () => setDialog({ kind: "edit", user }) },
                          // An Admin cannot lock their own account (the API answers 409 SELF_DEACTIVATION).
                          ...(user.id === me?.id
                            ? []
                            : [
                                {
                                  label: user.isActive ? "Khoá tài khoản" : "Mở khoá",
                                  onSelect: () => setDialog({ kind: "status", user }),
                                },
                              ]),
                        ]}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <Pagination
              page={users.data.page}
              pageSize={users.data.pageSize}
              totalPages={users.data.totalPages}
              onPageChange={(page) => setQuery((current) => ({ ...current, page }))}
              onPageSizeChange={(pageSize) => filter({ pageSize })}
            />
          </>
        )}
      </div>

      {dialog?.kind === "create" && <UserFormModal onClose={() => setDialog(null)} />}
      {dialog?.kind === "edit" && <UserFormModal user={dialog.user} onClose={() => setDialog(null)} />}
      {dialog?.kind === "status" && <StatusModal user={dialog.user} onClose={() => setDialog(null)} />}
    </section>
  );
}
