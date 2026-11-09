import { apiClient, unwrap, type components } from "@ats/api-client";

export type Source = components["schemas"]["Source"];
export type ApplicationCreated = components["schemas"]["ApplicationCreated"];
export type CandidateMatch = components["schemas"]["CandidateMatch"];

export interface ApplicationInput {
  positionId: string;
  fullName: string;
  email?: string;
  phone?: string;
  source: Source;
  sourceNote?: string;
  notes?: string;
  cv: File;
}

/** `createApplication` is `multipart/form-data`: empty optional fields are left out of the form. */
export async function createApplication({ cv, ...fields }: ApplicationInput) {
  const form = new FormData();
  Object.entries(fields).forEach(([name, value]) => value && form.append(name, value));
  form.append("cv", cv, cv.name);
  return unwrap(
    await apiClient.POST("/api/applications", {
      // The typed body only satisfies the contract; the serializer sends the form.
      body: { ...fields, cv: "" },
      bodySerializer: () => form,
    }),
  );
}

export async function matchCandidate(query: { email?: string; phone?: string; positionId?: string }) {
  return unwrap(await apiClient.GET("/api/candidates/match", { params: { query } }));
}
