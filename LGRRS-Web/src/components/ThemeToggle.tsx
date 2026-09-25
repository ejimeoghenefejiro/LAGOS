import { useState } from "react";
import { Link } from "react-router-dom";

export default function ThemeToggle() {
  const [dark, setDark] = useState(() => document.documentElement.dataset.theme === "dark");
  function toggle() {
    const next = !dark;
    setDark(next);
    document.documentElement.dataset.theme = next ? "dark" : "light";
    try { localStorage.setItem("lgrrs.theme", next ? "dark" : "light"); } catch { /* Storage is optional. */ }
  }
  return (
    <div className="theme-toolbar">
      <Link to="/">Switch portal</Link>
      <button className="theme-toggle" type="button" onClick={toggle} aria-pressed={dark} aria-label="Dark mode">
        <span aria-hidden="true">{dark ? "☾" : "☀"}</span>
        {dark ? "Dark mode" : "Light mode"}
      </button>
    </div>
  );
}
