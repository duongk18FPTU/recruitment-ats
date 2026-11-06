import type { Role } from "@ats/api-client";
import type { PositionSummary } from "./api";

/** Mirrors `x-roles` and BR-14 to hide actions; the API stays authoritative. */
export const canPickOwner = (role: Role) => role !== "INTERVIEWER"; // listUserOptions

/** `createPosition` x-roles: Admin, HR Manager, Recruiter (AC-US05 #3). */
export const canCreatePosition = (role: Role) => role !== "INTERVIEWER";

/** `updatePosition`: Admin and HR Manager edit any position, a Recruiter only their own (BR-14, AC-US06 #1). */
export const canEditPosition = (me: { id: string; role: Role }, position: Pick<PositionSummary, "owner">) =>
  me.role === "ADMIN" || me.role === "HR_MANAGER" || (me.role === "RECRUITER" && position.owner.id === me.id);

/** Only Admin and HR Manager reassign the owner (BR-14). */
export const canChangeOwner = (role: Role) => role === "ADMIN" || role === "HR_MANAGER";
