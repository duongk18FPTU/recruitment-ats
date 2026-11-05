import chevronLeft from "../assets/icons/chevron-left.svg";
import chevronRight from "../assets/icons/chevron-right.svg";
import "./components.css";

const PAGE_SIZES = [20, 50, 100] as const;

interface PaginationProps {
  page: number;
  pageSize: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

/** List footer (SCR-70): page size, previous / numbered pages / next, right-aligned. */
export function Pagination(props: PaginationProps) {
  const { page, pageSize, totalPages, onPageChange, onPageSizeChange } = props;

  return (
    <nav className="pagination" aria-label="Phân trang">
      <select
        className="pagination__size"
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
        <img src={chevronLeft} width={16} height={16} alt="" />
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
        <img src={chevronRight} width={16} height={16} alt="" />
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
