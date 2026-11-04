import { ApiError, authSession } from "@ats/api-client";
import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { useNavigate } from "react-router";
import { AuthProvider } from "../features/auth";

/**
 * Server state for the app. A 403 from any query or mutation shows #/403 (the user stays
 * signed in); 4xx errors are not retried because repeating them cannot succeed.
 */
export function createQueryClient(onForbidden: () => void): QueryClient {
  const handle = (error: Error) => {
    if (error instanceof ApiError && error.status === 403) {
      onForbidden();
    }
  };

  return new QueryClient({
    queryCache: new QueryCache({ onError: handle }),
    mutationCache: new MutationCache({ onError: handle }),
    defaultOptions: {
      queries: {
        retry: (failureCount, error) =>
          !(error instanceof ApiError && error.status < 500) && failureCount < 2,
      },
    },
  });
}

interface AppProvidersProps {
  children: ReactNode;
  /** Restores the session on start; replaced in tests. */
  restoreSession?: () => Promise<unknown>;
}

/** Must be rendered inside the router. */
export function AppProviders({ children, restoreSession }: AppProvidersProps) {
  const navigate = useNavigate();
  const [forbiddenCount, setForbiddenCount] = useState(0);
  const [queryClient] = useState(() => createQueryClient(() => setForbiddenCount((count) => count + 1)));

  // `navigate` changes with every location, so only a new 403 may navigate.
  const handledForbidden = useRef(0);
  useEffect(() => {
    if (forbiddenCount > handledForbidden.current) {
      handledForbidden.current = forbiddenCount;
      void navigate("/403");
    }
  }, [forbiddenCount, navigate]);

  // Signing out (or a refused refresh) drops every cached record of the previous user.
  useEffect(
    () =>
      authSession.subscribe((session) => {
        if (!session) {
          queryClient.clear();
        }
      }),
    [queryClient],
  );

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider restore={restoreSession}>{children}</AuthProvider>
    </QueryClientProvider>
  );
}
