import type { ReactNode } from "react";
import { Logo } from "../../../components/layout/Logo";
import "../auth.css";

/** SCR-01 layout: brand above a single centred card, an optional note below. */
export function AuthCard({ title, children, note }: { title: string; children: ReactNode; note?: ReactNode }) {
  return (
    <main className="auth">
      <div className="auth__brand">
        <Logo />
        <span>
          Recruitment <strong>ATS</strong>
        </span>
      </div>
      <section className="auth__card" aria-labelledby="auth-title">
        <h1 id="auth-title">{title}</h1>
        {children}
      </section>
      {note && <p className="auth__note">{note}</p>}
    </main>
  );
}
