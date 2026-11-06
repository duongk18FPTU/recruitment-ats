import { ApiError } from "@ats/api-client";
import { useState, type ReactNode } from "react";
import { Link, useParams } from "react-router";
import { useAuth } from "../../auth";
import type { PositionDetail } from "../api";
import {
  criterionTypeLabels,
  employmentTypeLabels,
  formatDate,
  formatDateTime,
  levelLabels,
  stageLabels,
  statusTag,
  todayIn,
} from "../format";
import { usePosition } from "../queries";
import "../positions.css";

type Tab = "overview" | "criteria";

/**
 * SCR-12 position detail (US04): the Overview and Criteria tabs. The Job post (US22) and Pipeline
 * (US08) tabs come with their stories.
 */
export function PositionDetailPage() {
  const { positionId = "" } = useParams();
  const position = usePosition(positionId);
  const [tab, setTab] = useState<Tab>("overview");
  const auth = useAuth();
  const timeZone = (auth.status === "signed-in" ? auth.user.timezone : undefined) ?? "Asia/Ho_Chi_Minh";

  if (position.isPending) {
    return <p className="page-status">Đang tải vị trí…</p>;
  }
  if (position.isError) {
    const notFound = position.error instanceof ApiError && position.error.status === 404;
    return (
      <div role="alert" className="page-status">
        <p>{notFound ? "Không tìm thấy vị trí tuyển dụng." : position.error.message}</p>
        {notFound ? (
          <Link to="/jobs">Về danh sách vị trí</Link>
        ) : (
          <button type="button" className="button button--secondary" onClick={() => void position.refetch()}>
            Thử lại
          </button>
        )}
      </div>
    );
  }

  const p = position.data;
  const tag = statusTag(p.status, p.targetHireDate, todayIn(timeZone));

  return (
    <section className="position">
      <header className="position__header">
        <h1>
          {p.title} <span className={`tag tag--${tag.tone}`}>{tag.label}</span>
        </h1>
        <p className="position__meta">
          <span className="positions__code">{p.code}</span> · {p.department.name} · {p.location} · Hạn tuyển{" "}
          {formatDate(p.targetHireDate)} · Đã tuyển {p.hiredCount} / {p.headcount}
        </p>
      </header>

      <div className="position__tabs" role="tablist" aria-label="Thông tin vị trí">
        <TabButton id="overview" current={tab} onSelect={setTab}>
          Tổng quan
        </TabButton>
        <TabButton id="criteria" current={tab} onSelect={setTab}>
          Tiêu chí · {p.criteria.length}
        </TabButton>
      </div>

      <div role="tabpanel" id={`position-${tab}`} aria-labelledby={`position-tab-${tab}`}>
        {tab === "overview" ? <Overview position={p} timeZone={timeZone} /> : <Criteria position={p} />}
      </div>
    </section>
  );
}

function TabButton({
  id,
  current,
  onSelect,
  children,
}: {
  id: Tab;
  current: Tab;
  onSelect: (tab: Tab) => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      role="tab"
      id={`position-tab-${id}`}
      aria-selected={current === id}
      aria-controls={`position-${id}`}
      className="position__tab"
      onClick={() => onSelect(id)}
    >
      {children}
    </button>
  );
}

function Overview({ position: p, timeZone }: { position: PositionDetail; timeZone: string }) {
  const stages = p.stageCounts ?? [];
  return (
    <div className="position__overview">
      <dl className="position__facts">
        <Fact label="Phòng ban">{p.department.name}</Fact>
        <Fact label="Địa điểm">{p.location}</Fact>
        <Fact label="Hình thức">{employmentTypeLabels[p.employmentType]}</Fact>
        <Fact label="Cấp bậc">{levelLabels[p.level]}</Fact>
        <Fact label="Số lượng">{p.headcount}</Fact>
        <Fact label="Hạn tuyển">{formatDate(p.targetHireDate)}</Fact>
        <Fact label="Người phụ trách">{p.owner.fullName}</Fact>
        <Fact label="Ngày tạo">{formatDateTime(p.createdAt, timeZone)}</Fact>
        {p.closedAt && <Fact label="Ngày đóng">{formatDateTime(p.closedAt, timeZone)}</Fact>}
        <Fact label="Hồ sơ">
          {p.applicationCount === 0
            ? "Chưa có hồ sơ"
            : stages.map((s) => `${stageLabels[s.stage]} ${s.count}`).join(" · ")}
        </Fact>
      </dl>
      <h2 className="position__subtitle">Mô tả công việc</h2>
      <p className="position__description">{p.description}</p>
    </div>
  );
}

function Fact({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{children}</dd>
    </div>
  );
}

function Criteria({ position: p }: { position: PositionDetail }) {
  return (
    <div className="position__criteria">
      <p className="position__hint">Phiên bản tiêu chí {p.criteriaVersion}</p>
      <table className="positions__table">
        <thead>
          <tr>
            <th scope="col">Tiêu chí</th>
            <th scope="col">Loại</th>
            <th scope="col" className="positions__number">
              Trọng số
            </th>
          </tr>
        </thead>
        <tbody>
          {p.criteria.map((criterion) => (
            <tr key={criterion.id}>
              <td>
                <span className="positions__title">{criterion.name}</span>
                {criterion.description && <span className="positions__meta">{criterion.description}</span>}
              </td>
              <td>{criterionTypeLabels[criterion.type]}</td>
              <td className="positions__number">{criterion.weight}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
