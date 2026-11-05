import { ApiError, type Role } from "@ats/api-client";
import { useId, useRef, useState, type FormEvent } from "react";
import { roleLabels } from "../../../app/navigation";
import { Modal } from "../../../components/Modal";
import { useToast } from "../../../components/Toasts";
import type { User } from "../api";
import { useCreateUser, useUpdateUser } from "../queries";
import {
  generateTemporaryPassword,
  validateUserForm,
  type UserFormErrors,
  type UserFormField,
  type UserFormValues,
} from "../validation";

const ROLES = Object.keys(roleLabels) as Role[];

interface UserFormModalProps {
  /** Absent: create a user. */
  user?: User;
  onClose: () => void;
}

/** SCR-70 user form (create / edit). An issued password must be replaced at the user's first login. */
export function UserFormModal({ user, onClose }: UserFormModalProps) {
  const formId = useId();
  const formRef = useRef<HTMLFormElement>(null);
  const toast = useToast();
  const create = useCreateUser();
  const update = useUpdateUser();
  const mutation = user ? update : create;
  const [values, setValues] = useState<UserFormValues>({
    fullName: user?.fullName ?? "",
    email: user?.email ?? "",
    phone: user?.phone ?? "",
    role: user?.role ?? "",
    temporaryPassword: "",
  });
  const [issuePassword, setIssuePassword] = useState(!user);
  const [errors, setErrors] = useState<UserFormErrors>({});
  const [formError, setFormError] = useState<Error | null>(null);

  const set = (field: UserFormField) => (value: string) => setValues((v) => ({ ...v, [field]: value }));
  const bind = (name: UserFormField) => ({ name, value: values[name], onChange: set(name), error: errors[name] });

  const showErrors = (found: UserFormErrors) => {
    setErrors(found);
    const first = Object.keys(found)[0];
    if (first) {
      formRef.current?.querySelector<HTMLElement>(`[name="${first}"]`)?.focus();
    }
  };

  // A taken email (409) and field errors from the API are shown under their fields.
  const fail = (error: Error) => {
    if (error instanceof ApiError && error.errorCode === "DUPLICATE_EMAIL") {
      showErrors({ email: error.message });
    } else if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
      const fromApi = Object.entries(error.fieldErrors).map(([field, messages]) => [field, messages[0]]);
      showErrors(Object.fromEntries(fromApi));
    } else {
      setFormError(error);
    }
  };

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setFormError(null);
    const found = validateUserForm(values, issuePassword);
    showErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    const body = {
      fullName: values.fullName.trim(),
      email: values.email.trim(),
      phone: values.phone.trim() || null,
      role: values.role as Role,
    };
    if (user) {
      update.mutate(
        { userId: user.id, body: { ...body, temporaryPassword: issuePassword ? values.temporaryPassword : null } },
        {
          onSuccess: () => {
            toast(issuePassword ? "Đã lưu và cấp mật khẩu tạm mới." : "Đã lưu thay đổi.");
            onClose();
          },
          onError: fail,
        },
      );
    } else {
      create.mutate(
        { ...body, temporaryPassword: values.temporaryPassword },
        {
          onSuccess: (created) => {
            toast(`Đã tạo tài khoản cho ${created.fullName}.`);
            onClose();
          },
          onError: fail,
        },
      );
    }
  };

  return (
    <Modal
      title={user ? "Sửa người dùng" : "Thêm người dùng"}
      onClose={onClose}
      footer={
        <>
          <button type="button" className="button button--secondary" onClick={onClose}>
            Huỷ
          </button>
          <button type="submit" form={formId} className="button button--primary" disabled={mutation.isPending}>
            {mutation.isPending ? "Đang lưu…" : user ? "Lưu thay đổi" : "Tạo tài khoản"}
          </button>
        </>
      }
    >
      <form ref={formRef} id={formId} noValidate onSubmit={submit}>
        {formError && (
          <div role="alert" className="form-error">
            <p>{formError.message}</p>
          </div>
        )}
        <TextField label="Họ tên" {...bind("fullName")} />
        <TextField label="Email" type="email" {...bind("email")} />
        <TextField label="Số điện thoại (không bắt buộc)" type="tel" {...bind("phone")} />
        <SelectField label="Vai trò" {...bind("role")} />

        {user && (
          <label className="checkbox">
            <input
              type="checkbox"
              checked={issuePassword}
              onChange={(event) => setIssuePassword(event.target.checked)}
            />
            Cấp mật khẩu tạm mới (người dùng quên mật khẩu)
          </label>
        )}
        {issuePassword && (
          <>
            <div className="users-form__password">
              <TextField label="Mật khẩu tạm" {...bind("temporaryPassword")} />
              <button
                type="button"
                className="button button--secondary"
                onClick={() => set("temporaryPassword")(generateTemporaryPassword())}
              >
                Tạo ngẫu nhiên
              </button>
            </div>
            <p className="field__hint">
              Ít nhất 10 ký tự, có cả chữ và số. Gửi mật khẩu này cho người dùng; họ phải đổi ở lần đăng nhập tới
              {user ? ", và mọi phiên đang mở của họ sẽ bị đăng xuất" : ""}.
            </p>
          </>
        )}
      </form>
    </Modal>
  );
}

interface FieldProps {
  label: string;
  name: UserFormField;
  value: string;
  onChange: (value: string) => void;
  error?: string;
}

/** Admin-entered data about someone else: the browser must not autofill the Admin's own details. */
function TextField({ label, name, value, onChange, error, type = "text" }: FieldProps & { type?: string }) {
  const id = useId();
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        name={name}
        type={type}
        value={value}
        autoComplete="off"
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
        onChange={(event) => onChange(event.target.value)}
      />
      {error && (
        <p id={`${id}-error`} className="field__error">
          {error}
        </p>
      )}
    </div>
  );
}

function SelectField({ label, name, value, onChange, error }: FieldProps) {
  const id = useId();
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <select
        id={id}
        name={name}
        value={value}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">Chọn vai trò</option>
        {ROLES.map((role) => (
          <option key={role} value={role}>
            {roleLabels[role]}
          </option>
        ))}
      </select>
      {error && (
        <p id={`${id}-error`} className="field__error">
          {error}
        </p>
      )}
    </div>
  );
}
