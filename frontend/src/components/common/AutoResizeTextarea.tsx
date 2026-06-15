import React, { useEffect, useRef } from "react";

interface AutoResizeTextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  value: string;
  maxRows?: number;
}

export function AutoResizeTextarea({ value, maxRows = 20, className = "", ...props }: AutoResizeTextareaProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    const textarea = textareaRef.current;
    if (textarea) {
      // Reset height to auto to get the correct scrollHeight when text is deleted
      textarea.style.height = "auto";
      
      // Calculate line height approximately based on computed styles, or just use CSS max-height
      // We will rely on Tailwind's max-h-[x] or a calculated max-height
      // The easiest way is to let the CSS max-height handle the maxRows limitation.
      
      textarea.style.height = `${textarea.scrollHeight}px`;
    }
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
