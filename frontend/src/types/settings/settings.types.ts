// ── Types ──────────────────────────────────────────────────
/** Non-secret branding + maintenance subset, readable by any client at startup. */
export interface PublicSettings {
  primaryColor: string;
  headerName: string;
  logoUrl: string;
  maintenanceMode: boolean;
  version: string;
}

/** A single admin-editable system setting row. */
export interface SystemSetting {
  key: string;
  value: string;
  dataType: string;
  description?: string | null;
  category?: string | null;
}
