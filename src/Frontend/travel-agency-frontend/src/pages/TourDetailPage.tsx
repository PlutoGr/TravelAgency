import { useState, useMemo, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ChevronRight,
  MapPin,
  Heart,
  Minus,
  Plus,
  MessageCircle,
  Hotel,
  Clock,
} from 'lucide-react';
import clsx from 'clsx';
import { format, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';
import type { Tour } from '@/types';
import { PageTransition, FadeInOnScroll } from '@/components/common';
import { Button, StarRating, Skeleton, Badge, Select } from '@/components/ui';
import { TourGallery, TourTabs, TourCard } from '@/components/tour';
import { getTourById, getTours } from '@/api/catalog';
import { useFavoritesStore } from '@/store/favoritesStore';

function formatPrice(price: number): string {
  return price.toLocaleString('ru-RU') + ' ₽';
}

function DetailSkeleton() {
  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <Skeleton className="mb-6 h-4 w-48" />
      <div className="grid gap-8 lg:grid-cols-[1fr_360px]">
        <div className="space-y-6">
          <Skeleton variant="rectangular" className="aspect-[16/10] w-full" />
          <Skeleton className="h-8 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton variant="rectangular" className="h-64 w-full" />
        </div>
        <div className="space-y-4">
          <Skeleton variant="rectangular" className="h-80 w-full" />
        </div>
      </div>
    </div>
  );
}

function BookingSidebar({ tour }: { tour: Tour }) {
  const { toggleFavorite, isFavorite } = useFavoritesStore();
  const [travelers, setTravelers] = useState(1);
  const [selectedDateIndex, setSelectedDateIndex] = useState<string>('0');
  const favorite = isFavorite(tour.id);

  const dateOptions = useMemo(
    () =>
      tour.dates.map((d, i) => ({
        value: String(i),
        label: `${format(parseISO(d.start), 'd MMM', { locale: ru })} — ${format(parseISO(d.end), 'd MMM', { locale: ru })}`,
      })),
    [tour.dates],
  );

  const totalPrice = tour.price * travelers;

  const decrementTravelers = useCallback(() => {
    setTravelers((p) => Math.max(1, p - 1));
  }, []);

  const incrementTravelers = useCallback(() => {
    setTravelers((p) => Math.min(tour.maxTravelers, p + 1));
  }, [tour.maxTravelers]);

  return (
    <div className="sticky top-24 space-y-5 rounded-[16px] bg-white p-6 shadow-card">
      {/* Price */}
      <div>
        {tour.originalPrice && (
          <span className="text-sm text-warm-gray line-through">
            {formatPrice(tour.originalPrice)}
          </span>
        )}
        <div className="flex items-baseline gap-2">
          <span className="font-heading text-3xl font-bold text-primary">
            {formatPrice(tour.price)}
          </span>
          <span className="text-sm text-warm-gray">/ чел.</span>
        </div>
        {tour.originalPrice && (
          <Badge variant="green" size="sm">
            Экономия {formatPrice(tour.originalPrice - tour.price)}
          </Badge>
        )}
      </div>

      {/* Date selector */}
      <Select
        label="Дата поездки"
        options={dateOptions}
        value={selectedDateIndex}
        onChange={setSelectedDateIndex}
        placeholder="Выберите дату"
      />

      {/* Travelers */}
      <div>
        <label className="mb-1.5 block text-xs font-medium text-primary">
          Количество путешественников
        </label>
        <div className="flex items-center gap-4 rounded-[12px] border border-sand px-4 py-2.5">
          <motion.button
            whileTap={{ scale: 0.9 }}
            onClick={decrementTravelers}
            disabled={travelers <= 1}
            className="flex h-8 w-8 items-center justify-center rounded-full bg-sand text-dark transition-colors hover:bg-warm-gray/20 disabled:opacity-30"
          >
            <Minus size={16} />
          </motion.button>
          <span className="min-w-[24px] text-center font-heading text-lg font-semibold text-dark">
            {travelers}
          </span>
          <motion.button
            whileTap={{ scale: 0.9 }}
            onClick={incrementTravelers}
            disabled={travelers >= tour.maxTravelers}
            className="flex h-8 w-8 items-center justify-center rounded-full bg-sand text-dark transition-colors hover:bg-warm-gray/20 disabled:opacity-30"
          >
            <Plus size={16} />
          </motion.button>
          <span className="ml-auto text-xs text-warm-gray">
            макс. {tour.maxTravelers}
          </span>
        </div>
      </div>

      {/* Total */}
      <div className="rounded-[12px] bg-sand/50 p-4">
        <div className="flex items-center justify-between text-sm text-dark">
          <span>
            {formatPrice(tour.price)} × {travelers}{' '}
            {travelers === 1 ? 'чел.' : 'чел.'}
          </span>
          <span className="font-heading text-xl font-bold text-primary">
            {formatPrice(totalPrice)}
          </span>
        </div>
      </div>

      {/* Actions */}
      <Button variant="terracotta" size="lg" fullWidth>
        Забронировать
      </Button>

      <Button variant="secondary" size="md" fullWidth leftIcon={<MessageCircle size={18} />}>
        Задать вопрос
      </Button>

      {/* Favorite */}
      <motion.button
        whileTap={{ scale: 0.95 }}
        onClick={() => toggleFavorite(tour.id)}
        className={clsx(
          'flex w-full items-center justify-center gap-2 rounded-[12px] py-2.5 text-sm font-medium transition-colors',
          favorite
            ? 'bg-red-50 text-red-500'
            : 'text-warm-gray hover:bg-sand hover:text-dark',
        )}
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
              className={favorite ? 'fill-red-500 text-red-500' : ''}
            />
          </motion.div>
        </AnimatePresence>
        {favorite ? 'В избранном' : 'Добавить в избранное'}
      </motion.button>
    </div>
  );
}

