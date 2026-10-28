import { useEffect } from "react";
import { createPortal } from "react-dom";
import { motion, AnimatePresence } from "framer-motion";
import clsx from "clsx";
import type { ReactNode } from "react";
import CloseButton from "./CloseButton";

type ModalSize = "sm" | "md" | "lg";

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
  size?: ModalSize;
  dialogLabel?: string;
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
  dialogLabel,
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
            role={dialogLabel ? "dialog" : undefined}
            aria-modal={dialogLabel ? true : undefined}
            aria-label={dialogLabel}
            className={clsx(
              "relative w-full rounded-[20px] bg-white shadow-modal",
              sizeStyles[size],
            )}
          >
            {title && (
              <div className="relative z-10 flex items-center justify-between border-b border-sand px-6 py-4">
                <h2 className="font-heading text-lg font-semibold text-dark">
                  {title}
                </h2>
                <CloseButton onClick={onClose} />
              </div>
            )}

            {/* Кнопка раньше контента в дереве. У контента бывают positioned-потомки
                (вкладки с relative): при z-index: auto они рисуются поверх крестика. */}
            {!title && <CloseButton floating onClick={onClose} />}

            <div className={title ? "p-6" : "px-6 pb-6 pt-14"}>{children}</div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>,
    document.body,
  );
}
