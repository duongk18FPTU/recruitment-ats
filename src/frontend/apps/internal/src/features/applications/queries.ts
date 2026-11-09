import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { createApplication, matchCandidate } from "./api";
import { isEmail } from "./schemas";

const MATCH_DELAY_MS = 400;
/** Fewer digits cannot be a whole phone number, so nothing is looked up while it is typed. */
const MIN_PHONE_DIGITS = 9;

const applicationsKey = ["applications"] as const;

/**
 * The existing candidate behind the email or phone being typed (BR-06), looked up 400 ms after the last key
 * with `matchCandidate`; `createApplication` still decides on save. A result for contacts the recruiter has
 * since changed is not shown.
 */
export function useCandidateMatch(email: string, phone: string, positionId: string) {
  const typed = { email: email.trim(), phone: phone.trim() };
  const [debounced, setDebounced] = useState(typed);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced({ email: email.trim(), phone: phone.trim() }), MATCH_DELAY_MS);
    return () => clearTimeout(timer);
  }, [email, phone]);

  const query = {
    ...(isEmail(debounced.email) ? { email: debounced.email } : {}),
    ...(debounced.phone.replace(/\D/g, "").length >= MIN_PHONE_DIGITS ? { phone: debounced.phone } : {}),
  };
  const lookup = useQuery({
    queryKey: [...applicationsKey, "match", positionId, query],
    queryFn: () => matchCandidate({ ...query, positionId }),
    enabled: Object.keys(query).length > 0,
    staleTime: 30_000,
  });
  return debounced.email === typed.email && debounced.phone === typed.phone && lookup.data?.matched ? lookup.data : null;
}

export function useCreateApplication() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createApplication,
    // Counts per stage and per position change; the lists of applications (US08) will share this key.
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ["positions"] }),
        queryClient.invalidateQueries({ queryKey: applicationsKey }),
      ]),
  });
}
