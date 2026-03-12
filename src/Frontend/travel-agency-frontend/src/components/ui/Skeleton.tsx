import clsx from "clsx";

type SkeletonVariant = "text" | "circular" | "rectangular";

interface SkeletonProps {
  width?: string | number;
  height?: string | number;
  className?: string;
  variant?: SkeletonVariant;
}

export default function Skeleton({
  width,
  height,
  className,
  variant = "text",
}: SkeletonProps) {
  return (
    <div
      className={clsx(
        "animate-pulse bg-sand",
        variant === "circular" && "rounded-full",
        variant === "rectangular" && "rounded-[16px]",
        variant === "text" && "rounded-md",
        !height && variant === "text" && "h-4",
        className,
      )}
      style={{
        width: typeof width === "number" ? `${width}px` : width,
        height: typeof height === "number" ? `${height}px` : height,
      }}
    />
  );
}
