import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Heart, MapPin, Clock } from 'lucide-react';
import clsx from 'clsx';
import type { Tour } from '@/types';
import { Card, Badge, StarRating, Button } from '@/components/ui';
import { useFavoritesStore } from '@/store/favoritesStore';

interface TourCardProps {
  tour: Tour;
}

function formatPrice(price: number): string {
  return price.toLocaleString('ru-RU') + ' ₽';
}

export default function TourCard({ tour }: TourCardProps) {
  const { toggleFavorite, isFavorite } = useFavoritesStore();
  const [imgLoaded, setImgLoaded] = useState(false);
  const favorite = isFavorite(tour.id);

  return (
    <Card hover className="group flex h-full flex-col">
      <div className="relative aspect-[4/3] overflow-hidden">
        <Link to={`/tours/${tour.id}`} className="block h-full">
          <div
            className={clsx(
              'absolute inset-0 bg-sand transition-opacity duration-300',
              imgLoaded ? 'opacity-0' : 'opacity-100',
            )}
          />
          <img
            src={tour.photos[0]}
            alt={tour.title}
            onLoad={() => setImgLoaded(true)}
            className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
          />
        </Link>

        {tour.isHot && (
          <div className="absolute left-3 top-3">
            <Badge variant="red" size="sm">
              <span className="flex items-center gap-1">
                🔥 Горящий
              </span>
            </Badge>
          </div>
        )}

        <motion.button
          whileTap={{ scale: 0.8 }}
          onClick={(e) => {
            e.preventDefault();
            toggleFavorite(tour.id);
          }}
          className="absolute right-3 top-3 flex h-9 w-9 items-center justify-center rounded-full bg-white/80 backdrop-blur-sm transition-colors hover:bg-white"
        >
          <AnimatePresence mode="wait">
            <motion.div
              key={favorite ? 'filled' : 'empty'}
              initial={{ scale: 0.5 }}
              animate={{ scale: 1 }}
              exit={{ scale: 0.5 }}
              transition={{ type: 'spring', stiffness: 500, damping: 20 }}
            >
              <Heart
                size={18}
                className={clsx(
                  'transition-colors',
                  favorite ? 'fill-red-500 text-red-500' : 'text-dark/60',
                )}
              />
            </motion.div>
          </AnimatePresence>
        </motion.button>

        {tour.originalPrice && (
          <div className="absolute bottom-3 left-3 rounded-lg bg-olive px-2.5 py-1 text-xs font-semibold text-white">
            −{Math.round(((tour.originalPrice - tour.price) / tour.originalPrice) * 100)}%
          </div>
        )}
      </div>

      <div className="flex flex-1 flex-col p-4">
        <div className="mb-2 flex items-center gap-3 text-xs text-warm-gray">
          <span className="flex items-center gap-1">
            <MapPin size={13} />
            {tour.country}, {tour.city}
          </span>
          <span className="flex items-center gap-1">
            <Clock size={13} />
            {tour.duration} дн.
          </span>
        </div>

        <Link to={`/tours/${tour.id}`} className="group/title mb-2">
          <h3 className="line-clamp-2 font-heading text-base font-semibold text-dark transition-colors group-hover/title:text-primary">
            {tour.title}
          </h3>
        </Link>

        <div className="mb-3 flex items-center gap-2">
          <StarRating rating={tour.rating} size="sm" />
          <span className="text-xs text-warm-gray">
            {tour.rating} ({tour.reviewCount})
          </span>
        </div>

        <div className="mt-auto flex items-end justify-between">
          <div>
            {tour.originalPrice && (
              <span className="block text-xs text-warm-gray line-through">
                {formatPrice(tour.originalPrice)}
              </span>
            )}
            <span className="font-heading text-lg font-bold text-primary">
              {formatPrice(tour.price)}
            </span>
          </div>

          <Link to={`/tours/${tour.id}`}>
            <Button variant="secondary" size="sm">
              Подробнее
            </Button>
          </Link>
        </div>
      </div>
    </Card>
  );
}
