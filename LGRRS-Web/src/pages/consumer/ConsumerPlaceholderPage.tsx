export default function ConsumerPlaceholderPage({ title }: { title: string }) {
  return (
    <div className="consumer-card">
      <h2 className="consumer-heading">{title}</h2>
      <p className="consumer-muted">This area is coming soon in the prototype.</p>
    </div>
  );
}
