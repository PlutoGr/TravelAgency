import { useState, useRef, useEffect } from "react";
import { ChevronDown } from "lucide-react";
import { motion, AnimatePresence } from "framer-motion";
import clsx from "clsx";

interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps {
  options: SelectOption[];
  value?: string;
  onChange: (value: string) => void;
  placeholder?: string;
  label?: string;
}

export default function Select({
  options,
  value,
  onChange,
  placeholder = "Выберите...",
  label,
}: SelectProps) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const selected = options.find((o) => o.value === value);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <div ref={ref} className="relative w-full">
      {label && (
        <label className="mb-1.5 block text-xs font-medium text-primary">
          {label}
        </label>
      )}

      <button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        className={clsx(
          "flex w-full items-center justify-between rounded-[12px] border bg-white px-4 py-3 text-left transition-all",
          open
            ? "border-primary ring-2 ring-primary/10"
            : "border-sand hover:border-warm-gray",
        )}
      >
        <span className={clsx(selected ? "text-dark" : "text-warm-gray")}>
          {selected?.label ?? placeholder}
        </span>
        <ChevronDown
          size={18}
          className={clsx(
            "text-warm-gray transition-transform",
            open && "rotate-180",
          )}
        />
      </button>

      <AnimatePresence>
        {open && (
          <motion.ul
            initial={{ opacity: 0, y: -8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            transition={{ duration: 0.15 }}
            className="absolute z-20 mt-1.5 w-full overflow-hidden rounded-[12px] border border-sand bg-white py-1 shadow-card"
          >
            {options.map((option) => (
              <li key={option.value}>
                <button
                  type="button"
                  onClick={() => {
                    onChange(option.value);
                    setOpen(false);
                  }}
                  className={clsx(
                    "w-full px-4 py-2.5 text-left text-sm transition-colors",
                    option.value === value
                      ? "bg-primary/5 font-medium text-primary"
                      : "text-dark hover:bg-sand",
                  )}
                >
                  {option.label}
                </button>
              </li>
            ))}
          </motion.ul>
        )}
      </AnimatePresence>
    </div>
  );
}
