import clsx from "clsx";

type AvatarSize = "sm" | "md" | "lg" | "xl";

interface AvatarProps {
  src?: string;
  name: string;
  size?: AvatarSize;
}

const sizeStyles: Record<AvatarSize, string> = {
  sm: "h-8 w-8 text-xs",
  md: "h-10 w-10 text-sm",
  lg: "h-14 w-14 text-base",
  xl: "h-20 w-20 text-xl",
};

const bgColors = [
  "bg-primary",
  "bg-terracotta",
  "bg-olive",
  "bg-amber-500",
  "bg-purple-500",
  "bg-rose-500",
];

function getInitials(name: string): string {
  return name
    .split(" ")
    .map((part) => part[0])
    .filter(Boolean)
    .slice(0, 2)
    .join("")
    .toUpperCase();
}

function getColorFromName(name: string): string {
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return bgColors[Math.abs(hash) % bgColors.length];
}

export default function Avatar({ src, name, size = "md" }: AvatarProps) {
  if (src) {
    return (
      <img
        src={src}
        alt={name}
        className={clsx(
          "rounded-full object-cover",
          sizeStyles[size],
        )}
      />
    );
  }

  return (
    <div
      className={clsx(
        "flex items-center justify-center rounded-full font-heading font-semibold text-white",
        sizeStyles[size],
        getColorFromName(name),
      )}
      title={name}
    >
      {getInitials(name)}
    </div>
  );
}
