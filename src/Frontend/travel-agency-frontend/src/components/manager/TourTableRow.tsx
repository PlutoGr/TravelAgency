import { motion } from 'framer-motion';
import { Edit3, Trash2, Star, MapPin, Flame } from 'lucide-react';
import type { Tour } from '@/types';
import { Badge } from '@/components/ui';
import { formatPrice } from '@/utils/format';

interface TourTableRowProps {
  tour: Tour;
  index: number;
  onEdit: (tour: Tour) => void;
  onDelete: (tourId: string) => void;
}

export default function TourTableRow({
  tour,
  index,
  onEdit,
  onDelete,
}: TourTableRowProps) {
  return (
    <motion.tr
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.03 }}
      className="border-b border-sand/60 transition-colors last:border-0 hover:bg-cream/40"
    >
      <td className="px-5 py-3">
        {tour.photos?.[0] ? (
          <img
            src={tour.photos[0]}
            alt={tour.title}
            className="h-12 w-16 rounded-lg object-cover"
          />
        ) : (
          <div className="h-12 w-16 rounded-lg bg-sand" aria-hidden />
        )}
      </td>
      <td className="px-5 py-3">
        <div className="flex items-center gap-2">
          <span className="font-medium text-dark">{tour.title}</span>
          {tour.isHot && (
            <Badge variant="red" size="sm">
              <span className="flex items-center gap-0.5">
                <Flame size={10} />
                Горящий
              </span>
            </Badge>
          )}
        </div>
      </td>
      <td className="px-5 py-3 text-warm-gray">
        <span className="flex items-center gap-1">
          <MapPin size={14} />
          {tour.country}
        </span>
      </td>
      <td className="px-5 py-3 font-semibold text-primary">
        {formatPrice(tour.price)}
      </td>
      <td className="px-5 py-3">
        <span className="flex items-center gap-1 text-warm-gray">
          <Star size={14} className="fill-amber-400 text-amber-400" />
          {tour.rating}
        </span>
      </td>
      <td className="px-5 py-3">
        <Badge variant="gray" size="sm">
          {tour.category}
        </Badge>
      </td>
      <td className="px-5 py-3">
        <div className="flex items-center gap-2">
          <button
            onClick={() => onEdit(tour)}
            className="rounded-lg p-1.5 text-warm-gray transition-colors hover:bg-sand hover:text-primary"
            title="Редактировать"
          >
            <Edit3 size={16} />
          </button>
          <button
            onClick={() => onDelete(tour.id)}
            className="rounded-lg p-1.5 text-warm-gray transition-colors hover:bg-red-50 hover:text-red-500"
            title="Удалить"
          >
            <Trash2 size={16} />
          </button>
        </div>
      </td>
    </motion.tr>
  );
}
