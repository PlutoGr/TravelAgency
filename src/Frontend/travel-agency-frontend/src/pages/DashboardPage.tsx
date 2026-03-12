import { useState, useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { PlusCircle, Briefcase, Heart } from 'lucide-react';
import type { Booking } from '@/types';
import { PageTransition } from '@/components/common';
import { Card, Modal } from '@/components/ui';
import { BookingCard, BookingForm } from '@/components/booking';
import TourCard from '@/components/tour/TourCard';
import { useAuthStore } from '@/store/authStore';
import { useFavoritesStore } from '@/store/favoritesStore';
import { getMyBookings } from '@/api/bookings';
import { mockTours } from '@/mocks/tours';
import { mockUser } from '@/mocks/users';

const ACTION_TILES = [
  {
    id: 'new-booking',
    icon: PlusCircle,
    title: 'Новая заявка',
    description: 'Создайте заявку на подбор тура',
  },
  {
    id: 'my-bookings',
    icon: Briefcase,
    title: 'Мои бронирования',
    description: 'Отслеживайте статус ваших заявок',
    to: '/dashboard/bookings',
  },
] as const;

function getRandomTours(count: number) {
  const shuffled = [...mockTours].sort(() => 0.5 - Math.random());
  return shuffled.slice(0, count);
}

export default function DashboardPage() {
  const storeUser = useAuthStore((s) => s.user);
  const user = storeUser ?? mockUser;
  const favCount = useFavoritesStore((s) => s.count);

  const [bookings, setBookings] = useState<Booking[]>([]);
  const [showForm, setShowForm] = useState(false);

  const suggestedTours = useMemo(() => getRandomTours(4), []);

  useEffect(() => {
    getMyBookings().then((all) => {
      const active = all.filter((b) => b.status !== 'closed').slice(0, 3);
      setBookings(active);
    });
  }, []);

  function handleBookingCreated() {
    setShowForm(false);
    getMyBookings().then((all) => {
      setBookings(all.filter((b) => b.status !== 'closed').slice(0, 3));
    });
  }

  return (
    <PageTransition>
      <div className="space-y-8">
        {/* Welcome */}
        <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">
          Добро пожаловать, {user.firstName}!
        </h1>

        {/* Action Tiles */}
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {ACTION_TILES.map((tile) => {
            const Icon = tile.icon;
            const content = (
              <Card hover className="flex items-start gap-4 p-5">
                <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-[12px] bg-primary/10 text-primary">
                  <Icon size={24} />
                </div>
                <div>
                  <h3 className="font-heading text-base font-semibold text-dark">
                    {tile.title}
                  </h3>
                  <p className="mt-0.5 text-sm text-warm-gray">
                    {tile.description}
                  </p>
                </div>
              </Card>
            );

            if (tile.id === 'new-booking') {
              return (
                <div key={tile.id} onClick={() => setShowForm(true)}>
                  {content}
                </div>
              );
            }

            return (
              <Link key={tile.id} to={tile.to!}>
                {content}
              </Link>
            );
          })}

          {/* Favorites tile — dynamic count */}
          <Link to="/dashboard/favorites">
            <Card hover className="flex items-start gap-4 p-5">
              <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-[12px] bg-primary/10 text-primary">
                <Heart size={24} />
              </div>
              <div>
                <h3 className="font-heading text-base font-semibold text-dark">
                  Избранное
                </h3>
                <p className="mt-0.5 text-sm text-warm-gray">
                  Ваши сохранённые туры ({favCount})
                </p>
              </div>
            </Card>
          </Link>
        </div>

        {/* Active Bookings */}
        {bookings.length > 0 && (
          <section>
            <div className="mb-4 flex items-center justify-between">
              <h2 className="font-heading text-lg font-semibold text-dark">
                Активные заявки
              </h2>
              <Link
                to="/dashboard/bookings"
                className="text-sm font-medium text-primary transition-colors hover:text-primary-light"
              >
                Все заявки
              </Link>
            </div>
            <div className="space-y-3">
              {bookings.map((b) => (
                <BookingCard key={b.id} booking={b} />
              ))}
            </div>
          </section>
        )}

        {/* Suggested Tours */}
        <section>
          <h2 className="mb-4 font-heading text-lg font-semibold text-dark">
            Вам может понравиться
          </h2>
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {suggestedTours.map((tour) => (
              <TourCard key={tour.id} tour={tour} />
            ))}
          </div>
        </section>
      </div>

      {/* Booking Form Modal */}
      <Modal
        isOpen={showForm}
        onClose={() => setShowForm(false)}
        title="Новая заявка"
        size="lg"
      >
        <BookingForm
          onSuccess={handleBookingCreated}
          onClose={() => setShowForm(false)}
        />
      </Modal>
    </PageTransition>
  );
}
