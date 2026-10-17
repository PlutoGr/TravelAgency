import type { ComponentPropsWithoutRef } from "react";
import { X } from "lucide-react";
import clsx from "clsx";

type CloseButtonProps = Omit<ComponentPropsWithoutRef<"button">, "children"> & {
  variant?: "default" | "overlay";
  floating?: boolean;
  iconSize?: number;
};

const variantClass = {
  default: "h-8 w-8 text-warm-gray hover:bg-sand hover:text-dark",
  overlay: "h-11 w-11 bg-white/10 text-white hover:bg-white/20",
} as const;

export default function CloseButton({
  variant = "default",
  floating = false,
  iconSize,
  className,
  type = "button",
  "aria-label": ariaLabel = "Закрыть",
  ...rest
}: CloseButtonProps) {
  const size = iconSize ?? (variant === "overlay" ? 22 : 20);

  return (
    <button
      type={type}
      aria-label={ariaLabel}
      className={clsx(
        "z-10 inline-flex shrink-0 cursor-pointer items-center justify-center rounded-full transition-colors",
        "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary",
        floating ? "absolute right-4 top-4" : "relative",
        variantClass[variant],
        className,
      )}
      {...rest}
    >
      {/* Иначе SVG ловит только штрих, а прозрачные пиксели крестика проваливаются сквозь кнопку. */}
      <X
        size={size}
        className="pointer-events-none"
        style={{ pointerEvents: "none" }}
        aria-hidden="true"
      />
    </button>
  );
}
