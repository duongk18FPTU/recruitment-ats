import type { Role } from "@ats/api-client";

/** Mirrors `x-roles` and BR-14 to hide actions; the API stays authoritative. */
export const canPickOwner = (role: Role) => role !== "INTERVIEWER"; // listUserOptions
