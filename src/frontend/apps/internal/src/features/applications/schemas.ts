import { zodResolver } from "@hookform/resolvers/zod";
import type { FieldErrors, Resolver } from "react-hook-form";
import { z } from "zod";
import type { Source } from "./api";

export const MAX_CV_BYTES = 10 * 1024 * 1024;

const PDF = "application/pdf";
const DOCX = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

/** BR-18, in the order of the source select. */
export const sources: readonly Source[] = [
  "LINKEDIN",
  "FACEBOOK",
  "TOPCV",
  "VIETNAMWORKS",
  "EMAIL",
  "REFERRAL",
  "CAREER_SITE",
  "OTHER",
];

export function isEmail(value: string): boolean {
  return value.length <= 254 && /^[^\s@]+@[^\s@]+$/.test(value);
}

/** A `File` by shape, so a file from another realm (a test double) counts too. */
function isFile(value: unknown): value is File {
  return typeof value === "object" && value !== null && "name" in value && "size" in value && "type" in value;
}

function hasCvExtension(file: File) {
  return file.type === PDF || file.type === DOCX || /\.(pdf|docx)$/i.test(file.name);
}

/**
 * SCR-21 form: mirrors OpenAPI `ApplicationCreateForm` for quick feedback (AC-US09 #4, #5); the API stays
 * authoritative, including the check that the file holds text.
 */
export const applicationFormSchema = z
  .object({
    fullName: z
      .string()
      .trim()
      .min(2, "Họ tên phải có từ 2 đến 120 ký tự.")
      .max(120, "Họ tên phải có từ 2 đến 120 ký tự."),
    email: z
      .string()
      .trim()
      .refine((value) => value === "" || isEmail(value), "Email không hợp lệ."),
    phone: z.string().trim().max(20, "Số điện thoại tối đa 20 ký tự."),
    source: z.custom<Source>((value) => sources.includes(value as Source), "Chọn nguồn hồ sơ."),
    sourceNote: z.string().trim().max(200, "Ghi chú nguồn tối đa 200 ký tự."),
    notes: z.string().trim().max(5000, "Ghi chú tối đa 5000 ký tự."),
    cv: z
      .custom<File>(isFile, "Chọn tệp CV (PDF hoặc DOCX).")
      .refine(hasCvExtension, "Chỉ chấp nhận tệp PDF hoặc DOCX.")
      .refine((file) => file.size <= MAX_CV_BYTES, "Tệp CV vượt quá 10 MB.")
      .refine((file) => file.size > 0, "Tệp CV trống."),
  });

/**
 * Rules between fields. Zod skips object-level checks while a field is invalid, so they run here, and the
 * recruiter sees every problem at once.
 */
function crossFieldErrors(values: Partial<ApplicationFormInput>): FieldErrors<ApplicationFormInput> {
  const errors: FieldErrors<ApplicationFormInput> = {};
  if (!values.email?.trim() && !values.phone?.trim()) {
    errors.email = { type: "custom", message: "Nhập email hoặc số điện thoại." };
  }
  if (values.source === "OTHER" && !values.sourceNote?.trim()) {
    errors.sourceNote = { type: "custom", message: "Ghi rõ nguồn khi chọn \"Khác\"." };
  }
  return errors;
}

export const applicationFormResolver: Resolver<ApplicationFormInput, unknown, ApplicationFormValues> = async (
  values,
  context,
  options,
) => {
  const result = await zodResolver(applicationFormSchema)(values, context, options);
  const extra = crossFieldErrors(values);
  return Object.keys(extra).length === 0 ? result : { values: {}, errors: { ...result.errors, ...extra } };
};

export type ApplicationFormInput = z.input<typeof applicationFormSchema>;
export type ApplicationFormValues = z.output<typeof applicationFormSchema>;
