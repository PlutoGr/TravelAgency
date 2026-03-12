import { Swiper, SwiperSlide } from 'swiper/react';
import { Autoplay, Pagination } from 'swiper/modules';
import { Quote } from 'lucide-react';
import 'swiper/swiper-bundle.css';

import { StarRating } from '@/components/ui';
import { FadeInOnScroll } from '@/components/common';
import { mockReviews } from '@/mocks/reviews';

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('ru-RU', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}

export default function ReviewsSlider() {
  return (
    <section id="reviews" className="bg-cream py-16 md:py-24">
      <div className="mx-auto max-w-7xl px-4">
        <FadeInOnScroll>
          <div className="mb-14 text-center">
            <Quote size={40} className="mx-auto mb-4 text-terracotta/30" />
            <h2 className="font-heading text-3xl font-bold text-dark md:text-4xl">
              Отзывы наших клиентов
            </h2>
            <div className="mx-auto mt-4 h-1 w-16 rounded-full bg-terracotta" />
          </div>
        </FadeInOnScroll>

        <FadeInOnScroll>
          <Swiper
            modules={[Autoplay, Pagination]}
            spaceBetween={24}
            autoplay={{ delay: 6000, disableOnInteraction: false }}
            pagination={{ clickable: true }}
            breakpoints={{
              0: { slidesPerView: 1 },
              768: { slidesPerView: 2 },
              1024: { slidesPerView: 3 },
            }}
            className="reviews-swiper !pb-12"
          >
            {mockReviews.map((review) => (
              <SwiperSlide key={review.id}>
                <div className="flex h-full flex-col rounded-2xl bg-white p-6 shadow-card">
                  <div className="flex items-center gap-4">
                    <img
                      src={review.userAvatar}
                      alt={review.userName}
                      className="h-12 w-12 rounded-full object-cover"
                      loading="lazy"
                    />
                    <div>
                      <p className="font-heading text-sm font-bold text-dark">
                        {review.userName}
                      </p>
                      <p className="text-xs text-warm-gray">{formatDate(review.date)}</p>
                    </div>
                  </div>

                  <div className="mt-4">
                    <StarRating rating={review.rating} size="sm" />
                  </div>

                  <p className="mt-3 flex-1 text-sm leading-relaxed text-dark/80 line-clamp-3">
                    {review.text}
                  </p>

                  <p className="mt-4 border-t border-sand pt-3 text-xs text-warm-gray">
                    Тур: <span className="font-medium text-primary">{review.tourTitle}</span>
                  </p>
                </div>
              </SwiperSlide>
            ))}
          </Swiper>
        </FadeInOnScroll>
      </div>
    </section>
  );
}
