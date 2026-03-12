import { useState } from "react";
import clsx from "clsx";
import type { InputHTMLAttributes } from "react";
import type { LucideIcon } from "lucide-react";

interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, "size"> {
  label: string;
  error?: string;
  icon?: LucideIcon;
}

export default function Input({
  label,
  error,
  icon: Icon,
  className,
  id,
  value,
  defaultValue,
  onFocus,
  onBlur,
  ...rest
}: InputProps) {
  const [focused, setFocused] = useState(false);
  const hasValue = value !== undefined ? String(value).length > 0 : false;
  const isFloating = focused || hasValue || (defaultValue !== undefined && String(defaultValue).length > 0);
  const inputId = id ?? label.toLowerCase().replace(/\s+/g, "-");

  return (
    <div className={clsx("relative w-full", className)}>
      <div className="relative">
        {Icon && (
          <Icon
            size={18}
            className={clsx(
              "absolute left-4 top-1/2 -translate-y-1/2 transition-colors",
              focused ? "text-primary" : "text-warm-gray",
            )}
          />
        )}

        <input
          id={inputId}
          value={value}
          defaultValue={defaultValue}
          className={clsx(
            "peer w-full rounded-[12px] border bg-white px-4 pt-5 pb-2 text-dark outline-none transition-all",
            "placeholder-transparent",
            Icon && "pl-11",
            error
              ? "border-red-400 focus:border-red-500 focus:ring-2 focus:ring-red-100"
              : "border-sand focus:border-primary focus:ring-2 focus:ring-primary/10",
          )}
          placeholder={label}
          onFocus={(e) => {
            setFocused(true);
            onFocus?.(e);
          }}
          onBlur={(e) => {
            setFocused(false);
            onBlur?.(e);
          }}
          {...rest}
        />

        <label
          htmlFor={inputId}
          className={clsx(
            "absolute transition-all pointer-events-none text-warm-gray",
            Icon ? "left-11" : "left-4",
            isFloating
              ? "top-1.5 text-xs font-medium text-primary"
              : "top-1/2 -translate-y-1/2 text-base",
          )}
        >
          {label}
        </label>
      </div>

      {error && (
        <p className="mt-1.5 text-sm text-red-500">{error}</p>
      )}
    </div>
  );
}
