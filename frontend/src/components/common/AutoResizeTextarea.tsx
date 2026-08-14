import React, { useEffect, useRef } from "react";

interface AutoResizeTextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  value: string;
  maxRows?: number;
}

export function AutoResizeTextarea({ value, maxRows = 20, className = "", ...props }: AutoResizeTextareaProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    const textarea = textareaRef.current;
    if (!textarea) return;

    // Reset to auto first, otherwise scrollHeight can only ever grow.
    textarea.style.height = "auto";

    // Tailwind's preflight makes every element border-box, so `height` has to cover the borders —
    // but scrollHeight measures the content box only. Without adding them back the field ends up a
    // couple of pixels short of its own text and the browser draws a scrollbar that never goes away.
    const styles = window.getComputedStyle(textarea);
    const borders = parseFloat(styles.borderTopWidth) + parseFloat(styles.borderBottomWidth);

    textarea.style.height = `${textarea.scrollHeight + borders}px`;
  }, [value]);

  return (
    <textarea
      ref={textareaRef}
      value={value}
      // Calculate approximate max height (e.g. 24px per line + padding) 
      // Tailwind text-sm is 20px line-height. py-2.5 is 20px (10px top + 10px bottom).
      // So max height = 20 * 20px + 20px = 420px.
      style={{ maxHeight: "420px", ...props.style }}
      className={`overflow-y-auto resize-none ${className}`}
      {...props}
    />
  );
}
