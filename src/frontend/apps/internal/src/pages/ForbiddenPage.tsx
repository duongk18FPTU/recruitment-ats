import { Link } from "react-router";

/** SCR-90: the user stays signed in and can go back to the Dashboard. */
export function ForbiddenPage() {
  return (
    <section className="page-message">
      <h1>Bạn không có quyền truy cập trang này</h1>
      <p>Vai trò hiện tại của bạn không được mở trang này. Liên hệ Quản trị viên nếu bạn cần quyền.</p>
      <Link to="/dashboard">Về trang Tổng quan</Link>
    </section>
  );
}
