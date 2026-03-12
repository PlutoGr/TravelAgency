import { motion } from "framer-motion";
import { Loader2 } from "lucide-react";
import clsx from "clsx";
import type { ComponentPropsWithoutRef, ReactNode } from "react";

type ButtonVariant = "primary" | "secondary" | "terracotta" | "ghost" | "danger";
type ButtonSize = "sm" | "md" | "lg";

interface ButtonProps extends ComponentPropsWithoutRef<"button"> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  isLoading?: boolean;
  leftIcon?: ReactNode;
  rightIcon?: ReactNode;
  fullWidth?: boolean;
}

const variantStyles: Record<ButtonVariant, string> = {
  primary:
    "bg-primary text-white shadow-button hover:bg-primary-light active:bg-primary-dark",
  secondary:
    "border-2 border-primary text-primary hover:bg-primary hover:text-white",
  terracotta:
    "bg-terracotta text-white hover:bg-terracotta-light active:bg-terracotta-dark",
  ghost:
    "text-dark hover:bg-sand",
  danger:
    "bg-red-500 text-white hover:bg-red-600 active:bg-red-700",
};

const sizeStyles: Record<ButtonSize, string> = {
  sm: "px-4 py-2 text-sm gap-1.5",
  md: "px-6 py-2.5 text-base gap-2",
  lg: "px-8 py-3.5 text-lg gap-2.5",
};

export default function Button({
  variant = "primary",
  size = "md",
  isLoading = false,
  leftIcon,
  rightIcon,
  fullWidth = false,
  children,
  disabled,
  className,
  // Exclude handlers that conflict with framer-motion
  onAnimationStart: _onAnimStart,
  onDrag: _onDrag,
  onDragEnd: _onDragEnd,
  onDragStart: _onDragStart,
  ...rest
}: ButtonProps) {
  return (
    <motion.button
      whileHover={disabled || isLoading ? undefined : { scale: 1.02 }}
      whileTap={disabled || isLoading ? undefined : { scale: 0.98 }}
      disabled={disabled || isLoading}
      className={clsx(
        "inline-flex items-center justify-center font-heading font-semibold rounded-[12px] transition-colors cursor-pointer",
        "disabled:opacity-50 disabled:cursor-not-allowed",
        variantStyles[variant],
        sizeStyles[size],
        fullWidth && "w-full",
        className,
      )}
      {...rest}
    >
      {isLoading ? (
        <Loader2 className="animate-spin" size={size === "sm" ? 16 : size === "lg" ? 22 : 18} />
      ) : (
        leftIcon
      )}
      {children}
      {!isLoading && rightIcon}
    </motion.button>
  );
}
