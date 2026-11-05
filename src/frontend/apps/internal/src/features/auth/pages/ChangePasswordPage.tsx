import { ApiError } from "@ats/api-client";
import { useMutation } from "@tanstack/react-query";
import { useRef, useState, type FormEvent } from "react";
import { Navigate } from "react-router";
import { changePassword } from "../api";
import { useAuth } from "../AuthProvider";
import { AuthCard } from "../components/AuthCard";
import { FormError } from "../components/FormError";
import { PasswordField } from "../components/PasswordField";
import { validateChangePassword, type ChangePasswordField, type FieldErrors } from "../validation";

const FIELDS: ChangePasswordField[] = ["currentPassword", "newPassword", "confirmPassword"];
const EMPTY: Record<ChangePasswordField, string> = { currentPassword: "", newPassword: "", confirmPassword: "" };

/**
 * First login with an issued password (seed or US20): nothing else opens until it is replaced.
 * The API enforces the same rule (403 PASSWORD_CHANGE_REQUIRED).
 */
export function ChangePasswordPage() {
  const auth = useAuth();
  const currentRef = useRef<HTMLInputElement>(null);
  const newRef = useRef<HTMLInputElement>(null);
  const confirmRef = useRef<HTMLInputElement>(null);
  const [values, setValues] = useState(EMPTY);
  const [errors, setErrors] = useState<FieldErrors<ChangePasswordField>>({});
  const mutation = useMutation({
    mutationFn: changePassword,
    onError: (error) => {
      // Field errors from the API (e.g. a wrong current password) are shown under the field.
      if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
        const fromApi = Object.fromEntries(
          Object.entries(error.fieldErrors).map(([field, messages]) => [field, messages[0]]),
        ) as FieldErrors<ChangePasswordField>;
        setErrors(fromApi);
        focusFirst(fromApi);
      }
    },
  });

  if (auth.status === "signed-in" && !auth.user.mustChangePassword) {
    return <Navigate to="/dashboard" replace />;
  }

  function focusFirst(found: FieldErrors<ChangePasswordField>) {
    const inputs = { currentPassword: currentRef, newPassword: newRef, confirmPassword: confirmRef };
    const first = FIELDS.find((field) => found[field]);
    if (first) {
      inputs[first].current?.focus();
    }
  }

  const set = (field: ChangePasswordField) => (value: string) => setValues((v) => ({ ...v, [field]: value }));

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const found = validateChangePassword(values);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      focusFirst(found);
      return;
    }
    mutation.mutate({ currentPassword: values.currentPassword, newPassword: values.newPassword });
  };

  const apiFieldErrors = mutation.error instanceof ApiError && Object.keys(mutation.error.fieldErrors).length > 0;

  return (
    <AuthCard title="Đổi mật khẩu">
      <p className="auth__lead">Đây là lần đăng nhập đầu tiên. Hãy đặt mật khẩu mới mà chỉ bạn biết.</p>
      <form noValidate onSubmit={submit}>
        {mutation.error && !apiFieldErrors && <FormError error={mutation.error} />}
        <PasswordField
          label="Mật khẩu hiện tại"
          name="currentPassword"
          value={values.currentPassword}
          onChange={set("currentPassword")}
          error={errors.currentPassword}
          autoComplete="current-password"
          inputRef={currentRef}
        />
        <PasswordField
          label="Mật khẩu mới"
          name="newPassword"
          value={values.newPassword}
          onChange={set("newPassword")}
          error={errors.newPassword}
          autoComplete="new-password"
          inputRef={newRef}
        />
        <p className="field__hint">Ít nhất 10 ký tự, có cả chữ và số.</p>
        <PasswordField
          label="Nhập lại mật khẩu mới"
          name="confirmPassword"
          value={values.confirmPassword}
          onChange={set("confirmPassword")}
          error={errors.confirmPassword}
          autoComplete="new-password"
          inputRef={confirmRef}
        />
        <button type="submit" className="button button--primary button--block" disabled={mutation.isPending}>
          {mutation.isPending ? "Đang lưu…" : "Đổi mật khẩu"}
        </button>
      </form>
    </AuthCard>
  );
}
