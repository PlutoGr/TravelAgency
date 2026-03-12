import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus } from 'lucide-react';
import type { Booking, BookingStatus } from '@/types';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { Tabs, Button, Skeleton, Modal } from '@/components/ui';
import { BookingCard, BookingForm } from '@/components/booking';
import { getMyBookings } from '@/api/bookings';

const STATUS_TABS = [
  { id: 'all', label: 'Все' },
  { id: 'new', label: 'Новые' },
  { id: 'in_progress', label: 'В работе' },
  { id: 'confirmed', label: 'Подтверждённые' },
  { id: 'closed', label: 'Закрытые' },
];

const BREADCRUMBS = [
  { label: 'Личный кабинет', path: '/dashboard' },
  { label: 'Мои бронирования' },
];

export default function MyBookingsPage() {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('all');
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);

  function loadBookings() {
    setIsLoading(true);
    const statusFilter = activeTab === 'all' ? undefined : (activeTab as BookingStatus);
    getMyBookings(statusFilter)
      .then(setBookings)
      .finally(() => setIsLoading(false));
  }

  useEffect(() => {
    loadBookings();
  }, [activeTab]);

  function handleBookingCreated() {
    setShowForm(false);
    loadBookings();
  }

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="font-heading text-2xl font-bold text-dark">
            Мои бронирования
          </h1>
          <Button
            leftIcon={<Plus size={18} />}
            size="sm"
            onClick={() => setShowForm(true)}
          >
            Новая заявка
          </Button>
        </div>

        <Tabs tabs={STATUS_TABS} activeTab={activeTab} onChange={setActiveTab} />

        {isLoading ? (
          <div className="space-y-4">
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} variant="rectangular" height={120} className="w-full" />
            ))}
          </div>
        ) : bookings.length === 0 ? (
          <div className="flex min-h-[40vh] flex-col items-center justify-center gap-4 text-center">
            <p className="text-lg text-warm-gray">
              У вас пока нет бронирований
            </p>
            <Button onClick={() => setShowForm(true)}>
              Создать заявку
            </Button>
          </div>
        ) : (
          <div className="space-y-3">
            {bookings.map((b) => (
              <BookingCard key={b.id} booking={b} />
            ))}
          </div>
        )}
      </div>

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
