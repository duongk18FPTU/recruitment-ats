import { authSession, refreshSession, type UserProfile } from "@ats/api-client";
import { createContext, useContext, useEffect, useState, useSyncExternalStore, type ReactNode } from "react";

export type AuthState =
  | { status: "restoring" }
  | { status: "anonymous" }
  | { status: "signed-in"; user: UserProfile };

const AuthContext = createContext<AuthState | null>(null);

interface AuthProviderProps {
  children: ReactNode;
  /** Restores the session from the refresh cookie on start; replaced in tests. */
  restore?: () => Promise<unknown>;
}

/** Session state for the app; the token itself stays in `@ats/api-client` (memory only). */
export function AuthProvider({ children, restore = refreshSession }: AuthProviderProps) {
  const session = useSyncExternalStore(authSession.subscribe, authSession.get);
  const [restored, setRestored] = useState(() => authSession.get() !== null);

  useEffect(() => {
    if (restored) {
      return;
    }
    let active = true;
    void restore().finally(() => {
      if (active) {
        setRestored(true);
      }
    });
    return () => {
      active = false;
    };
  }, [restore, restored]);

  const state: AuthState = session
    ? { status: "signed-in", user: session.user }
    : restored
      ? { status: "anonymous" }
      : { status: "restoring" };

  return <AuthContext value={state}>{children}</AuthContext>;
}

export function useAuth(): AuthState {
  const state = useContext(AuthContext);
  if (!state) {
    throw new Error("useAuth must be used inside <AuthProvider>.");
  }
  return state;
}
