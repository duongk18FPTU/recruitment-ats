import { Modal } from "../../../components/Modal";
import { useToast } from "../../../components/Toasts";
import type { User } from "../api";
import { useUpdateUserStatus } from "../queries";

/** Confirms locking (deactivating) or unlocking an account. */
export function StatusModal({ user, onClose }: { user: User; onClose: () => void }) {
  const toast = useToast();
  const mutation = useUpdateUserStatus();
  const deactivate = user.isActive;

  const confirm = () =>
    mutation.mutate(
      { userId: user.id, isActive: !deactivate },
      {
        onSuccess: () => {
          toast(deactivate ? `Đã khoá tài khoản ${user.fullName}.` : `Đã mở khoá tài khoản ${user.fullName}.`);
          onClose();
        },
      },
    );

  return (
    <Modal
      title={deactivate ? "Khoá tài khoản?" : "Mở khoá tài khoản?"}
      onClose={onClose}
      footer={
        <>
          <button type="button" className="button button--secondary" onClick={onClose}>
            Huỷ
          </button>
          <button
            type="button"
            className={`button ${deactivate ? "button--danger" : "button--primary"}`}
            disabled={mutation.isPending}
            onClick={confirm}
          >
            {deactivate ? "Khoá tài khoản" : "Mở khoá"}
          </button>
        </>
      }
    >
      {mutation.error && (
        <div role="alert" className="form-error">
          <p>{mutation.error.message}</p>
        </div>
      )}
      <p className="users-modal__text">
        {deactivate ? (
          <>
            <strong>{user.fullName}</strong> ({user.email}) sẽ bị đăng xuất và không đăng nhập được nữa. Hồ sơ và lịch
            sử của họ vẫn được giữ.
          </>
        ) : (
          <>
            <strong>{user.fullName}</strong> ({user.email}) sẽ đăng nhập lại được bằng mật khẩu hiện có.
          </>
        )}
      </p>
    </Modal>
  );
}
