import { useEffect } from "react";
import { createPortal } from "react-dom";
import { motion, AnimatePresence } from "framer-motion";
import { X } from "lucide-react";
import clsx from "clsx";
import type { ReactNode } from "react";

type ModalSize = "sm" | "md" | "lg";

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
  size?: ModalSize;
}

const sizeStyles: Record<ModalSize, string> = {
  sm: "max-w-md",
  md: "max-w-lg",
  lg: "max-w-2xl",
};

export default function Modal({
  isOpen,
  onClose,
  title,
  children,
  size = "md",
}: ModalProps) {
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = "hidden";
    }
    return () => {
      document.body.style.overflow = "";
    };
  }, [isOpen]);

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    if (isOpen) {
      document.addEventListener("keydown", handleKeyDown);
    }
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  return createPortal(
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="absolute inset-0 bg-dark/40 backdrop-blur-sm"
            onClick={onClose}
          />

          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: "spring", stiffness: 350, damping: 30 }}
            className={clsx(
              "relative w-full rounded-[20px] bg-white shadow-modal",
              sizeStyles[size],
            )}
          >
            {title && (
              <div className="flex items-center justify-between border-b border-sand px-6 py-4">
                <h2 className="font-heading text-lg font-semibold text-dark">
                  {title}
                </h2>
                <button
                  onClick={onClose}
                  className="rounded-full p-1.5 text-warm-gray transition-colors hover:bg-sand hover:text-dark"
                >
                  <X size={20} />
                </button>
              </div>
            )}

            {!title && (
              <button
                onClick={onClose}
                className="absolute right-4 top-4 rounded-full p-1.5 text-warm-gray transition-colors hover:bg-sand hover:text-dark"
              >
                <X size={20} />
              </button>
            )}

            <div className="p-6">{children}</div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>,
    document.body,
  );
}
