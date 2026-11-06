import { apiClient, unwrap, type components, type operations } from "@ats/api-client";

export type Department = components["schemas"]["Department"];
export type UserOption = components["schemas"]["UserOption"];
export type UserOptionQuery = NonNullable<operations["listUserOptions"]["parameters"]["query"]>;

export async function listDepartments() {
  return unwrap(await apiClient.GET("/api/lookups/departments"));
}

export async function listUserOptions(query: UserOptionQuery) {
  return unwrap(await apiClient.GET("/api/lookups/users", { params: { query } }));
}
