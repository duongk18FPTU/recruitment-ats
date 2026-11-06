import { z } from "zod";
import type { PositionCreateRequest, PositionDetail } from "./api";

export const MAX_CRITERIA = 30;

const employmentTypes = ["FULL_TIME", "PART_TIME", "CONTRACT", "INTERNSHIP"] as const;
const levels = ["INTERN", "FRESHER", "JUNIOR", "MIDDLE", "SENIOR", "LEAD", "MANAGER"] as const;

/**
 * SCR-11 form: mirrors OpenAPI `PositionFields` for quick feedback (AC-US05 #2); the API stays
 * authoritative. `today` is the viewer's calendar date (yyyy-MM-dd).
 */
export function positionFormSchema(today: string) {
  return z.object({
    title: z
      .string()
      .trim()
      .min(2, "Tên vị trí phải có từ 2 đến 150 ký tự.")
      .max(150, "Tên vị trí phải có từ 2 đến 150 ký tự."),
    departmentId: z.string().min(1, "Chọn phòng ban."),
    location: z.string().trim().min(1, "Nhập địa điểm.").max(100, "Địa điểm tối đa 100 ký tự."),
    employmentType: z.enum(employmentTypes, { error: "Chọn hình thức." }),
    level: z.enum(levels, { error: "Chọn cấp bậc." }),
    headcount: z.number({ error: "Nhập số lượng." }).int("Số lượng là số nguyên.").min(1, "Số lượng phải từ 1 trở lên."),
    targetHireDate: z
      .string()
      .min(1, "Chọn hạn tuyển.")
      .refine((date) => date > today, "Hạn tuyển phải sau hôm nay."),
    description: z.string().trim().min(1, "Nhập mô tả công việc.").max(20000, "Mô tả công việc tối đa 20.000 ký tự."),
    criteria: z
      .array(
        z.object({
          id: z.string().optional(),
          name: z
            .string()
            .trim()
            .min(2, "Tên tiêu chí phải có từ 2 đến 150 ký tự.")
            .max(150, "Tên tiêu chí phải có từ 2 đến 150 ký tự."),
          description: z.string().nullable().optional(),
          type: z.enum(["MUST_HAVE", "NICE_TO_HAVE"], { error: "Chọn loại tiêu chí." }),
          weight: z.number().int().min(1, "Trọng số phải từ 1 đến 5.").max(5, "Trọng số phải từ 1 đến 5."),
        }),
      )
      .min(1, "Thêm ít nhất một tiêu chí đánh giá.")
      .max(MAX_CRITERIA, `Tối đa ${MAX_CRITERIA} tiêu chí.`),
    /** Editing only, for Admin and HR Manager. */
    ownerId: z.string().optional(),
  });
}

export type PositionFormInput = z.input<ReturnType<typeof positionFormSchema>>;
export type PositionFormValues = z.output<ReturnType<typeof positionFormSchema>>;

export const newCriterion = (): PositionFormInput["criteria"][number] => ({ name: "", type: "MUST_HAVE", weight: 3 });

/** The form filled with a position being edited; criteria keep their ids and descriptions. */
export function formValuesOf(position: PositionDetail): PositionFormInput {
  return {
    title: position.title,
    departmentId: position.department.id,
    location: position.location,
    employmentType: position.employmentType,
    level: position.level,
    headcount: position.headcount,
    targetHireDate: position.targetHireDate,
    description: position.description,
    criteria: position.criteria.map(({ id, name, description, type, weight }) => ({ id, name, description, type, weight })),
    ownerId: position.owner.id,
  };
}

/** The request fields shared by create and update. */
export function toPositionFields(values: PositionFormValues): Omit<PositionCreateRequest, "status"> {
  return {
    title: values.title,
    departmentId: values.departmentId,
    location: values.location,
    employmentType: values.employmentType,
    level: values.level,
    headcount: values.headcount,
    targetHireDate: values.targetHireDate,
    description: values.description,
    criteria: values.criteria.map((criterion) => ({ ...criterion, description: criterion.description ?? null })),
  };
}

/** API field paths (`criteria[0].weight`) as form paths (`criteria.0.weight`). */
export function formPath(apiField: string): string {
  return apiField.replace(/\[(\d+)\]/g, ".$1");
}
