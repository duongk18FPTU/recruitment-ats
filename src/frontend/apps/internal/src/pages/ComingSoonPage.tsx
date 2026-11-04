/** Stands in for a page until its story is built (docs/13-progress.md). */
export function ComingSoonPage({ title }: { title: string }) {
  return (
    <section className="page-message">
      <h1>{title}</h1>
      <p>Chức năng này đang được xây dựng.</p>
    </section>
  );
}
