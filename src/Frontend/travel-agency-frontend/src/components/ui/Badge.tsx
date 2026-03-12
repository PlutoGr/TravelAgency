import clsx from "clsx";
import type { ReactNode } from "react";

type BadgeVariant = "blue" | "green" | "amber" | "purple" | "gray" | "red";
type BadgeSize = "sm" | "md";

interface BadgeProps {
  variant?: BadgeVariant;
  size?: BadgeSize;
  children: ReactNode;
}

const variantStyles: Record<BadgeVariant, string> = {
  blue: "bg-blue-100 text-blue-700",
  green: "bg-emerald-100 text-emerald-700",
  amber: "bg-amber-100 text-amber-700",
  purple: "bg-purple-100 text-purple-700",
  gray: "bg-gray-100 text-gray-600",
  red: "bg-red-100 text-red-700",
};

const sizeStyles: Record<BadgeSize, string> = {
  sm: "px-2 py-0.5 text-xs",
  md: "px-3 py-1 text-sm",
};

export default function Badge({
  variant = "blue",
  size = "md",
  children,
}: BadgeProps) {
  return (
    <span
      className={clsx(
        "inline-flex items-center rounded-full font-medium",
        variantStyles[variant],
        sizeStyles[size],
      )}
    >
      {children}
    </span>
  );
}