function SimilarTours({ tour }: { tour: Tour }) {
  const { data } = useQuery({
    queryKey: ['similar-tours', tour.country],
    queryFn: () => getTours({ country: [tour.country] }),
  });

  const similar = useMemo(
    () => (data?.items ?? []).filter((t) => t.id !== tour.id).slice(0, 4),
    [data, tour.id],
  );

  if (similar.length === 0) return null;

  return (
    <FadeInOnScroll>
      <section className="mt-16">
        <h2 className="mb-6 font-heading text-xl font-bold text-dark sm:text-2xl">
          Похожие туры
        </h2>
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {similar.map((t) => (
            <TourCard key={t.id} tour={t} />
          ))}
        </div>
      </section>
    </FadeInOnScroll>
  );
}

export default function TourDetailPage() {
  const { id } = useParams<{ id: string }>();

  const { data: tour, isLoading, error } = useQuery({
    queryKey: ['tour', id],
    queryFn: () => getTourById(id!),
    enabled: Boolean(id),
  });

  if (isLoading) return <DetailSkeleton />;

  if (error || !tour) {
    return (
      <PageTransition>
        <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 px-4 text-center">
          <h1 className="font-heading text-2xl font-bold text-dark">
            Тур не найден
          </h1>
          <p className="text-warm-gray">
            Возможно, тур был удалён или ссылка неверна
          </p>
          <Link to="/tours">
            <Button variant="primary">Вернуться в каталог</Button>
          </Link>
        </div>
      </PageTransition>
    );
  }

  return (
    <PageTransition>
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        {/* Breadcrumbs */}
        <nav className="mb-6 flex items-center gap-2 text-sm text-warm-gray">
          <Link to="/" className="transition-colors hover:text-primary">
            Главная
          </Link>
          <ChevronRight size={14} />
          <Link to="/tours" className="transition-colors hover:text-primary">
            Каталог
          </Link>
          <ChevronRight size={14} />
          <span className="line-clamp-1 text-dark">{tour.title}</span>
        </nav>

        {/* Main Grid */}
        <div className="grid gap-8 lg:grid-cols-[1fr_360px]">
          {/* Left Column */}
          <div className="min-w-0 space-y-8">
            <TourGallery photos={tour.photos} />

            {/* Title & Meta */}
            <div>
              <div className="mb-3 flex flex-wrap items-center gap-3">
                {tour.isHot && (
                  <Badge variant="red" size="sm">
                    🔥 Горящий тур
                  </Badge>
                )}
                <Badge variant="blue" size="sm">
                  {tour.category}
                </Badge>
              </div>

              <h1 className="mb-3 font-heading text-2xl font-bold text-dark sm:text-3xl">
                {tour.title}
              </h1>

              <div className="flex flex-wrap items-center gap-4 text-sm text-warm-gray">
                <span className="flex items-center gap-1.5">
                  <MapPin size={15} className="text-terracotta" />
                  {tour.country}, {tour.city}
                </span>
                <span className="flex items-center gap-1.5">
                  <Hotel size={15} className="text-primary" />
                  {tour.hotel}
                </span>
                <span className="flex items-center gap-1.5">
                  <Clock size={15} className="text-olive" />
                  {tour.duration} дней
                </span>
                <span className="flex items-center gap-1.5">
                  <StarRating rating={tour.rating} size="sm" />
                  <span className="font-medium text-dark">{tour.rating}</span>
                  ({tour.reviewCount} отзывов)
                </span>
              </div>
            </div>

            {/* Tabs */}
            <TourTabs tour={tour} />
          </div>

          {/* Right Sidebar */}
          <div className="hidden lg:block">
            <BookingSidebar tour={tour} />
          </div>
        </div>

        {/* Mobile Booking Bar */}
        <div className="fixed inset-x-0 bottom-0 z-30 border-t border-sand bg-white px-4 py-3 shadow-header lg:hidden">
          <div className="flex items-center justify-between gap-4">
            <div>
              {tour.originalPrice && (
                <span className="text-xs text-warm-gray line-through">
                  {formatPrice(tour.originalPrice)}
                </span>
              )}
              <p className="font-heading text-xl font-bold text-primary">
                {formatPrice(tour.price)}
              </p>
            </div>
            <Button variant="terracotta" size="md">
              Забронировать
            </Button>
          </div>
        </div>

        {/* Similar Tours */}
        <SimilarTours tour={tour} />

        {/* Bottom spacer for mobile booking bar */}
        <div className="h-20 lg:hidden" />
      </div>
    </PageTransition>
  );
}
