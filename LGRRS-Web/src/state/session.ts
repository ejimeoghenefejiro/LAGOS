import { createContext, useContext } from "react";

export interface Session {
  accessToken: string;
  role: string;
  displayName: string;
}

interface SessionContextValue {
  session: Session | null;
  setSession: (session: Session | null) => void;
}

export const SessionContext = createContext<SessionContextValue>({
  session: null,
  setSession: () => {}
});

export const useSession = () => useContext(SessionContext);

const STORAGE_KEY = "lgrrs.session";

export function loadStoredSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as Session) : null;
  } catch {
    return null;
  }
}

export function persistSession(session: Session | null): void {
  try {
    if (session) {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } else {
      sessionStorage.removeItem(STORAGE_KEY);
    }
  } catch {
    // Storage unavailable (private mode, etc.) — session just won't survive a reload.
  }
}
