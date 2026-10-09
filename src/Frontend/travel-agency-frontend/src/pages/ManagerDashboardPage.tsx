import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { motion } from 'framer-motion';
import {
  Inbox,
  Loader,
  Send,
  CheckCircle2,
  ArrowRight,
  Calendar,
  MapPin,
  AlertCircle,
} from 'lucide-react';
import type { BookingStatus } from '@/types';
import { getAllBookings, bookingTourTitle } from '@/api/bookings';
import { useAuthStore } from '@/store/authStore';
import { Card, Button, Skeleton } from '@/components/ui';
import { BookingStatusBadge } from '@/components/booking';
import { PageTransition } from '@/components/common';
import { formatDate, formatBookingId } from '@/utils/format';

const KPI_CONFIG: {
  key: BookingStatus;
  label: string;
  icon: typeof Inbox;
  color: string;
  border: string;
}[] = [
  {
    key: 'new',
    label: 'Новые заявки',
    icon: Inbox,
    color: 'text-blue-600',
    border: 'border-l-blue-500',
  },
  {
    key: 'in_progress',
    label: 'Активные в работе',
    icon: Loader,
    color: 'text-amber-600',
    border: 'border-l-amber-500',
  },
  {
    key: 'proposal_sent',
    label: 'Ожидают ответа',
    icon: Send,
    color: 'text-purple-600',
    border: 'border-l-purple-500',
  },
  {
    key: 'confirmed',
    label: 'Подтверждённые',
    icon: CheckCircle2,
    color: 'text-emerald-600',
    border: 'border-l-emerald-500',
  },
];

const containerVariants = {
  hidden: {},
  show: { transition: { staggerChildren: 0.08 } },
};

const itemVariants = {
  hidden: { opacity: 0, y: 16 },
  show: { opacity: 1, y: 0, transition: { duration: 0.35 } },
};

