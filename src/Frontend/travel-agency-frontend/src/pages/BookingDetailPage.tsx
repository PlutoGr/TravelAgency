import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  MapPin,
  Calendar,
  Users,
  Wallet,
  UserCircle,
  StickyNote,
  Clock,
  ArrowLeft,
  CheckCircle,
  CreditCard,
  ExternalLink,
} from 'lucide-react';
import { format, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';
import clsx from 'clsx';
import type { Booking, BookingStatus } from '@/types';
import { getBookingById, updateBookingStatus } from '@/api/bookings';
import { PageTransition } from '@/components/common';
import { Breadcrumbs } from '@/components/layout';
import { BookingStatusBadge, ChatWindow } from '@/components/booking';
import { Card, Button, Skeleton, StarRating } from '@/components/ui';
import { useAuthStore } from '@/store/authStore';

function formatDate(dateStr: string): string {
  return format(parseISO(dateStr), 'd MMMM yyyy', { locale: ru });
}

function formatBudget(amount: number): string {
  return `от ${amount.toLocaleString('ru-RU')} ₽`;
}

function InfoRow({
  icon: Icon,
  label,
  children,
}: {
  icon: React.ComponentType<{ size?: number; className?: string }>;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex items-start gap-3 py-3">
      <Icon size={20} className="mt-0.5 shrink-0 text-primary" />
      <div className="min-w-0 flex-1">
        <p className="text-xs font-medium text-warm-gray">{label}</p>
        <p className="mt-0.5 text-sm font-medium text-dark">{children}</p>
      </div>
    </div>
  );
}

function BookingInfoSkeleton() {
  return (
    <div className="space-y-6">
      <Skeleton width="120px" height={28} variant="rectangular" />
      <div className="space-y-4 rounded-2xl bg-white p-6 shadow-card">
        {Array.from({ length: 6 }, (_, i) => (
          <div key={i} className="flex items-center gap-3">
            <Skeleton width={20} height={20} variant="circular" />
            <div className="flex-1 space-y-1.5">
              <Skeleton width="30%" height={12} />
              <Skeleton width="60%" height={16} />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function TourPreview({ booking }: { booking: Booking }) {
  const tour = booking.tour;
  if (!tour) return null;

  const showPayButton =
    booking.status === 'proposal_sent' || booking.status === 'confirmed';

  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: 0.2 }}
    >
      <h3 className="mb-3 font-heading text-lg font-semibold text-dark">
        Предложенный тур
      </h3>
      <Card className="overflow-hidden">
        {tour.photos[0] && (
          <div className="relative h-48 w-full overflow-hidden">
            <img
              src={tour.photos[0]}
              alt={tour.title}
              className="h-full w-full object-cover"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black/40 to-transparent" />
            <div className="absolute bottom-3 left-4 right-4">
              <p className="text-lg font-heading font-bold text-white drop-shadow">
                {tour.price.toLocaleString('ru-RU')} ₽
              </p>
            </div>
          </div>
        )}

        <div className="p-4 space-y-3">
          <div>
            <h4 className="font-heading text-base font-semibold text-dark">
              {tour.title}
            </h4>
            <p className="mt-1 text-sm text-warm-gray">
              {tour.city}, {tour.country} · {tour.duration} дн.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <StarRating rating={tour.rating} size="sm" />
            <span className="text-sm font-medium text-dark">
              {tour.rating}
            </span>
            <span className="text-xs text-warm-gray">
              ({tour.reviewCount} отзывов)
            </span>
          </div>

          <p className="text-sm text-dark/70 line-clamp-2">
            {tour.shortDescription}
          </p>

          <div className="flex flex-col gap-2 pt-1 sm:flex-row">
            <Link to={`/tours/${tour.id}`}>
              <Button variant="secondary" size="sm" leftIcon={<ExternalLink size={16} />}>
                Подробнее
              </Button>
            </Link>
            {showPayButton && (
              <Button variant="primary" size="sm" leftIcon={<CreditCard size={16} />}>
                Оплатить
              </Button>
            )}
          </div>
        </div>
      </Card>
    </motion.div>
  );
}

export default function BookingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const user = useAuthStore((s) => s.user);
  const [booking, setBooking] = useState<Booking | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUpdating, setIsUpdating] = useState(false);

  const fetchBooking = useCallback(async () => {
    if (!id) return;
    setIsLoading(true);
    try {
      const data = await getBookingById(id);
      setBooking(data);
    } catch {
      setBooking(null);
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => { fetchBooking(); }, [fetchBooking]);

  const handleStatusUpdate = async (status: BookingStatus) => {
    if (!id || isUpdating) return;
    setIsUpdating(true);
    try {
      const updated = await updateBookingStatus(id, status);
      setBooking(updated);
    } finally {
      setIsUpdating(false);
    }
  };

  const breadcrumbs = [
    { label: 'Личный кабинет', path: '/dashboard' },
    { label: 'Мои бронирования', path: '/dashboard/bookings' },
    { label: `Заявка #${id ?? ''}` },
  ];

  return (
    <PageTransition>
      <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        <Breadcrumbs items={breadcrumbs} />

        <h1 className="mt-4 font-heading text-2xl font-bold text-dark sm:text-3xl">
          Заявка #{id}
        </h1>

        {isLoading ? (
          <div className="mt-6 grid gap-6 lg:grid-cols-5">
            <div className="lg:col-span-2">
              <BookingInfoSkeleton />
            </div>
            <div className="lg:col-span-3">
              <Skeleton width="100%" height={500} variant="rectangular" />
            </div>
          </div>
        ) : !booking ? (
          <div className="mt-12 flex flex-col items-center justify-center py-16 text-center">
            <p className="font-heading text-xl font-semibold text-dark">
              Заявка не найдена
            </p>
            <p className="mt-2 text-sm text-warm-gray">
              Возможно, она была удалена или у вас нет доступа
            </p>
            <Link to="/dashboard/bookings" className="mt-6">
              <Button variant="secondary" leftIcon={<ArrowLeft size={18} />}>
                Вернуться к списку
              </Button>
            </Link>
          </div>
        ) : (
          <div className="mt-6 grid gap-6 lg:grid-cols-5">
            {/* Left column — booking info */}
            <div className="space-y-6 lg:col-span-2">
              <motion.div
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
              >
                <BookingStatusBadge status={booking.status} />
              </motion.div>

              <motion.div
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.1 }}
              >
                <Card className="divide-y divide-sand p-5">
                  <InfoRow icon={MapPin} label="Направление">
                    {booking.destination}, {booking.country}
                  </InfoRow>
                  <InfoRow icon={Calendar} label="Даты">
                    {formatDate(booking.dateFrom)} — {formatDate(booking.dateTo)}
                  </InfoRow>
                  <InfoRow icon={Users} label="Путешественники">
                    {booking.travelers}
                  </InfoRow>
                  <InfoRow icon={Wallet} label="Бюджет">
                    {formatBudget(booking.budget)}
                  </InfoRow>
                  <InfoRow icon={UserCircle} label="Менеджер">
                    {booking.managerName ?? 'Не назначен'}
                  </InfoRow>
                  {booking.notes && (
                    <InfoRow icon={StickyNote} label="Заметки">
                      {booking.notes}
                    </InfoRow>
                  )}
                  <InfoRow icon={Clock} label="Дата создания">
                    {formatDate(booking.createdAt)}
                  </InfoRow>
                </Card>
              </motion.div>

              {booking.tour && <TourPreview booking={booking} />}

              <motion.div
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: 0.3 }}
                className="flex flex-col gap-3 sm:flex-row"
              >
                {booking.status === 'confirmed' && (
                  <Button
                    variant="primary"
                    leftIcon={<CheckCircle size={18} />}
                    isLoading={isUpdating}
                    onClick={() => handleStatusUpdate('closed')}
                    className={clsx('!bg-olive hover:!bg-olive-light')}
                  >
                    Завершить
                  </Button>
                )}
                <Link to="/dashboard/bookings">
                  <Button
                    variant="ghost"
                    leftIcon={<ArrowLeft size={18} />}
                  >
                    Вернуться к списку
                  </Button>
                </Link>
              </motion.div>
            </div>

            {/* Right column — chat */}
            <motion.div
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.15 }}
              className="lg:col-span-3"
            >
              <Card className="flex h-[600px] flex-col overflow-hidden lg:h-[calc(100vh-220px)] lg:min-h-[500px] lg:max-h-[800px]">
                <div className="border-b border-sand px-5 py-3">
                  <h2 className="font-heading text-base font-semibold text-dark">
                    Чат с менеджером
                  </h2>
                  {booking.managerName && (
                    <p className="text-xs text-warm-gray">
                      {booking.managerName}
                    </p>
                  )}
                </div>
                <ChatWindow
                  bookingId={booking.id}
                  currentUserId={user?.id ?? ''}
                  currentUserName={
                    user ? `${user.firstName} ${user.lastName}` : ''
                  }
                  currentUserRole={
                    (user?.role as 'client' | 'manager') ?? 'client'
                  }
                />
              </Card>
            </motion.div>
          </div>
        )}
      </div>
    </PageTransition>
  );
}
