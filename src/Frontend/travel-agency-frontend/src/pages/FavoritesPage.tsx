import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Heart } from 'lucide-react';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Button, Skeleton } from '@/components/ui';
import TourCard from '@/components/tour/TourCard';
import { useFavoritesStore } from '@/store/favoritesStore';

const BREADCRUMBS = [
  { label: 'Личный кабинет', path: '/dashboard' },
  { label: 'Избранное' },
];

export default function FavoritesPage() {
  const { favoriteTours, loadFavorites, isLoading, error } = useFavoritesStore();

  useEffect(() => {
    loadFavorites();
  }, [loadFavorites]);

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <h1 className="font-heading text-2xl font-bold text-dark">
          Избранное
        </h1>

        {error ? (
          <div role="alert" className="rounded-[16px] border border-terracotta/30 bg-terracotta/5 px-4 py-6 text-center">
            <p className="text-dark">{error}</p>
            <Button className="mt-4" variant="secondary" onClick={() => loadFavorites()}>
              Повторить
            </Button>
          </div>
        ) : isLoading ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex flex-col">
                <Skeleton variant="rectangular" className="aspect-[4/3] w-full !rounded-t-2xl" />
                <div className="space-y-3 p-4">
                  <Skeleton className="h-3 w-1/2" />
                  <Skeleton className="h-5 w-full" />
                  <Skeleton className="h-4 w-1/3" />
                </div>
              </div>
            ))}
          </div>
        ) : favoriteTours.length === 0 ? (
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
            {favoriteTours.map((tour, index) => (
              <TourCard key={tour.id} tour={tour} priority={index === 0} />
            ))}
          </div>
        )}
      </div>
    </PageTransition>
  );
}
