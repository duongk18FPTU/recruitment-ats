import type { Role } from "@ats/api-client";

/** Client-side checks for UX only; the API is authoritative (UserRequestValidators). */
export type UserFormField = "fullName" | "email" | "phone" | "role" | "temporaryPassword";
export type UserFormErrors = Partial<Record<UserFormField, string>>;

export interface UserFormValues {
  fullName: string;
  email: string;
  phone: string;
  role: Role | "";
  /** Required when creating; when editing, only if a new temporary password is issued. */
  temporaryPassword: string;
}

export function validateUserForm(values: UserFormValues, passwordRequired: boolean): UserFormErrors {
  const errors: UserFormErrors = {};
  const fullName = values.fullName.trim();
  const email = values.email.trim();

  if (!fullName) {
    errors.fullName = "Nhập họ tên.";
  } else if (fullName.length < 2 || fullName.length > 120) {
    errors.fullName = "Họ tên phải có từ 2 đến 120 ký tự.";
  }
  if (!email) {
    errors.email = "Nhập email.";
  } else if (!isEmail(email)) {
    errors.email = "Email không hợp lệ.";
  }
  if (values.phone.trim().length > 20) {
    errors.phone = "Số điện thoại tối đa 20 ký tự.";
  }
  if (!values.role) {
    errors.role = "Chọn vai trò.";
  }
  if (passwordRequired) {
    const password = values.temporaryPassword;
    if (!password) {
      errors.temporaryPassword = "Nhập mật khẩu tạm.";
    } else if (password.length < 10 || password.length > 128) {
      errors.temporaryPassword = "Mật khẩu tạm phải có từ 10 đến 128 ký tự.";
    } else if (!/\p{L}/u.test(password) || !/\p{Nd}/u.test(password)) {
      errors.temporaryPassword = "Mật khẩu tạm phải có cả chữ và số.";
    }
  }
  return errors;
}

/** Same shape check as the form; the API's validator is authoritative. */
export function isEmail(value: string): boolean {
  return value.length <= 254 && /^[^\s@]+@[^\s@]+$/.test(value);
}

// No look-alike characters (0/O, 1/l/I), so the password can be read out or typed from a message.
const LETTERS = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
const DIGITS = "23456789";

/** 12 random characters with at least one letter and one digit. */
export function generateTemporaryPassword(random: (n: number) => Uint32Array = randomValues): string {
  const alphabet = LETTERS + DIGITS;
  const values = random(12);
  const chars = [...values].map((value) => alphabet[value % alphabet.length]!);
  chars[0] = LETTERS[values[0]! % LETTERS.length]!;
  chars[values[1]! % 11 + 1] = DIGITS[values[2]! % DIGITS.length]!;
  return chars.join("");
}

function randomValues(n: number): Uint32Array {
  return crypto.getRandomValues(new Uint32Array(n));
}
