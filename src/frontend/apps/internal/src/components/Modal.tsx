import { useEffect, useId, useRef, type ReactNode } from "react";
import "./components.css";

interface ModalProps {
  title: string;
  onClose: () => void;
  children: ReactNode;
  /** Buttons in the footer, right-aligned. */
  footer: ReactNode;
  /** `wide` for forms with several columns (SCR-11). */
  size?: "default" | "wide";
}

/**
 * A dialog over the page: Escape and the backdrop close it, focus moves into it and returns to
 * the element that opened it, and Tab stays inside.
 */
export function Modal({ title, onClose, children, footer, size = "default" }: ModalProps) {
  const titleId = useId();
  const panelRef = useRef<HTMLDivElement>(null);
  const onCloseRef = useRef(onClose);

  useEffect(() => {
    onCloseRef.current = onClose;
  });

  useEffect(() => {
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const panel = panelRef.current;
    const first = panel?.querySelector<HTMLElement>("input, select, textarea, button:not(.modal__close)");
    (first ?? panel)?.focus();

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onCloseRef.current();
      } else if (event.key === "Tab" && panel) {
        const focusable = [...panel.querySelectorAll<HTMLElement>("input, select, textarea, button, a[href]")].filter(
          (element) => !element.hasAttribute("disabled"),
        );
        const [head, tail] = [focusable[0], focusable[focusable.length - 1]];
        if (event.shiftKey && document.activeElement === head) {
          event.preventDefault();
          tail?.focus();
        } else if (!event.shiftKey && document.activeElement === tail) {
          event.preventDefault();
          head?.focus();
        }
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      opener?.focus();
    };
  }, []);

  return (
    <div className="modal" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <div
        ref={panelRef}
        className={size === "wide" ? "modal__panel modal__panel--wide" : "modal__panel"}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
      >
        <header className="modal__header">
          <h2 id={titleId}>{title}</h2>
          <button type="button" className="modal__close" aria-label="Đóng" onClick={onClose}>
            <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
              <path d="M6 6l12 12M18 6L6 18" fill="none" stroke="currentColor" strokeWidth="1.6" />
            </svg>
          </button>
        </header>
        <div className="modal__body">{children}</div>
        <footer className="modal__footer">{footer}</footer>
      </div>
    </div>
  );
}
