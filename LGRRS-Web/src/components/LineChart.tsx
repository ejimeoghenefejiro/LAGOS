import type { WeeklyPoint } from "../api/types";

export default function LineChart({ points }: { points: WeeklyPoint[] }) {
  if (points.length === 0) return null;

  const width = 640;
  const height = 200;
  const padding = 32;
  const max = Math.max(...points.map((p) => p.count), 1);
  const step = (width - padding * 2) / Math.max(points.length - 1, 1);

  const coords = points.map((p, i) => {
    const x = padding + i * step;
    const y = height - padding - (p.count / max) * (height - padding * 1.5);
    return { x, y, ...p };
  });

  const linePath = coords.map((c, i) => `${i === 0 ? "M" : "L"}${c.x},${c.y}`).join(" ");
  const areaPath = `${linePath} L${coords[coords.length - 1].x},${height - padding} L${coords[0].x},${height - padding} Z`;

  const yTicks = [0, 0.25, 0.5, 0.75, 1].map((f) => Math.round(max * f));

  return (
    <svg viewBox={`0 0 ${width} ${height}`} className="line-chart" preserveAspectRatio="none">
      {yTicks.map((t, i) => {
        const y = height - padding - (t / max) * (height - padding * 1.5);
        return (
          <g key={i}>
            <line x1={padding} y1={y} x2={width - 8} y2={y} className="line-chart-grid" />
            <text x={4} y={y + 4} className="line-chart-axis">{t}</text>
          </g>
        );
      })}
      <path d={areaPath} className="line-chart-area" />
      <path d={linePath} className="line-chart-line" />
      {coords.map((c, i) => (
        <g key={i}>
          <circle cx={c.x} cy={c.y} r={3.5} className="line-chart-dot" />
          <text x={c.x} y={c.y - 10} textAnchor="middle" className="line-chart-value">{c.count.toLocaleString()}</text>
          <text x={c.x} y={height - 8} textAnchor="middle" className="line-chart-axis">{c.weekLabel}</text>
        </g>
      ))}
    </svg>
  );
}
