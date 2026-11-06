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

export type PositionCreateRequest = components["schemas"]["PositionCreateRequest"];
export type PositionUpdateRequest = components["schemas"]["PositionUpdateRequest"];

export async function createPosition(body: PositionCreateRequest) {
  return unwrap(await apiClient.POST("/api/positions", { body }));
}

export async function updatePosition(positionId: string, body: PositionUpdateRequest) {
  return unwrap(await apiClient.PUT("/api/positions/{positionId}", { params: { path: { positionId } }, body }));
}

export async function updatePositionStatus(positionId: string, status: PositionStatus) {
  return unwrap(
    await apiClient.PUT("/api/positions/{positionId}/status", { params: { path: { positionId } }, body: { status } }),
  );
}
