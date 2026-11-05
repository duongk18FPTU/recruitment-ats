import { apiClient, unwrap, type components, type operations } from "@ats/api-client";

export type User = components["schemas"]["User"];
export type UserPage = components["schemas"]["UserPage"];
export type UserCreateRequest = components["schemas"]["UserCreateRequest"];
export type UserUpdateRequest = components["schemas"]["UserUpdateRequest"];
export type UserQuery = NonNullable<operations["listUsers"]["parameters"]["query"]>;

export async function listUsers(query: UserQuery) {
  return unwrap(await apiClient.GET("/api/users", { params: { query } }));
}

export async function createUser(body: UserCreateRequest) {
  return unwrap(await apiClient.POST("/api/users", { body }));
}

export async function updateUser(userId: string, body: UserUpdateRequest) {
  return unwrap(await apiClient.PUT("/api/users/{userId}", { params: { path: { userId } }, body }));
}

export async function updateUserStatus(userId: string, isActive: boolean) {
  return unwrap(
    await apiClient.PUT("/api/users/{userId}/status", { params: { path: { userId } }, body: { isActive } }),
  );
}
