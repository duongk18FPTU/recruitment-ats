import { useQuery } from "@tanstack/react-query";
import { listDepartments, listUserOptions, type UserOptionQuery } from "./api";

/** Lookups change rarely; a few minutes of staleness is fine for forms and filters. */
const LOOKUP_STALE_MS = 5 * 60 * 1000;

export function useDepartments() {
  return useQuery({ queryKey: ["lookups", "departments"], queryFn: listDepartments, staleTime: LOOKUP_STALE_MS });
}

/** Active users for pickers; `enabled` is false for roles that may not call `listUserOptions`. */
export function useUserOptions(query: UserOptionQuery, enabled = true) {
  return useQuery({
    queryKey: ["lookups", "users", query],
    queryFn: () => listUserOptions(query),
    staleTime: LOOKUP_STALE_MS,
    enabled,
  });
}
