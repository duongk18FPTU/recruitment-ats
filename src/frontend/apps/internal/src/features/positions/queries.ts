import { keepPreviousData, useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { createPosition, getPosition, listPositions, type PositionQuery, type PositionStatus } from "./api";

export const positionsKey = ["positions"] as const;

/** The previous page stays on screen while the next one loads. */
export function usePositions(query: PositionQuery) {
  return useQuery({
    queryKey: [...positionsKey, "list", query],
    queryFn: () => listPositions(query),
    placeholderData: keepPreviousData,
  });
}

/**
 * Counts for the SCR-10 status tabs under the other filters: one `listPositions` call per tab with
 * `pageSize=1`, reading `totalItems` (the contract has no count operation).
 */
export function useStatusCounts(filters: Omit<PositionQuery, "status" | "page" | "pageSize" | "sort">) {
  const tabs: (PositionStatus | undefined)[] = ["OPEN", "DRAFT", "CLOSED", undefined];
  const results = useQueries({
    queries: tabs.map((status) => {
      const query: PositionQuery = { ...filters, status, page: 1, pageSize: 1 };
      return { queryKey: [...positionsKey, "list", query], queryFn: () => listPositions(query) };
    }),
  });
  const [open, draft, closed, all] = results.map((result) => result.data?.totalItems);
  return { OPEN: open, DRAFT: draft, CLOSED: closed, ALL: all };
}

export function usePosition(positionId: string) {
  return useQuery({ queryKey: [...positionsKey, "detail", positionId], queryFn: () => getPosition(positionId) });
}

function usePositionMutation<TVariables, TResult>(mutationFn: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: positionsKey }),
  });
}

export const useCreatePosition = () => usePositionMutation(createPosition);
