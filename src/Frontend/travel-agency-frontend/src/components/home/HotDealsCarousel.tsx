import { Link } from 'react-router-dom';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Navigation, Pagination, Autoplay } from 'swiper/modules';
import { Heart, MapPin, Clock, ArrowRight } from 'lucide-react';
import { motion } from 'framer-motion';
import clsx from 'clsx';
import 'swiper/swiper-bundle.css';

import { StarRating } from '@/components/ui';
import { FadeInOnScroll } from '@/components/common';
import { useFavoritesStore } from '@/store/favoritesStore';
import { mockTours } from '@/mocks/tours';

const hotTours = mockTours.filter((t) => t.isHot);

function formatPrice(price: number) {
  return price.toLocaleString('ru-RU');
}

export default function HotDealsCarousel() {
  const toggleFavorite = useFavoritesStore((s) => s.toggleFavorite);
  const isFavorite = useFavoritesStore((s) => s.isFavorite);

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
                    <img
                      src={tour.photos[0]}
                      alt={tour.title}
                      className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-110"
                      loading="lazy"
                    />
                    <div className="absolute inset-0 bg-gradient-to-t from-black/40 to-transparent" />

                    <span className="absolute left-3 top-3 rounded-full bg-terracotta px-3 py-1 text-xs font-bold text-white shadow-lg">
                      Горящий тур
                    </span>

                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        toggleFavorite(tour.id);
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
                            {formatPrice(tour.originalPrice)} ₽
                          </span>
                        )}
                        <span className="font-heading text-xl font-bold text-terracotta">
                          {formatPrice(tour.price)} ₽
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
