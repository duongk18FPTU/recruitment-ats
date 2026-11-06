import { zodResolver } from "@hookform/resolvers/zod";
import { ApiError } from "@ats/api-client";
import { useId, useState, type ReactNode } from "react";
import { useFieldArray, useForm, type FieldError, type Path } from "react-hook-form";
import plusIcon from "../../../assets/icons/plus.svg";
import { Modal } from "../../../components/Modal";
import { useToast } from "../../../components/Toasts";
import { useDepartments } from "../../lookups";
import type { PositionCreateRequest } from "../api";
import { criterionTypeLabels, employmentTypeLabels, levelLabels } from "../format";
import { useCreatePosition } from "../queries";
import {
  MAX_CRITERIA,
  formPath,
  newCriterion,
  positionFormSchema,
  toPositionFields,
  type PositionFormInput,
  type PositionFormValues,
} from "../schemas";

const WEIGHTS = [1, 2, 3, 4, 5];

interface PositionFormModalProps {
  /** The viewer's calendar date (yyyy-MM-dd): the target hire date must be after it. */
  today: string;
  onClose: () => void;
}

/** SCR-11 Tạo vị trí tuyển dụng (US05): general information and weighted criteria; save as a draft or open. */
export function PositionFormModal({ today, onClose }: PositionFormModalProps) {
  const formId = useId();
  const toast = useToast();
  const departments = useDepartments();
  const create = useCreatePosition();
  const [formError, setFormError] = useState<Error | null>(null);
  const form = useForm<PositionFormInput, unknown, PositionFormValues>({
    resolver: zodResolver(positionFormSchema(today)),
    defaultValues: {
      title: "",
      departmentId: "",
      location: "",
      employmentType: "FULL_TIME",
      level: undefined,
      headcount: 1,
      targetHireDate: "",
      description: "",
      criteria: [newCriterion()],
    },
  });
  const { register, control, handleSubmit, setError, formState } = form;
  const criteria = useFieldArray({ control, name: "criteria" });
  const errors = formState.errors;

  // Field errors from the API go under their fields; anything else is shown above the form.
  const fail = (error: Error) => {
    const fieldErrors = error instanceof ApiError ? Object.entries(error.fieldErrors) : [];
    if (fieldErrors.length === 0) {
      setFormError(error);
      return;
    }
    fieldErrors.forEach(([field, messages], index) =>
      setError(formPath(field) as Path<PositionFormInput>, { message: messages[0] }, { shouldFocus: index === 0 }),
    );
  };

  const save = (status: NonNullable<PositionCreateRequest["status"]>) =>
    handleSubmit((values) => {
      setFormError(null);
      create.mutate(
        { ...toPositionFields(values), status },
        {
          onSuccess: (created) => {
            toast(
              status === "OPEN"
                ? `Đã tạo và mở tuyển ${created.code} ${created.title}.`
                : `Đã lưu nháp ${created.code} ${created.title}.`,
            );
            onClose();
          },
          onError: fail,
        },
      );
    });

  return (
    <Modal
      title="Tạo vị trí tuyển dụng"
      size="wide"
      onClose={onClose}
      footer={
        <>
          <button type="button" className="button button--secondary" onClick={onClose}>
            Huỷ
          </button>
          <button type="button" className="button button--secondary" disabled={create.isPending} onClick={save("DRAFT")}>
            Lưu nháp
          </button>
          <button type="button" className="button button--primary" disabled={create.isPending} onClick={save("OPEN")}>
            {create.isPending ? "Đang lưu…" : "Lưu và mở tuyển"}
          </button>
        </>
      }
    >
      <form id={formId} noValidate onSubmit={save("DRAFT")}>
        {formError && (
          <div role="alert" className="form-error">
            <p>{formError.message}</p>
          </div>
        )}

        <h3 className="position-form__section">Thông tin chung</h3>
        <Field label="Tên vị trí *" error={errors.title}>
          {(props) => <input {...props} {...register("title")} autoComplete="off" maxLength={150} />}
        </Field>
        <div className="position-form__grid">
          <Field label="Phòng ban *" error={errors.departmentId}>
            {(props) => (
              <select {...props} {...register("departmentId")}>
                <option value="">Chọn phòng ban</option>
                {departments.data?.map((department) => (
                  <option key={department.id} value={department.id}>
                    {department.name}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field label="Địa điểm *" error={errors.location}>
            {(props) => <input {...props} {...register("location")} placeholder="Hà Nội" maxLength={100} />}
          </Field>
          <Field label="Hình thức *" error={errors.employmentType}>
            {(props) => (
              <select {...props} {...register("employmentType")}>
                {Object.entries(employmentTypeLabels).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field label="Cấp bậc *" error={errors.level}>
            {(props) => (
              <select {...props} {...register("level")}>
                <option value="">Chọn cấp bậc</option>
                {Object.entries(levelLabels).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field label="Số lượng *" error={errors.headcount}>
            {(props) => <input {...props} {...register("headcount", { valueAsNumber: true })} type="number" min={1} />}
          </Field>
          <Field label="Hạn tuyển *" error={errors.targetHireDate}>
            {(props) => <input {...props} {...register("targetHireDate")} type="date" min={today} />}
          </Field>
        </div>
        <Field label="Mô tả công việc *" error={errors.description}>
          {(props) => <textarea {...props} {...register("description")} rows={4} />}
        </Field>

        <div className="position-form__criteria-header">
          <h3 className="position-form__section">Tiêu chí đánh giá</h3>
          <button
            type="button"
            className="link-button position-form__add"
            disabled={criteria.fields.length >= MAX_CRITERIA}
            onClick={() => criteria.append(newCriterion())}
          >
            <img src={plusIcon} width={16} height={16} alt="" />
            Thêm tiêu chí
          </button>
        </div>
        {errors.criteria?.root && (
          <p role="alert" className="field__error position-form__criteria-error">
            {errors.criteria.root.message}
          </p>
        )}
        {errors.criteria?.message && (
          <p role="alert" className="field__error position-form__criteria-error">
            {errors.criteria.message}
          </p>
        )}
        <table className="position-form__criteria">
          <thead>
            <tr>
              <th scope="col">Tiêu chí</th>
              <th scope="col">Loại</th>
              <th scope="col">Trọng số</th>
              <th scope="col">
                <span className="visually-hidden">Xoá</span>
              </th>
            </tr>
          </thead>
          <tbody>
            {criteria.fields.map((field, index) => {
              const rowErrors = errors.criteria?.[index];
              const number = index + 1;
              return (
                <tr key={field.id}>
                  <td>
                    <CellInput label={`Tiêu chí ${number}`} error={rowErrors?.name}>
                      {(props) => <input {...props} {...register(`criteria.${index}.name`)} maxLength={150} />}
                    </CellInput>
                  </td>
                  <td>
                    <CellInput label={`Loại tiêu chí ${number}`} error={rowErrors?.type}>
                      {(props) => (
                        <select {...props} {...register(`criteria.${index}.type`)}>
                          {Object.entries(criterionTypeLabels).map(([value, label]) => (
                            <option key={value} value={value}>
                              {label}
                            </option>
                          ))}
                        </select>
                      )}
                    </CellInput>
                  </td>
                  <td>
                    <CellInput label={`Trọng số tiêu chí ${number}`} error={rowErrors?.weight}>
                      {(props) => (
                        <select {...props} {...register(`criteria.${index}.weight`, { valueAsNumber: true })}>
                          {WEIGHTS.map((weight) => (
                            <option key={weight} value={weight}>
                              {weight}
                            </option>
                          ))}
                        </select>
                      )}
                    </CellInput>
                  </td>
                  <td>
                    <button
                      type="button"
                      className="position-form__remove"
                      aria-label={`Xoá tiêu chí ${number}`}
                      onClick={() => criteria.remove(index)}
                    >
                      <svg width="16" height="16" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                        <path d="M6 6l12 12M18 6L6 18" fill="none" stroke="currentColor" strokeWidth="1.6" />
                      </svg>
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
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

/** A control in a criteria row: the column header is visible, so the label is for screen readers only. */
function CellInput({
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
    <div className="field position-form__cell">
      <label htmlFor={id} className="visually-hidden">
        {label}
      </label>
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
