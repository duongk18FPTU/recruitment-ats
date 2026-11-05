/** Client-side checks for UX only; the API is authoritative (docs/09-folder-structure.md §4.1). */
export type FieldErrors<K extends string> = Partial<Record<K, string>>;

export function validateLogin(values: { email: string; password: string }): FieldErrors<"email" | "password"> {
  const errors: FieldErrors<"email" | "password"> = {};
  if (!values.email.trim()) {
    errors.email = "Nhập email.";
  }
  if (!values.password) {
    errors.password = "Nhập mật khẩu.";
  }
  return errors;
}

export type ChangePasswordField = "currentPassword" | "newPassword" | "confirmPassword";

/** Same rules as ChangePasswordRequestValidator: 10–128 characters, letters and digits, a new value. */
export function validateChangePassword(values: Record<ChangePasswordField, string>): FieldErrors<ChangePasswordField> {
  const errors: FieldErrors<ChangePasswordField> = {};
  const next = values.newPassword;

  if (!values.currentPassword) {
    errors.currentPassword = "Nhập mật khẩu hiện tại.";
  }
  if (!next) {
    errors.newPassword = "Nhập mật khẩu mới.";
  } else if (next.length < 10 || next.length > 128) {
    errors.newPassword = "Mật khẩu mới phải có từ 10 đến 128 ký tự.";
  } else if (!/\p{L}/u.test(next) || !/\p{Nd}/u.test(next)) {
    errors.newPassword = "Mật khẩu mới phải có cả chữ và số.";
  } else if (next === values.currentPassword) {
    errors.newPassword = "Mật khẩu mới phải khác mật khẩu hiện tại.";
  }
  if (values.confirmPassword !== next) {
    errors.confirmPassword = "Mật khẩu nhập lại không khớp.";
  }
  return errors;
}
