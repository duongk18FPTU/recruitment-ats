import type { Role } from "@ats/api-client";
import type { PositionStatus, PositionSummary } from "./api";

/** Mirrors `x-roles` and BR-14 to hide actions; the API stays authoritative. */
export const canPickOwner = (role: Role) => role !== "INTERVIEWER"; // listUserOptions

/** `createPosition` x-roles: Admin, HR Manager, Recruiter (AC-US05 #3). */
export const canCreatePosition = (role: Role) => role !== "INTERVIEWER";

/** `updatePosition`: Admin and HR Manager edit any position, a Recruiter only their own (BR-14, AC-US06 #1). */
export const canEditPosition = (me: { id: string; role: Role }, position: Pick<PositionSummary, "owner">) =>
  me.role === "ADMIN" || me.role === "HR_MANAGER" || (me.role === "RECRUITER" && position.owner.id === me.id);

/** Only Admin and HR Manager reassign the owner (BR-14). */
export const canChangeOwner = (role: Role) => role === "ADMIN" || role === "HR_MANAGER";

/**
 * Status moves the viewer may make (`updatePositionStatus`, US07): a draft is opened by Admin, HR Manager,
 * or its owner; closing and reopening are for Admin and HR Manager only (AC-US07 #2).
 */
export function statusTargets(
  me: { id: string; role: Role },
  position: Pick<PositionSummary, "status" | "owner">,
): PositionStatus[] {
  const manager = canChangeOwner(me.role);
  switch (position.status) {
    case "DRAFT":
      return manager || (me.role === "RECRUITER" && position.owner.id === me.id) ? ["OPEN"] : [];
    case "OPEN":
      return manager ? ["CLOSED"] : [];
    case "CLOSED":
      return manager ? ["OPEN"] : [];
  }
}

export function statusActionLabel(from: PositionStatus, to: PositionStatus): string {
  if (to === "CLOSED") {
    return "Đóng vị trí";
  }
  return from === "CLOSED" ? "Mở lại" : "Mở tuyển";
}
