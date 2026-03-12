import { Star } from "lucide-react";
import clsx from "clsx";

type StarSize = "sm" | "md";

interface StarRatingProps {
  rating: number;
  maxStars?: number;
  size?: StarSize;
}

const sizeMap: Record<StarSize, number> = {
  sm: 16,
  md: 20,
};

export default function StarRating({
  rating,
  maxStars = 5,
  size = "md",
}: StarRatingProps) {
  const iconSize = sizeMap[size];

  return (
    <div className="inline-flex items-center gap-0.5">
      {Array.from({ length: maxStars }, (_, i) => {
        const starIndex = i + 1;
        const isFull = rating >= starIndex;
        const isHalf = !isFull && rating >= starIndex - 0.5;

        return (
          <span key={i} className="relative">
            <Star
              size={iconSize}
              className="text-gray-200"
              fill="currentColor"
            />

            {(isFull || isHalf) && (
              <span
                className="absolute inset-0 overflow-hidden"
                style={{ width: isFull ? "100%" : "50%" }}
              >
                <Star
                  size={iconSize}
                  className={clsx("text-amber-400")}
                  fill="currentColor"
                />
              </span>
            )}
          </span>
        );
      })}
    </div>
  );
}
