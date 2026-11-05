import { createContext, useCallback, useContext, useState, type ReactNode } from "react";
import "./components.css";

interface Toast {
  id: number;
  message: string;
}

const TOAST_MS = 4000;

const ToastContext = createContext<((message: string) => void) | null>(null);

/** Short confirmations after a mutation succeeds; read by screen readers through a polite live region. */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const show = useCallback((message: string) => {
    const id = Date.now() + Math.random();
    setToasts((current) => [...current, { id, message }]);
    setTimeout(() => setToasts((current) => current.filter((toast) => toast.id !== id)), TOAST_MS);
  }, []);

  return (
    <ToastContext value={show}>
      {children}
      <div className="toasts" aria-live="polite">
        {toasts.map((toast) => (
          <p key={toast.id} className="toast">
            {toast.message}
          </p>
        ))}
      </div>
    </ToastContext>
  );
}

export function useToast(): (message: string) => void {
  const show = useContext(ToastContext);
  if (!show) {
    throw new Error("useToast must be used inside <ToastProvider>.");
  }
  return show;
}
