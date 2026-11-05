import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createUser, listUsers, updateUser, updateUserStatus, type UserQuery, type UserUpdateRequest } from "./api";

const usersKey = ["users"] as const;

/** The previous page stays on screen while the next one loads. */
export function useUsers(query: UserQuery) {
  return useQuery({
    queryKey: [...usersKey, query],
    queryFn: () => listUsers(query),
    placeholderData: keepPreviousData,
  });
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
