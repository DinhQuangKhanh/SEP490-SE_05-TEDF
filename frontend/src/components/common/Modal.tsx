import { useEffect, useRef, type ReactNode } from "react";
import { createPortal } from "react-dom";

const DEFAULT_CONTENT_CLASS =
  "w-full max-w-2xl p-6 bg-white shadow-2xl rounded-xl max-h-[90vh] overflow-y-auto";

/**
 * Dimmed overlay modal that closes on backdrop click and on Escape. The close listeners are attached
 * natively (not via JSX onClick on a non-interactive div) so the markup stays accessibility-clean,
 * and a single target check replaces the usual inner `stopPropagation` wrapper.
 */
export function Modal({
  onClose,
  children,
  contentClassName = DEFAULT_CONTENT_CLASS,
}: {
  onClose: () => void;
  children: ReactNode;
  contentClassName?: string;
}) {
  const backdropRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [onClose]);

  useEffect(() => {
    const el = backdropRef.current;
    if (!el) return;
    const onClick = (e: MouseEvent) => {
      if (e.target === el) onClose(); // only when the dimmed area itself is clicked, not its content
    };
    el.addEventListener("click", onClick);
    return () => el.removeEventListener("click", onClick);
  }, [onClose]);

  return createPortal(
    <div ref={backdropRef} className="fixed inset-0 z-[60] bg-black/50 flex items-center justify-center p-4">
      <div className={contentClassName} role="dialog" aria-modal="true">
        {children}
      </div>
    </div>,
    document.body,
  );
}
