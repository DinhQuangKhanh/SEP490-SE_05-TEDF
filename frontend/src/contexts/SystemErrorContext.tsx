import { createContext, useContext, useState, useCallback, type ReactNode } from "react";
import { SystemErrorModal } from "@/components/common/SystemErrorModal";

interface SystemErrorContextValue {
  showError: (message: string) => void;
}

const SystemErrorContext = createContext<SystemErrorContextValue | null>(null);

export function SystemErrorProvider({ children }: { children: ReactNode }) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const showError = useCallback((message: string) => {
    setErrorMessage(message);
  }, []);

  const dismiss = useCallback(() => {
    setErrorMessage(null);
  }, []);

  return (
    <SystemErrorContext.Provider value={{ showError }}>
      {children}
      <SystemErrorModal message={errorMessage} onClose={dismiss} />
    </SystemErrorContext.Provider>
  );
}

export function useSystemError(): SystemErrorContextValue {
  const ctx = useContext(SystemErrorContext);
  if (!ctx) throw new Error("useSystemError must be used inside SystemErrorProvider");
  return ctx;
}
