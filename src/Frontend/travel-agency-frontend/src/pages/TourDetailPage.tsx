import { useMemo, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronRight, MapPin, Heart, Plane, Clock } from 'lucide-react';
import clsx from 'clsx';
import type { Tour, TourOffer } from '@/types';
import { formatDate, formatFromMoney, formatMoney, formatDateRange } from '@/utils/format';
import { isTourAvailable, tourTypeLabel } from '@/utils/tourLabels';
import { PageTransition, FadeInOnScroll } from '@/components/common';
import { Button, Skeleton, Badge, Modal } from '@/components/ui';
import { TourGallery, TourCard } from '@/components/tour';
import { TourAccommodation, TourInclusions, TourOffers, TourProgram } from '@/components/tour/TourSections';
import { BookingForm } from '@/components/booking';
import { getTourById, getTours } from '@/api/catalog';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';

function DetailSkeleton() {
  return (
    <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <Skeleton className="mb-6 h-4 w-48" />
      <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_360px]">
        <div className="space-y-6">
          <Skeleton variant="rectangular" className="aspect-[16/10] w-full" />
          <Skeleton className="h-8 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
          <Skeleton variant="rectangular" className="h-64 w-full" />
        </div>
        <div className="hidden space-y-4 lg:block">
          <Skeleton variant="rectangular" className="h-80 w-full" />
        </div>
      </div>
    </div>
  );
}

function isNotFound(error: unknown): boolean {
  return error instanceof Error && error.message.includes('не найден');
}

function offerComment(offer: TourOffer | null): string {
  if (!offer) return '';
  return `Хочу поехать ${formatDateRange(offer.start, offer.end)}, ${formatMoney(offer.price, offer.currency)} за человека.`;
}

function RequestButton({ onClick, fullWidth = false }: { onClick: () => void; fullWidth?: boolean }) {
  return (
    <Button variant="terracotta" size="lg" fullWidth={fullWidth} onClick={onClick}>
      Оставить заявку
    </Button>
  );
}

