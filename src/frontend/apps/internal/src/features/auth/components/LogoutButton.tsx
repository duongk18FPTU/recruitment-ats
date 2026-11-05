import { useMutation } from "@tanstack/react-query";
import { logout } from "../api";

interface LogoutButtonProps {
  /** `icon`: the sidebar user block (Figma); `link`: a text button. */
  variant: "icon" | "link";
  className?: string;
}

/** Signing out clears the session; the query cache is cleared by AppProviders and Login opens. */
export function LogoutButton({ variant, className }: LogoutButtonProps) {
  const mutation = useMutation({ mutationFn: logout });

  return (
    <button
      type="button"
      className={className}
      aria-label={variant === "icon" ? "Đăng xuất" : undefined}
      title={variant === "icon" ? "Đăng xuất" : undefined}
      disabled={mutation.isPending}
      onClick={() => mutation.mutate()}
    >
      {variant === "icon" ? (
        <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
          <path d="M14 4h4a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-4" fill="none" stroke="currentColor" strokeWidth="1.6" />
          <path d="M10 8l-4 4 4 4M6 12h10" fill="none" stroke="currentColor" strokeWidth="1.6" />
        </svg>
      ) : (
        "Đăng xuất"
      )}
    </button>
  );
}
