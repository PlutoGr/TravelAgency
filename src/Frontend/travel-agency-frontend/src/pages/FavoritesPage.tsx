import { Link } from 'react-router-dom';
import { Heart } from 'lucide-react';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Button } from '@/components/ui';
import TourCard from '@/components/tour/TourCard';
import { useFavoritesStore } from '@/store/favoritesStore';
import { mockTours } from '@/mocks/tours';

const BREADCRUMBS = [
  { label: 'Личный кабинет', path: '/dashboard' },
  { label: 'Избранное' },
];

export default function FavoritesPage() {
  const favoriteIds = useFavoritesStore((s) => s.favoriteIds);
  const favoriteTours = mockTours.filter((t) => favoriteIds.includes(t.id));

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <h1 className="font-heading text-2xl font-bold text-dark">
          Избранное
        </h1>

        {favoriteTours.length === 0 ? (
          <div className="flex min-h-[40vh] flex-col items-center justify-center gap-4 text-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-sand">
              <Heart size={28} className="text-warm-gray" />
            </div>
            <p className="text-lg text-warm-gray">
              Вы пока не добавили туры в избранное
            </p>
            <Link to="/tours">
              <Button>Перейти в каталог</Button>
            </Link>
          </div>
        ) : (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {favoriteTours.map((tour) => (
              <TourCard key={tour.id} tour={tour} />
            ))}
          </div>
        )}
      </div>
    </PageTransition>
  );
}
