import type { FraudRiskSummary } from "../api/types";

const RADIUS = 54;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

export default function DonutChart({ summary }: { summary: FraudRiskSummary }) {
  const total = summary.high + summary.medium + summary.low;
  const segments = [
    { label: "High Risk", value: summary.high, className: "donut-high" },
    { label: "Medium Risk", value: summary.medium, className: "donut-medium" },
    { label: "Low Risk", value: summary.low, className: "donut-low" }
  ];

  let offset = 0;

  return (
    <div className="donut-wrap">
      <svg viewBox="0 0 140 140" className="donut-chart">
        <circle cx="70" cy="70" r={RADIUS} className="donut-track" />
        {total > 0 && segments.map((seg) => {
          const fraction = seg.value / total;
          const dash = fraction * CIRCUMFERENCE;
          const circle = (
            <circle
              key={seg.label}
              cx="70"
              cy="70"
              r={RADIUS}
              className={`donut-segment ${seg.className}`}
              strokeDasharray={`${dash} ${CIRCUMFERENCE - dash}`}
              strokeDashoffset={-offset}
            />
          );
          offset += dash;
          return circle;
        })}
        <text x="70" y="66" textAnchor="middle" className="donut-total">{total}</text>
        <text x="70" y="82" textAnchor="middle" className="donut-total-label">Total Blocked</text>
      </svg>
      <ul className="donut-legend">
        {segments.map((seg) => (
          <li key={seg.label} className={seg.className}>
            <span className="donut-legend-dot" />
            <span className="donut-legend-label">{seg.label}</span>
            <span className="donut-legend-value">{seg.value} ({total > 0 ? Math.round((seg.value / total) * 100) : 0}%)</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
