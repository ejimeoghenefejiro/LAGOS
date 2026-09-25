export default function AdminPlaceholderPage({ title }: { title: string }) {
  return (
    <div className="admin-panel">
      <h2>{title}</h2>
      <p className="admin-muted-text">This area is coming soon in the prototype.</p>
    </div>
  );
}
