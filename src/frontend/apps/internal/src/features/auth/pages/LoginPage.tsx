import { useMutation } from "@tanstack/react-query";
import { useId, useRef, useState, type FormEvent } from "react";
import { Navigate, useLocation, type Location } from "react-router";
import { login } from "../api";
import { useAuth } from "../AuthProvider";
import { AuthCard } from "../components/AuthCard";
import { FormError } from "../components/FormError";
import { PasswordField } from "../components/PasswordField";
import { validateLogin, type FieldErrors } from "../validation";

/** SCR-01 (US01). Accounts are issued by an Admin, so there is no sign-up link. */
export function LoginPage() {
  const auth = useAuth();
  const from = (useLocation().state as { from?: Location } | null)?.from;
  const emailId = useId();
  const emailRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<FieldErrors<"email" | "password">>({});
  const mutation = useMutation({ mutationFn: login });

  if (auth.status === "signed-in") {
    return <Navigate to={from ?? "/dashboard"} replace />;
  }

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const found = validateLogin({ email, password });
    setErrors(found);
    if (found.email || found.password) {
      (found.email ? emailRef : passwordRef).current?.focus();
      return; // AC-US01 #3: no request is sent
    }
    mutation.mutate({ email: email.trim(), password });
  };

  return (
    <AuthCard title="Đăng nhập" note="Chưa có tài khoản? Liên hệ quản trị viên của bạn.">
      <form noValidate onSubmit={submit}>
        {mutation.error && <FormError error={mutation.error} />}
        <div className="field">
          <label htmlFor={emailId}>Email công ty</label>
          <input
            ref={emailRef}
            id={emailId}
            name="email"
            type="email"
            autoComplete="username"
            value={email}
            aria-invalid={errors.email ? true : undefined}
            aria-describedby={errors.email ? `${emailId}-error` : undefined}
            onChange={(event) => setEmail(event.target.value)}
          />
          {errors.email && (
            <p id={`${emailId}-error`} className="field__error">
              {errors.email}
            </p>
          )}
        </div>
        <PasswordField
          label="Mật khẩu"
          name="password"
          value={password}
          onChange={setPassword}
          error={errors.password}
          autoComplete="current-password"
          inputRef={passwordRef}
        />
        <button type="submit" className="button button--primary button--block" disabled={mutation.isPending}>
          {mutation.isPending ? "Đang đăng nhập…" : "Đăng nhập"}
        </button>
      </form>
    </AuthCard>
  );
}
