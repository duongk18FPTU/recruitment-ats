import "./components.css";

const PAGE_SIZES = [20, 50, 100] as const;

interface PaginationProps {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  /** Noun after the count, e.g. "người dùng" → "1–7 trong 7 người dùng". */
  noun: string;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

/** List footer (SCR-70): range, page size, previous / numbered pages / next. */
export function Pagination(props: PaginationProps) {
  const { page, pageSize, totalItems, totalPages, noun, onPageChange, onPageSizeChange } = props;
  const first = totalItems === 0 ? 0 : (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, totalItems);

  return (
    <nav className="pagination" aria-label="Phân trang">
      <p className="pagination__range">
        {first}–{last} trong {totalItems} {noun}
      </p>
      <select
        className="select"
        aria-label="Số dòng mỗi trang"
        value={pageSize}
        onChange={(event) => onPageSizeChange(Number(event.target.value))}
      >
        {PAGE_SIZES.map((size) => (
          <option key={size} value={size}>
            {size} / trang
          </option>
        ))}
      </select>
      <button
        type="button"
        className="pagination__button"
        aria-label="Trang trước"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        ‹
      </button>
      {pageNumbers(page, Math.max(totalPages, 1)).map((number, index) =>
        number === null ? (
          <span key={`gap-${index}`} className="pagination__gap" aria-hidden="true">
            …
          </span>
        ) : (
          <button
            key={number}
            type="button"
            className="pagination__button"
            aria-label={`Trang ${number}`}
            aria-current={number === page ? "page" : undefined}
            onClick={() => onPageChange(number)}
          >
            {number}
          </button>
        ),
      )}
      <button
        type="button"
        className="pagination__button"
        aria-label="Trang sau"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        ›
      </button>
    </nav>
  );
}

/** 1 … 4 5 6 … 12: the first, last, and neighbours of the current page; `null` is a gap. */
export function pageNumbers(page: number, totalPages: number): (number | null)[] {
  const shown = [...new Set([1, page - 1, page, page + 1, totalPages])]
    .filter((n) => n >= 1 && n <= totalPages)
    .sort((a, b) => a - b);
  const result: (number | null)[] = [];
  for (const n of shown) {
    const previous = result[result.length - 1];
    if (typeof previous === "number" && n - previous > 1) {
      result.push(n - previous === 2 ? previous + 1 : null);
    }
    result.push(n);
  }
  return result;
}
