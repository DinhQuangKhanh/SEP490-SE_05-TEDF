import { createContext, useContext, useEffect, useState, useCallback, ReactNode } from "react";
import { settingsService } from "@/lib";
import { useMaintenance } from "./MaintenanceContext";

interface BrandingValue {
  primaryColor: string;
  headerName: string;
  logoUrl: string;
  version: string;
  /** Re-fetch public settings and re-apply branding (call after an admin saves). */
  refresh: () => Promise<void>;
}

const BrandingContext = createContext<BrandingValue | undefined>(undefined);

const DEFAULT_COLOR = "#2c6090";

function adjustColor(color: string, amount: number) {
  const hex = color.replace("#", "");
  const r = Math.max(0, Math.min(255, parseInt(hex.slice(0, 2), 16) + amount));
  const g = Math.max(0, Math.min(255, parseInt(hex.slice(2, 4), 16) + amount));
  const b = Math.max(0, Math.min(255, parseInt(hex.slice(4, 6), 16) + amount));
  return `#${r.toString(16).padStart(2, "0")}${g.toString(16).padStart(2, "0")}${b.toString(16).padStart(2, "0")}`;
}

function applyColor(color: string) {
  document.documentElement.style.setProperty("--color-primary", color);
  document.documentElement.style.setProperty("--color-primary-dark", adjustColor(color, -20));
  document.documentElement.style.setProperty("--color-primary-light", adjustColor(color, 20));
}

/**
 * Loads the public (server-side) branding + maintenance settings once at startup and applies them
 * for EVERY user — primary color, header/brand name (document title), logo, and maintenance state.
 * Falls back to the locally cached color if the API is unreachable.
 */
export function BrandingProvider({ children }: { children: ReactNode }) {
  const { setMaintenanceMode } = useMaintenance();
  const [branding, setBranding] = useState<Omit<BrandingValue, "refresh">>({
    primaryColor: localStorage.getItem("themeColor") || DEFAULT_COLOR,
    headerName: "TEDF",
    logoUrl: "",
    version: "",
  });

  const load = useCallback(async () => {
    try {
      const s = await settingsService.getPublicSettings();
      applyColor(s.primaryColor);
      localStorage.setItem("themeColor", s.primaryColor);
      document.title = s.headerName || "TEDF";
      setMaintenanceMode(s.maintenanceMode);
      setBranding({
        primaryColor: s.primaryColor,
        headerName: s.headerName || "TEDF",
        logoUrl: s.logoUrl || "",
        version: s.version,
      });
    } catch {
      // Offline / API down: keep the localStorage color already applied by App.
    }
  }, [setMaintenanceMode]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <BrandingContext.Provider value={{ ...branding, refresh: load }}>
      {children}
    </BrandingContext.Provider>
  );
}

export function useBranding(): BrandingValue {
  const ctx = useContext(BrandingContext);
  if (ctx === undefined) {
    throw new Error("useBranding must be used within a BrandingProvider");
  }
  return ctx;
}
