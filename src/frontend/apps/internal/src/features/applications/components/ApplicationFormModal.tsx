import { ApiError } from "@ats/api-client";
import { useId, useState, type ReactNode } from "react";
import { useForm, useWatch, type FieldError, type Path } from "react-hook-form";
import { Modal } from "../../../components/Modal";
import { useToast } from "../../../components/Toasts";
import { sourceLabels } from "../format";
import { useCandidateMatch, useCreateApplication } from "../queries";
import {
  applicationFormResolver,
  sources,
  type ApplicationFormInput,
  type ApplicationFormValues,
} from "../schemas";
import "../applications.css";

/**
 * SCR-21 add application (US09): upload a CV and confirm the candidate's details. While the recruiter types
 * an email or phone, a known candidate is shown (BR-06): the new application is attached to them, and a
 * candidate who already applied to this position cannot be added twice.
 */
export function ApplicationFormModal({
  position,
  onClose,
}: {
  position: { id: string; code: string; title: string };
  onClose: () => void;
}) {
  const formId = useId();
  const toast = useToast();
  const create = useCreateApplication();
  const [formError, setFormError] = useState<Error | null>(null);
  const form = useForm<ApplicationFormInput, unknown, ApplicationFormValues>({
    resolver: applicationFormResolver,
    defaultValues: { fullName: "", email: "", phone: "", source: undefined, sourceNote: "", notes: "" },
  });
  const { register, control, handleSubmit, setError, setValue, formState } = form;
  const errors = formState.errors;
  const [email, phone, source] = useWatch({ control, name: ["email", "phone", "source"] });
  const match = useCandidateMatch(email ?? "", phone ?? "", position.id);

  // Field errors from the API go under their fields; anything else is shown above the form.
  const fail = (error: Error) => {
    const fieldErrors = error instanceof ApiError ? Object.entries(error.fieldErrors) : [];
    const known = fieldErrors.filter(([field]) => field !== "positionId");
    if (known.length === 0) {
      setFormError(error);
      return;
    }
    known.forEach(([field, messages], index) =>
      setError(field as Path<ApplicationFormInput>, { message: messages[0] }, { shouldFocus: index === 0 }),
    );
  };

  const save = handleSubmit((values) => {
    setFormError(null);
    create.mutate(
      {
        positionId: position.id,
        fullName: values.fullName,
        email: values.email,
        phone: values.phone,
        source: values.source,
        sourceNote: values.source === "OTHER" ? values.sourceNote : "",
        notes: values.notes,
        cv: values.cv,
      },
      {
        onSuccess: (created) => {
          const name = created.application.candidate.fullName;
          toast(
            created.candidateMatched
              ? `Đã thêm hồ sơ của ${name} (ứng viên đã có trong hệ thống) vào ${position.code}.`
              : `Đã thêm hồ sơ của ${name} vào ${position.code}.`,
          );
          onClose();
        },
        onError: fail,
      },
    );
  });

  const alreadyApplied = match?.alreadyApplied === true;

  return (
    <Modal
      title="Thêm hồ sơ ứng tuyển"
      size="wide"
      onClose={onClose}
      footer={
        <>
          <button type="button" className="button button--secondary" onClick={onClose}>
            Huỷ
          </button>
          <button
            type="submit"
            form={formId}
            className="button button--primary"
            disabled={create.isPending || alreadyApplied}
          >
            {create.isPending ? "Đang lưu…" : "Thêm hồ sơ"}
          </button>
        </>
      }
    >
      <form id={formId} noValidate onSubmit={save}>
        <p className="application-form__position">
          Vị trí:{" "}
          <strong>
            {position.code} {position.title}
          </strong>
        </p>
        {formError && (
          <div role="alert" className="form-error">
            <p>{formError.message}</p>
          </div>
        )}

        <Field label="Tệp CV (PDF hoặc DOCX, tối đa 10 MB) *" error={errors.cv as FieldError | undefined}>
          {(props) => (
            <input
              {...props}
              onChange={(event) =>
                // react-hook-form hands a file input back as a FileList; the form wants the one File.
                setValue("cv", event.target.files?.[0] as File, { shouldValidate: formState.isSubmitted })
              }
              type="file"
              accept=".pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            />
          )}
        </Field>
        <Field label="Họ và tên *" error={errors.fullName}>
          {(props) => <input {...props} {...register("fullName")} autoComplete="off" maxLength={120} />}
        </Field>
        <div className="application-form__grid">
          <Field label="Email" error={errors.email}>
            {(props) => <input {...props} {...register("email")} type="email" autoComplete="off" maxLength={254} />}
          </Field>
          <Field label="Số điện thoại" error={errors.phone}>
            {(props) => <input {...props} {...register("phone")} type="tel" autoComplete="off" maxLength={20} />}
          </Field>
        </div>
        {match && (
          <div role="status" className={alreadyApplied ? "form-error" : "application-form__match"}>
            {alreadyApplied ? (
              <p>
                <strong>{match.candidate?.fullName}</strong> đã ứng tuyển vị trí này. Không thể thêm hồ sơ thứ hai.
              </p>
            ) : (
              <p>
                Ứng viên đã có trong hệ thống: <strong>{match.candidate?.fullName}</strong>
                {match.candidate?.email ? ` · ${match.candidate.email}` : ""}
                {match.candidate?.phone ? ` · ${match.candidate.phone}` : ""}. Hồ sơ mới sẽ gắn vào ứng viên này.
              </p>
            )}
          </div>
        )}
        <div className="application-form__grid">
          <Field label="Nguồn hồ sơ *" error={errors.source}>
            {(props) => (
              <select {...props} {...register("source")}>
                <option value="">Chọn nguồn</option>
                {sources.map((value) => (
                  <option key={value} value={value}>
                    {sourceLabels[value]}
                  </option>
                ))}
              </select>
            )}
          </Field>
          {source === "OTHER" && (
            <Field label="Ghi rõ nguồn *" error={errors.sourceNote}>
              {(props) => <input {...props} {...register("sourceNote")} autoComplete="off" maxLength={200} />}
            </Field>
          )}
        </div>
        <Field label="Ghi chú" error={errors.notes}>
          {(props) => <textarea {...props} {...register("notes")} rows={3} maxLength={5000} />}
        </Field>
      </form>
    </Modal>
  );
}

interface ControlProps {
  id: string;
  "aria-invalid"?: true;
  "aria-describedby"?: string;
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: FieldError;
  children: (props: ControlProps) => ReactNode;
}) {
  const id = useId();
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      {children({
        id,
        "aria-invalid": error ? true : undefined,
        "aria-describedby": error ? `${id}-error` : undefined,
      })}
      {error && (
        <p id={`${id}-error`} className="field__error">
          {error.message}
        </p>
      )}
    </div>
  );
}
