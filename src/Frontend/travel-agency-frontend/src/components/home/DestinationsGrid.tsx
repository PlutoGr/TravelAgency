import { Link } from 'react-router-dom';
import { MapPin } from 'lucide-react';
import clsx from 'clsx';

import { FadeInOnScroll } from '@/components/common';
import { mockDestinations } from '@/mocks/destinations';

export default function DestinationsGrid() {
  return (
    <section id="destinations" className="bg-sand py-16 md:py-24">
      <div className="mx-auto max-w-7xl px-4">
        <FadeInOnScroll>
          <div className="mb-14 text-center">
            <h2 className="font-heading text-3xl font-bold text-dark md:text-4xl">
              Популярные направления
            </h2>
            <div className="mx-auto mt-4 h-1 w-16 rounded-full bg-terracotta" />
          </div>
        </FadeInOnScroll>

        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {mockDestinations.map((dest, idx) => {
            const isLarge = idx < 2;

            return (
              <FadeInOnScroll
                key={dest.id}
                delay={idx * 0.08}
                className={clsx(isLarge && 'sm:col-span-2 lg:col-span-2')}
              >
                <Link
                  to="/tours"
                  className={clsx(
                    'group relative block overflow-hidden rounded-2xl',
                    isLarge ? 'aspect-[16/9]' : 'aspect-[4/3]',
                  )}
                >
                  <img
                    src={dest.photo}
                    alt={dest.name}
                    className="h-full w-full object-cover transition-transform duration-500 group-hover:scale-110"
                    loading="lazy"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-black/20 to-transparent" />

                  <div className="absolute inset-x-0 bottom-0 p-5">
                    <div className="flex items-center gap-1.5 text-white/70">
                      <MapPin size={14} />
                      <span className="text-sm">{dest.country}</span>
                    </div>
                    <h3
                      className={clsx(
                        'mt-1 font-heading font-bold text-white',
                        isLarge ? 'text-2xl' : 'text-lg',
                      )}
                    >
                      {dest.name}
                    </h3>
                    <span className="mt-2 inline-block rounded-full bg-white/20 px-3 py-1 text-xs font-medium text-white backdrop-blur-sm">
                      {dest.tourCount} туров
                    </span>
                  </div>
                </Link>
              </FadeInOnScroll>
            );
          })}
        </div>
      </div>
    </section>
  );
}
