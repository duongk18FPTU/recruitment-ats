import { apiClient, unwrap, type components, type operations } from "@ats/api-client";

export type PositionSummary = components["schemas"]["PositionSummary"];
export type PositionDetail = components["schemas"]["PositionDetail"];
export type PositionStatus = components["schemas"]["PositionStatus"];
export type PositionQuery = NonNullable<operations["listPositions"]["parameters"]["query"]>;

export async function listPositions(query: PositionQuery) {
  return unwrap(await apiClient.GET("/api/positions", { params: { query } }));
}

export async function getPosition(positionId: string) {
  return unwrap(await apiClient.GET("/api/positions/{positionId}", { params: { path: { positionId } } }));
}
