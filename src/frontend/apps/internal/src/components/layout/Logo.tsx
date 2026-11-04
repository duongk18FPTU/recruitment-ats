/** Brand mark (docs/05-ui-ux.md, "Brand"): pine square, three narrowing bars, the hire as a light dot. */
export function Logo({ size = 32 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <rect width="32" height="32" rx="8" fill="var(--color-brand)" />
      <rect x="8" y="9" width="16" height="3" rx="1.5" fill="#fff" />
      <rect x="8" y="14.5" width="12" height="3" rx="1.5" fill="#fff" />
      <rect x="8" y="20" width="7" height="3" rx="1.5" fill="#fff" />
      <circle cx="21" cy="21.5" r="2.5" fill="var(--color-brand-accent)" />
    </svg>
  );
}
