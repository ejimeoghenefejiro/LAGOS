import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import "./index.css";
import "./tokens.css";
import "./theme.css";
import App from "./App.tsx";

try {
  document.documentElement.dataset.theme = localStorage.getItem("lgrrs.theme") === "dark" ? "dark" : "light";
} catch {
  document.documentElement.dataset.theme = "light";
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>
);
