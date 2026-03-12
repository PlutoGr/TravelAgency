import { motion } from "framer-motion";
import clsx from "clsx";
import type { ReactNode } from "react";

interface CardProps {
  className?: string;
  children: ReactNode;
  onClick?: () => void;
  hover?: boolean;
}

export default function Card({
  className,
  children,
  onClick,
  hover = false,
}: CardProps) {
  return (
    <motion.div
      whileHover={
        hover
          ? { y: -4, boxShadow: "0 12px 40px rgba(0, 0, 0, 0.12)" }
          : undefined
      }
      transition={{ type: "spring", stiffness: 300, damping: 25 }}
      onClick={onClick}
      className={clsx(
        "rounded-[16px] bg-white shadow-card overflow-hidden",
        onClick && "cursor-pointer",
        className,
      )}
    >
      {children}
    </motion.div>
  );
}
