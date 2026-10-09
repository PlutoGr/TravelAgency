import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Navigation, Pagination, Autoplay } from 'swiper/modules';
import { Heart, MapPin, Clock, ArrowRight } from 'lucide-react';
import { motion } from 'framer-motion';
import clsx from 'clsx';
import 'swiper/swiper-bundle.css';

import { StarRating, Skeleton } from '@/components/ui';
import { FadeInOnScroll } from '@/components/common';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { getTours } from '@/api/catalog';
import { formatPrice } from '@/utils/format';
import { coverMediaFileId, mediaImageUrl } from '@/utils/media';

const HOT_DEALS_PAGE_SIZE = 50;

export default function HotDealsCarousel() {
  const toggleFavorite = useFavoritesStore((s) => s.toggleFavorite);
  const isFavorite = useFavoritesStore((s) => s.isFavorite);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const openAuthModal = useUIStore((s) => s.openAuthModal);

  const { data: toursData, isLoading } = useQuery({
    queryKey: ['catalog', 'tours', 'hot-deals'],
    queryFn: () => getTours(undefined, 1, HOT_DEALS_PAGE_SIZE),
  });

  const hotTours = useMemo(
    () => (toursData?.items ?? []).filter((t) => t.isHot),
    [toursData?.items],
  );

  return (
    <section id="hot-deals" className="py-16 md:py-24">
      <div className="mx-auto max-w-7xl px-4">
        <FadeInOnScroll>
          <div className="mb-12 flex items-center justify-between">
            <div>
              <h2 className="font-heading text-3xl font-bold text-dark md:text-4xl">
                Горящие предложения 🔥
              </h2>
              <p className="mt-2 text-warm-gray">Успейте забронировать по лучшей цене</p>
            </div>
            <Link
              to="/tours"
              className="hidden items-center gap-1 font-heading text-sm font-semibold text-primary transition-colors hover:text-primary-light md:flex"
            >
              Все туры
              <ArrowRight size={16} />
            </Link>
          </div>
        </FadeInOnScroll>

        <FadeInOnScroll>
          {isLoading ? (
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="overflow-hidden rounded-2xl bg-white shadow-card">
                  <Skeleton className="aspect-[4/3] w-full" />
                  <div className="p-5">
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="mt-2 h-5 w-full" />
                    <Skeleton className="mt-3 h-4 w-20" />
                    <Skeleton className="mt-4 h-8 w-24" />
                  </div>
                </div>
              ))}
            </div>
          ) : hotTours.length === 0 ? (
            <div className="rounded-2xl bg-sand/40 py-16 text-center text-warm-gray">
              <p className="font-medium">Нет горящих предложений</p>
              <p className="mt-1 text-sm">Следите за обновлениями каталога</p>
              <Link
                to="/tours"
                className="mt-4 inline-flex items-center gap-1 font-heading text-sm font-semibold text-primary"
              >
                Все туры
                <ArrowRight size={16} />
              </Link>
            </div>
          ) : (
          <Swiper
            modules={[Navigation, Pagination, Autoplay]}
            spaceBetween={24}
            loop={hotTours.length > 3}
            autoplay={{ delay: 5000, disableOnInteraction: false }}
            navigation
            pagination={{ clickable: true }}
            breakpoints={{
              0: { slidesPerView: 1 },
              640: { slidesPerView: 2 },
              1024: { slidesPerView: 3 },
              1280: { slidesPerView: 4 },
            }}
            className="hot-deals-swiper !pb-12"
          >
            {hotTours.map((tour) => (
              <SwiperSlide key={tour.id}>
                <motion.div
                  whileHover={{ y: -6 }}
                  transition={{ type: 'spring', stiffness: 300, damping: 25 }}
                  className="group overflow-hidden rounded-2xl bg-white shadow-card"
                >
                  <div className="relative aspect-[4/3] overflow-hidden">
                    {coverMediaFileId(tour) ? (
                    <img
                      src={mediaImageUrl(coverMediaFileId(tour)!, 'w800')}
                      alt={tour.title}
                      className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-110"
                      loading="lazy"
                    />
                    ) : (
                    <div className="h-full w-full bg-sand" aria-hidden />
                    )}
                    <div className="absolute inset-0 bg-gradient-to-t from-black/40 to-transparent" />

                    <span className="absolute left-3 top-3 rounded-full bg-terracotta px-3 py-1 text-xs font-bold text-white shadow-lg">
                      Горящий тур
                    </span>

                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        if (!isAuthenticated) {
                          openAuthModal('login');
                          return;
                        }
                        toggleFavorite(tour.id, tour);
                      }}
                      className="absolute right-3 top-3 flex h-9 w-9 items-center justify-center rounded-full bg-white/80 backdrop-blur-sm transition-colors hover:bg-white"
                      aria-label="Добавить в избранное"
                    >
                      <Heart
                        size={18}
                        className={clsx(
                          'transition-colors',
                          isFavorite(tour.id)
                            ? 'fill-red-500 text-red-500'
                            : 'text-dark/60',
                        )}
                      />
                    </button>
                  </div>

                  <div className="p-5">
                    <div className="flex items-center gap-1.5 text-sm text-warm-gray">
                      <MapPin size={14} className="text-terracotta" />
                      <span>
                        {tour.country}, {tour.city}
                      </span>
                    </div>

                    <h3 className="mt-2 line-clamp-2 font-heading text-base font-bold text-dark">
                      {tour.title}
                    </h3>

                    <div className="mt-3 flex items-center gap-3">
                      <div className="flex items-center gap-1.5">
                        <StarRating rating={tour.rating} size="sm" />
                        <span className="text-sm font-medium text-dark">{tour.rating}</span>
                      </div>
                      <div className="flex items-center gap-1 text-sm text-warm-gray">
                        <Clock size={14} />
                        <span>{tour.duration} дней</span>
                      </div>
                    </div>

                    <div className="mt-4 flex items-end justify-between">
                      <div>
                        {tour.originalPrice && (
                          <span className="block text-sm text-warm-gray line-through">
                            {formatPrice(tour.originalPrice)}
                          </span>
                        )}
                        <span className="font-heading text-xl font-bold text-terracotta">
                          {formatPrice(tour.price)}
                        </span>
                      </div>
                      <Link
                        to={`/tours/${tour.id}`}
                        className="rounded-xl bg-primary px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-primary-light"
                      >
                        Подробнее
                      </Link>
                    </div>
                  </div>
                </motion.div>
              </SwiperSlide>
            ))}
          </Swiper>
          )}
        </FadeInOnScroll>

        <div className="mt-6 text-center md:hidden">
          <Link
            to="/tours"
            className="inline-flex items-center gap-1 font-heading text-sm font-semibold text-primary"
          >
            Все туры
            <ArrowRight size={16} />
          </Link>
        </div>
      </div>
    </section>
  );
}