function BookingRail({
  tour,
  offer,
  onRequest,
}: {
  tour: Tour;
  offer: TourOffer | null;
  onRequest: () => void;
}) {
  const { toggleFavorite, isFavorite } = useFavoritesStore();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const openAuthModal = useUIStore((s) => s.openAuthModal);
  const favorite = isFavorite(tour.id);
  const amount = offer?.price ?? tour.priceFrom ?? (tour.price > 0 ? tour.price : null);
  const currency = offer?.currency ?? tour.currency;
  const available = isTourAvailable(tour);

  return (
    <div className="sticky top-24 space-y-5 rounded-[16px] bg-white p-6 shadow-card">
      <div>
        <p className="text-xs uppercase tracking-wide text-warm-gray">
          {offer ? 'Выбранные даты' : 'Цена за человека'}
        </p>
        {amount != null && (
          <p className="mt-1 font-heading text-3xl font-bold text-primary">
            {offer ? formatMoney(amount, currency) : formatFromMoney(amount, currency)}
          </p>
        )}
        {offer && (
          <p className="mt-1 text-sm text-dark/70">{formatDateRange(offer.start, offer.end)}</p>
        )}
      </div>

      {available && <RequestButton fullWidth onClick={onRequest} />}

      <motion.button
        type="button"
        whileTap={{ scale: 0.95 }}
        onClick={() => {
          if (!isAuthenticated) {
            openAuthModal('login');
            return;
          }
          toggleFavorite(tour.id, tour);
        }}
        className={clsx(
          'flex w-full items-center justify-center gap-2 rounded-[12px] py-2.5 text-sm font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary',
          favorite ? 'bg-red-50 text-red-500' : 'text-warm-gray hover:bg-sand hover:text-dark',
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
            <Heart size={18} className={favorite ? 'fill-red-500 text-red-500' : ''} />
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
    enabled: Boolean(tour.country),
  });

  const similar = useMemo(
    () => (data?.items ?? []).filter((item) => item.id !== tour.id).slice(0, 4),
    [data, tour.id],
  );

  if (similar.length === 0) return null;

  return (
    <FadeInOnScroll>
      <section className="mt-16">
        <h2 className="mb-6 font-heading text-xl font-bold text-dark sm:text-2xl">Похожие туры</h2>
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {similar.map((item) => (
            <TourCard key={item.id} tour={item} />
          ))}
        </div>
      </section>
    </FadeInOnScroll>
  );
}

export default function TourDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [showBookingModal, setShowBookingModal] = useState(false);
  const [offerId, setOfferId] = useState<string | null>(null);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const openAuthModal = useUIStore((s) => s.openAuthModal);

  const { data: tour, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['tour', id],
    queryFn: () => getTourById(id!),
    enabled: Boolean(id),
  });

  const selectedOffer =
    tour?.offers?.find((offer) => offer.id === offerId) ?? tour?.offers?.[0] ?? null;
  const available = tour ? isTourAvailable(tour) : false;
  const typeLabel = tour ? tourTypeLabel(tour.category) : '';
  const priceFrom = tour?.priceFrom ?? (tour && tour.price > 0 ? tour.price : null);

  function openRequest() {
    if (!isAuthenticated) {
      openAuthModal('login');
      return;
    }
    setShowBookingModal(true);
  }

  if (isLoading) return <DetailSkeleton />;

  if (isError && isNotFound(error)) {
    return (
      <PageTransition>
        <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 px-4 text-center">
          <h1 className="font-heading text-2xl font-bold text-dark">Тур не найден</h1>
          <p className="text-warm-gray">Черновик и снятый тур по прямой ссылке не открываются</p>
          <Link to="/tours">
            <Button variant="primary">Вернуться в каталог</Button>
          </Link>
        </div>
      </PageTransition>
    );
  }

  if (isError || !tour) {
    return (
      <PageTransition>
        <div role="alert" className="flex min-h-[60vh] flex-col items-center justify-center gap-4 px-4 text-center">
          <h1 className="font-heading text-2xl font-bold text-dark">Не удалось загрузить тур</h1>
          <Button variant="secondary" onClick={() => refetch()}>
            Повторить
          </Button>
        </div>
      </PageTransition>
    );
  }

  return (
    <PageTransition>
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <nav className="mb-6 flex flex-wrap items-center gap-2 text-sm text-warm-gray">
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

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_360px]">
          <div className="min-w-0 space-y-8">
            <TourGallery images={tour.images} photos={tour.photos} title={tour.title} />

            <div>
              {typeLabel && (
                <div className="mb-3">
                  <Badge variant="blue" size="sm">
                    {typeLabel}
                  </Badge>
                </div>
              )}

              <h1 className="mb-3 text-balance font-heading text-2xl font-bold text-dark sm:text-3xl">
                {tour.title}
              </h1>

              {tour.shortDescription && (
                <p className="mb-4 max-w-3xl text-pretty text-base leading-relaxed text-dark/80">
                  {tour.shortDescription}
                </p>
              )}

              <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-warm-gray">
                {tour.country && (
                  <span className="flex items-center gap-1.5">
                    <MapPin size={15} className="text-terracotta" />
                    {tour.country}
                  </span>
                )}
                {tour.departureCity && (
                  <span className="flex items-center gap-1.5">
                    <Plane size={15} className="text-primary" />
                    Вылет из {tour.departureCity}
                  </span>
                )}
                {tour.duration > 0 && (
                  <span className="flex items-center gap-1.5">
                    <Clock size={15} className="text-olive" />
                    {tour.duration} дней
                  </span>
                )}
                {tour.nearestDate && (
                  <span>ближайший выезд {formatDate(tour.nearestDate, 'd MMM')}</span>
                )}
              </div>
            </div>

            {tour.description && (
              <section aria-labelledby="tour-about">
                <h2 id="tour-about" className="mb-3 font-heading text-xl font-bold text-dark">
                  Описание
                </h2>
                <p className="whitespace-pre-line text-pretty leading-relaxed text-dark/80">{tour.description}</p>
              </section>
            )}

            <TourProgram tour={tour} />
            <TourInclusions tour={tour} />
            <TourAccommodation tour={tour} />
            <TourOffers
              tour={tour}
              selectedId={selectedOffer?.id ?? null}
              onSelect={(offer) => setOfferId(offer.id)}
            />
          </div>

          <div className="hidden lg:block">
            <BookingRail tour={tour} offer={selectedOffer} onRequest={openRequest} />
          </div>
        </div>

        {available && (
          <div className="fixed inset-x-0 bottom-0 z-30 border-t border-sand bg-white px-4 py-3 shadow-header lg:hidden">
            <div className="flex items-center justify-between gap-4">
              <div className="min-w-0">
                {priceFrom != null && (
                  <p className="font-heading text-xl font-bold text-primary">
                    {selectedOffer
                      ? formatMoney(selectedOffer.price, selectedOffer.currency)
                      : formatFromMoney(priceFrom, tour.currency)}
                  </p>
                )}
              </div>
              <RequestButton onClick={openRequest} />
            </div>
          </div>
        )}

        <SimilarTours tour={tour} />
        <div className="h-20 lg:hidden" />
      </div>

      <Modal
        isOpen={showBookingModal}
        onClose={() => setShowBookingModal(false)}
        title="Оформить заявку"
        size="lg"
      >
        <BookingForm
          key={selectedOffer?.id ?? tour.id}
          tourId={tour.id}
          tours={[tour]}
          initialComment={offerComment(selectedOffer)}
          onSuccess={() => setShowBookingModal(false)}
          onClose={() => setShowBookingModal(false)}
        />
      </Modal>
    </PageTransition>
  );
}
