import { useCallback, useRef, useEffect, useState } from "react";
import clsx from "clsx";

interface RangeSliderProps {
  min: number;
  max: number;
  value: [number, number];
  onChange: (value: [number, number]) => void;
  step?: number;
  formatLabel?: (val: number) => string;
}

export default function RangeSlider({
  min,
  max,
  value,
  onChange,
  step = 1,
  formatLabel = (v) => String(v),
}: RangeSliderProps) {
  const trackRef = useRef<HTMLDivElement>(null);
  const [dragging, setDragging] = useState<"min" | "max" | null>(null);

  const toPercent = useCallback(
    (val: number) => ((val - min) / (max - min)) * 100,
    [min, max],
  );

  const fromClientX = useCallback(
    (clientX: number): number => {
      const track = trackRef.current;
      if (!track) return min;
      const rect = track.getBoundingClientRect();
      const ratio = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
      const raw = min + ratio * (max - min);
      return Math.round(raw / step) * step;
    },
    [min, max, step],
  );

  const handlePointerDown = useCallback(
    (handle: "min" | "max") => (e: React.PointerEvent) => {
      e.preventDefault();
      setDragging(handle);
      (e.target as HTMLElement).setPointerCapture(e.pointerId);
    },
    [],
  );

  const handlePointerMove = useCallback(
    (e: React.PointerEvent) => {
      if (!dragging) return;
      const val = fromClientX(e.clientX);
      if (dragging === "min") {
        onChange([Math.min(val, value[1] - step), value[1]]);
      } else {
        onChange([value[0], Math.max(val, value[0] + step)]);
      }
    },
    [dragging, fromClientX, onChange, value, step],
  );

  const handlePointerUp = useCallback(() => {
    setDragging(null);
  }, []);

  useEffect(() => {
    if (dragging) {
      const onUp = () => setDragging(null);
      window.addEventListener("pointerup", onUp);
      return () => window.removeEventListener("pointerup", onUp);
    }
  }, [dragging]);

  const leftPercent = toPercent(value[0]);
  const rightPercent = toPercent(value[1]);

  return (
    <div className="w-full px-2 py-4">
      <div className="mb-3 flex justify-between text-sm font-medium text-dark">
        <span className="rounded-lg bg-sand px-2.5 py-1">{formatLabel(value[0])}</span>
        <span className="rounded-lg bg-sand px-2.5 py-1">{formatLabel(value[1])}</span>
      </div>

      <div
        ref={trackRef}
        className="relative h-2 w-full cursor-pointer rounded-full bg-sand"
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
      >
        <div
          className="absolute top-0 h-full rounded-full bg-primary"
          style={{
            left: `${leftPercent}%`,
            width: `${rightPercent - leftPercent}%`,
          }}
        />

        <div
          role="slider"
          tabIndex={0}
          aria-valuemin={min}
          aria-valuemax={value[1]}
          aria-valuenow={value[0]}
          onPointerDown={handlePointerDown("min")}
          className={clsx(
            "absolute top-1/2 -translate-x-1/2 -translate-y-1/2",
            "h-5 w-5 rounded-full border-2 border-primary bg-white shadow-md transition-shadow",
            "hover:shadow-lg focus:outline-none focus:ring-2 focus:ring-primary/30",
            dragging === "min" && "scale-110",
          )}
          style={{ left: `${leftPercent}%` }}
        />

        <div
          role="slider"
          tabIndex={0}
          aria-valuemin={value[0]}
          aria-valuemax={max}
          aria-valuenow={value[1]}
          onPointerDown={handlePointerDown("max")}
          className={clsx(
            "absolute top-1/2 -translate-x-1/2 -translate-y-1/2",
            "h-5 w-5 rounded-full border-2 border-primary bg-white shadow-md transition-shadow",
            "hover:shadow-lg focus:outline-none focus:ring-2 focus:ring-primary/30",
            dragging === "max" && "scale-110",
          )}
          style={{ left: `${rightPercent}%` }}
        />
      </div>
    </div>
  );
}
