import { Modal } from "../../../components/Modal";
import { useToast } from "../../../components/Toasts";
import type { PositionStatus, PositionSummary } from "../api";
import { useUpdatePositionStatus } from "../queries";

const copy: Record<PositionStatus, { title: string; action: string; done: string; consequence: string }> = {
  OPEN: {
    title: "Mở tuyển vị trí?",
    action: "Mở tuyển",
    done: "Đã mở tuyển",
    consequence: "Vị trí sẽ nhận hồ sơ mới.",
  },
  CLOSED: {
    title: "Đóng vị trí?",
    action: "Đóng vị trí",
    done: "Đã đóng",
    consequence: "Vị trí sẽ ngừng nhận hồ sơ mới. Các hồ sơ hiện có vẫn được giữ nguyên.",
  },
  DRAFT: { title: "", action: "", done: "", consequence: "" }, // never a target (docs/11 "Position status")
};

/** Confirms opening a draft, closing, or reopening a position (US07), naming it and the consequence. */
export function PositionStatusModal({
  position,
  target,
  onClose,
}: {
  position: Pick<PositionSummary, "id" | "code" | "title" | "status">;
  target: PositionStatus;
  onClose: () => void;
}) {
  const toast = useToast();
  const mutation = useUpdatePositionStatus();
  const reopening = position.status === "CLOSED" && target === "OPEN";
  const text = reopening
    ? { title: "Mở lại vị trí?", action: "Mở lại", done: "Đã mở lại", consequence: "Vị trí sẽ nhận hồ sơ mới trở lại." }
    : copy[target];

  const confirm = () =>
    mutation.mutate(
      { positionId: position.id, status: target },
      {
        onSuccess: () => {
          toast(`${text.done} ${position.code} ${position.title}.`);
          onClose();
        },
      },
    );

  return (
    <Modal
      title={text.title}
      onClose={onClose}
      footer={
        <>
          <button type="button" className="button button--secondary" onClick={onClose}>
            Huỷ
          </button>
          <button
            type="button"
            className={`button ${target === "CLOSED" ? "button--danger" : "button--primary"}`}
            disabled={mutation.isPending}
            onClick={confirm}
          >
            {text.action}
          </button>
        </>
      }
    >
      {mutation.error && (
        <div role="alert" className="form-error">
          <p>{mutation.error.message}</p>
        </div>
      )}
      <p className="position-modal__text">
        <strong>
          {position.code} {position.title}
        </strong>
        . {text.consequence}
      </p>
    </Modal>
  );
}
