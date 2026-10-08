import { useState } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { Heart, MapPin, Clock } from 'lucide-react';
import clsx from 'clsx';
import type { Tour, TourImage } from '@/types';
import { Card, Badge } from '@/components/ui';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { formatDate, formatFromMoney } from '@/utils/format';
import { isTourAvailable, tourTypeLabel } from '@/utils/tourLabels';
import ResponsiveTourImage from './ResponsiveTourImage';

interface TourCardProps {
  tour: Tour;
  priority?: boolean;
}

function coverOf(tour: Tour): TourImage | undefined {
  return tour.images?.find((image) => image.isCover) ?? tour.images?.[0];
}

export default function TourCard({ tour, priority = false }: TourCardProps) {
  const { toggleFavorite, isFavorite } = useFavoritesStore();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const openAuthModal = useUIStore((s) => s.openAuthModal);
  const [imgLoaded, setImgLoaded] = useState(false);
  const favorite = isFavorite(tour.id);
  const available = isTourAvailable(tour);
  const cover = coverOf(tour);
  const priceFrom = tour.priceFrom ?? (tour.price > 0 ? tour.price : null);
  const typeLabel = tourTypeLabel(tour.category);

  const favoriteButton = (
    <motion.button
      type="button"
      whileTap={{ scale: 0.8 }}
      aria-label={favorite ? 'Убрать из избранного' : 'Добавить в избранное'}
      onClick={(e) => {
        e.preventDefault();
        if (!isAuthenticated) {
          openAuthModal('login');
          return;
        }
        toggleFavorite(tour.id, tour);
      }}
      className="absolute right-3 top-3 z-10 flex h-11 w-11 items-center justify-center rounded-full bg-white/80 backdrop-blur-sm transition-colors hover:bg-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
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
  );

  return (
    <Card hover={available} className="group flex h-full min-w-0 flex-col">
      <div className="relative aspect-[4/3] overflow-hidden">
        {available ? (
          <Link to={`/tours/${tour.id}`} className="block h-full">
            <div
              className={clsx(
                'absolute inset-0 bg-sand transition-opacity duration-300',
                imgLoaded ? 'opacity-0' : 'opacity-100',
              )}
            />
            <ResponsiveTourImage
              mediaFileId={cover?.mediaFileId}
              fallbackUrl={tour.photos[0]}
              alt={cover?.alt || tour.title}
              variant="card"
              priority={priority}
              onLoad={() => setImgLoaded(true)}
              className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
            />
          </Link>
        ) : (
          <div className="block h-full">
            <ResponsiveTourImage
              mediaFileId={cover?.mediaFileId}
              fallbackUrl={tour.photos[0]}
              alt={cover?.alt || tour.title}
              variant="card"
              onLoad={() => setImgLoaded(true)}
              className="h-full w-full object-cover grayscale"
            />
          </div>
        )}

        {available && typeLabel && (
          <div className="absolute left-3 top-3">
            <Badge variant="blue" size="sm">
              {typeLabel}
            </Badge>
          </div>
        )}

        {!available && (
          <div className="absolute left-3 top-3">
            <Badge variant="amber" size="sm">
              Тур недоступен
            </Badge>
          </div>
        )}

        {favoriteButton}
      </div>

      <div className="flex flex-1 flex-col p-4">
        <div className="mb-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-warm-gray">
          {tour.country && (
            <span className="flex items-center gap-1">
              <MapPin size={13} />
              {tour.country}
            </span>
          )}
          {tour.duration > 0 && (
            <span className="flex items-center gap-1">
              <Clock size={13} />
              {tour.duration} дн.
            </span>
          )}
        </div>

        {available ? (
          <Link to={`/tours/${tour.id}`} className="group/title mb-2 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary">
            <h3 className="line-clamp-2 font-heading text-base font-semibold text-dark transition-colors group-hover/title:text-primary">
              {tour.title}
            </h3>
          </Link>
        ) : (
          <h3 className="mb-2 line-clamp-2 font-heading text-base font-semibold text-dark">
            {tour.title}
          </h3>
        )}

        {tour.departureCity && available && (
          <p className="mb-2 text-xs text-warm-gray">Вылет из {tour.departureCity}</p>
        )}

        {tour.shortDescription && available && (
          <p className="mb-3 line-clamp-2 text-sm text-dark/70">{tour.shortDescription}</p>
        )}

        {!available && (
          <p className="mb-3 text-sm font-medium text-amber-800">Тур недоступен</p>
        )}

        <div className="mt-auto flex items-end justify-between gap-3">
          <div className="min-w-0">
            {available && priceFrom != null && (
              <span className="font-heading text-lg font-bold text-primary">
                {formatFromMoney(priceFrom, tour.currency)}
              </span>
            )}
            {available && tour.nearestDate && (
              <span className="mt-0.5 block text-xs text-warm-gray">
                ближайший выезд {formatDate(tour.nearestDate, 'd MMM')}
              </span>
            )}
          </div>

          {available && (
            <Link
              to={`/tours/${tour.id}`}
              className="inline-flex shrink-0 items-center rounded-[12px] border-2 border-primary px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-primary hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              Подробнее
            </Link>
          )}
        </div>
      </div>
    </Card>
  );
}
