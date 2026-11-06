import { createContext, useContext, useEffect, useState, type ReactNode } from "react";

const BreadcrumbContext = createContext<{
  item: string | undefined;
  setItem: (item: string | undefined) => void;
} | null>(null);

/** Holds the last breadcrumb item of a page below a top-level page (for example a position's title). */
export function BreadcrumbProvider({ children }: { children: ReactNode }) {
  const [item, setItem] = useState<string>();
  return <BreadcrumbContext.Provider value={{ item, setItem }}>{children}</BreadcrumbContext.Provider>;
}

export function useBreadcrumbItemLabel(): string | undefined {
  return useContext(BreadcrumbContext)?.item;
}

/** Names the current record in the topbar breadcrumb while the page is shown; `undefined` while loading. */
export function useBreadcrumbItem(label: string | undefined) {
  const setItem = useContext(BreadcrumbContext)?.setItem;
  useEffect(() => {
    setItem?.(label);
    return () => setItem?.(undefined);
  }, [label, setItem]);
}