export default function ManagerDashboardPage() {
  const user = useAuthStore((s) => s.user);

  const { data: bookings = [], isLoading, isError } = useQuery({
    queryKey: ['manager', 'bookings', 'all'],
    queryFn: () => getAllBookings(),
  });

  const kpiCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const cfg of KPI_CONFIG) {
      counts[cfg.key] = bookings.filter((b) => b.status === cfg.key).length;
    }
    return counts;
  }, [bookings]);

  const recentBookings = useMemo(
    () =>
      [...bookings]
        .sort(
          (a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
        )
        .slice(0, 5),
    [bookings],
  );

  const greeting = user
    ? `Добро пожаловать, ${user.firstName}!`
    : 'Добро пожаловать!';

  if (isError) {
    return (
      <PageTransition>
        <div className="space-y-8">
          <div>
            <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">
              Панель менеджера
            </h1>
            <p className="mt-1 text-warm-gray">{greeting}</p>
          </div>
          <Card className="flex flex-col items-center justify-center gap-4 p-8">
            <AlertCircle size={48} className="text-amber-500" />
            <p className="text-center text-warm-gray">
              Не удалось загрузить данные. Попробуйте обновить страницу.
            </p>
            <Button variant="secondary" onClick={() => window.location.reload()}>
              Обновить
            </Button>
          </Card>
        </div>
      </PageTransition>
    );
  }

  return (
    <PageTransition>
      <div className="space-y-8">
        {/* Header */}
        <div>
          <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">
            Панель менеджера
          </h1>
          <p className="mt-1 text-warm-gray">{greeting}</p>
        </div>

        {/* KPI Tiles */}
        {isLoading ? (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {KPI_CONFIG.map((kpi) => (
              <Card key={kpi.key} className="border-l-4 border-sand p-5">
                <Skeleton className="h-9 w-24" />
                <Skeleton className="mt-2 h-4 w-32" />
              </Card>
            ))}
          </div>
        ) : (
        <motion.div
          variants={containerVariants}
          initial="hidden"
          animate="show"
          className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4"
        >
          {KPI_CONFIG.map((kpi) => {
            const Icon = kpi.icon;
            return (
              <motion.div key={kpi.key} variants={itemVariants}>
                <Card
                  className={`border-l-4 ${kpi.border} p-5`}
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="text-3xl font-bold text-dark">
                        {kpiCounts[kpi.key]}
                      </p>
                      <p className="mt-1 text-sm text-warm-gray">{kpi.label}</p>
                    </div>
                    <div
                      className={`rounded-xl bg-sand p-2.5 ${kpi.color}`}
                    >
                      <Icon size={22} />
                    </div>
                  </div>
                </Card>
              </motion.div>
            );
          })}
        </motion.div>
        )}

        {/* Recent Bookings */}
        <div>
          <div className="mb-4 flex items-center justify-between">
            <h2 className="font-heading text-lg font-semibold text-dark">
              Последние заявки
            </h2>
            <Link to="/manager/bookings">
              <Button variant="ghost" size="sm" rightIcon={<ArrowRight size={16} />}>
                Все бронирования
              </Button>
            </Link>
          </div>

          {/* Desktop table */}
          <Card className="hidden overflow-hidden lg:block">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-sand bg-cream/60 text-left">
                    <th className="px-5 py-3 font-medium text-warm-gray">ID</th>
                    <th className="px-5 py-3 font-medium text-warm-gray">
                      Клиент
                    </th>
                    <th className="px-5 py-3 font-medium text-warm-gray">
                      Тур
                    </th>
                    <th className="px-5 py-3 font-medium text-warm-gray">Дата</th>
                    <th className="px-5 py-3 font-medium text-warm-gray">
                      Статус
                    </th>
                    <th className="px-5 py-3 font-medium text-warm-gray">
                      Действия
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {isLoading ? (
                    Array.from({ length: 5 }).map((_, i) => (
                      <tr key={i} className="border-b border-sand/60 last:border-0">
                        <td className="px-5 py-3.5"><Skeleton className="h-4 w-16" /></td>
                        <td className="px-5 py-3.5"><Skeleton className="h-4 w-28" /></td>
                        <td className="px-5 py-3.5"><Skeleton className="h-4 w-24" /></td>
                        <td className="px-5 py-3.5"><Skeleton className="h-4 w-20" /></td>
                        <td className="px-5 py-3.5"><Skeleton className="h-5 w-20" /></td>
                        <td className="px-5 py-3.5"><Skeleton className="h-4 w-16" /></td>
                      </tr>
                    ))
                  ) : (
                  recentBookings.map((booking) => (
                    <tr
                      key={booking.id}
                      className="border-b border-sand/60 transition-colors last:border-0 hover:bg-cream/40"
                    >
                      <td className="px-5 py-3.5 font-mono text-xs text-warm-gray">
                        {formatBookingId(booking.id)}
                      </td>
                      <td className="px-5 py-3.5 font-medium text-dark">
                        {booking.clientName || booking.clientId}
                      </td>
                      <td className="px-5 py-3.5 text-dark">
                        <span className="flex items-center gap-1.5">
                          <MapPin size={14} className="text-warm-gray" />
                          {bookingTourTitle(booking)}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-warm-gray">
                        <span className="flex items-center gap-1.5">
                          <Calendar size={14} />
                          {formatDate(booking.createdAt)}
                        </span>
                      </td>
                      <td className="px-5 py-3.5">
                        <BookingStatusBadge status={booking.status} size="sm" />
                      </td>
                      <td className="px-5 py-3.5">
                        <Link
                          to={`/manager/bookings/${booking.id}`}
                          className="text-sm font-medium text-primary transition-colors hover:text-primary-light"
                        >
                          Открыть
                        </Link>
                      </td>
                    </tr>
                  ))
                  )}
                </tbody>
              </table>
            </div>
          </Card>

          {/* Mobile cards */}
          <div className="space-y-3 lg:hidden">
            {isLoading ? (
              Array.from({ length: 3 }).map((_, i) => (
                <Card key={i} className="p-4">
                  <Skeleton className="h-3 w-16" />
                  <Skeleton className="mt-2 h-4 w-32" />
                  <Skeleton className="mt-1 h-3 w-24" />
                  <Skeleton className="mt-2 h-3 w-20" />
                </Card>
              ))
            ) : (
              recentBookings.map((booking) => (
                <Link key={booking.id} to={`/manager/bookings/${booking.id}`}>
                  <Card className="p-4">
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0 flex-1">
                        <p className="font-mono text-xs text-warm-gray">
                          {formatBookingId(booking.id)}
                        </p>
                        <p className="mt-1 font-heading text-sm font-semibold text-dark">
                          {booking.clientName || booking.clientId}
                        </p>
                        <p className="mt-0.5 text-sm text-warm-gray">
                          {bookingTourTitle(booking)}
                        </p>
                      </div>
                      <BookingStatusBadge status={booking.status} size="sm" />
                    </div>
                    <p className="mt-2 text-xs text-warm-gray">
                      {formatDate(booking.createdAt)}
                    </p>
                  </Card>
                </Link>
              ))
            )}
          </div>
        </div>
      </div>
    </PageTransition>
  );
}
