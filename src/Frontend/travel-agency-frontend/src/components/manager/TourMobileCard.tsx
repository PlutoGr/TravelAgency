import { motion } from 'framer-motion';
import { Edit3, Trash2, Star, MapPin, Flame } from 'lucide-react';
import type { Tour } from '@/types';
import { Card, Badge } from '@/components/ui';
import { formatPrice } from '@/utils/format';
import { mediaImageUrl } from '@/utils/media';

interface TourMobileCardProps {
  tour: Tour;
  index: number;
  onEdit: (tour: Tour) => void;
  onDelete: (tourId: string) => void;
}

export default function TourMobileCard({
  tour,
  index,
  onEdit,
  onDelete,
}: TourMobileCardProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.04 }}
    >
      <Card className="overflow-hidden">
        <div className="flex gap-3 p-4">
          {tour.coverMediaFileId ? (
            <img
              src={mediaImageUrl(tour.coverMediaFileId, 'w200')}
              alt={tour.title}
              className="h-20 w-20 shrink-0 rounded-[12px] object-cover"
            />
          ) : (
            <div
              className="h-20 w-20 shrink-0 rounded-[12px] bg-sand"
              aria-hidden
            />
          )}
          <div className="min-w-0 flex-1">
            <div className="flex items-start gap-2">
              <h3 className="line-clamp-1 text-sm font-semibold text-dark">
                {tour.title}
              </h3>
              {tour.isHot && (
                <Badge variant="red" size="sm">
                  <Flame size={10} />
                </Badge>
              )}
            </div>
            <p className="mt-0.5 flex items-center gap-1 text-xs text-warm-gray">
              <MapPin size={12} />
              {tour.country}, {tour.city}
            </p>
            <div className="mt-1 flex items-center gap-3">
              <span className="text-sm font-bold text-primary">
                {formatPrice(tour.price)}
              </span>
              <span className="flex items-center gap-0.5 text-xs text-warm-gray">
                <Star size={12} className="fill-amber-400 text-amber-400" />
                {tour.rating}
              </span>
            </div>
          </div>
        </div>
        <div className="flex border-t border-sand">
          <button
            onClick={() => onEdit(tour)}
            className="flex flex-1 items-center justify-center gap-1.5 py-2.5 text-sm text-warm-gray transition-colors hover:text-primary"
          >
            <Edit3 size={14} />
            Изменить
          </button>
          <div className="w-px bg-sand" />
          <button
            onClick={() => onDelete(tour.id)}
            className="flex flex-1 items-center justify-center gap-1.5 py-2.5 text-sm text-warm-gray transition-colors hover:text-red-500"
          >
            <Trash2 size={14} />
            Удалить
          </button>
        </div>
      </Card>
    </motion.div>
  );
}
