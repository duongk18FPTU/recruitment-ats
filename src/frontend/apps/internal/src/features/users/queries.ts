import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { createUser, listUsers, updateUser, updateUserStatus, type UserQuery, type UserUpdateRequest } from "./api";
import { isEmail } from "./validation";

const EMAIL_CHECK_DELAY_MS = 400;

const usersKey = ["users"] as const;

/** The previous page stays on screen while the next one loads. */
export function useUsers(query: UserQuery) {
  return useQuery({
    queryKey: [...usersKey, query],
    queryFn: () => listUsers(query),
    placeholderData: keepPreviousData,
  });
}

/**
 * The user (other than `exceptId`) who already has this email, checked while the Admin types: a
 * search with `listUsers` filtered to an exact, case-insensitive match. `createUser` / `updateUser`
 * still answer 409 DUPLICATE_EMAIL if two Admins race.
 */
export function useEmailOwner(email: string, exceptId?: string) {
  const normalized = email.trim().toLowerCase();
  const [debounced, setDebounced] = useState(normalized);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(normalized), EMAIL_CHECK_DELAY_MS);
    return () => clearTimeout(timer);
  }, [normalized]);

  const check = useQuery({
    queryKey: [...usersKey, "email-owner", debounced],
    queryFn: () => listUsers({ q: debounced, pageSize: 100 }),
    enabled: isEmail(debounced),
    select: (page) => page.items.find((u) => u.email.toLowerCase() === debounced && u.id !== exceptId) ?? null,
    staleTime: 30_000,
  });
  // A result for an email the Admin has since changed is not shown.
  return debounced === normalized ? (check.data ?? null) : null;
}

function useUserMutation<TVariables, TResult>(mutationFn: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: usersKey }),
  });
}

export const useCreateUser = () => useUserMutation(createUser);

export const useUpdateUser = () =>
  useUserMutation(({ userId, body }: { userId: string; body: UserUpdateRequest }) => updateUser(userId, body));

export const useUpdateUserStatus = () =>
  useUserMutation(({ userId, isActive }: { userId: string; isActive: boolean }) => updateUserStatus(userId, isActive));
