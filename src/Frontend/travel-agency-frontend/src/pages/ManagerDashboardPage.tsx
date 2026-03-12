import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  Inbox,
  Loader,
  Send,
  CheckCircle2,
  ArrowRight,
  Calendar,
  MapPin,
} from 'lucide-react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import type { BookingStatus } from '@/types';
import { mockBookings } from '@/mocks/bookings';
import { useAuthStore } from '@/store/authStore';
import { Card, Button } from '@/components/ui';
import { BookingStatusBadge } from '@/components/booking';
import { PageTransition } from '@/components/common';

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

function formatDate(dateStr: string): string {
  return format(new Date(dateStr), 'd MMM yyyy', { locale: ru });
}

export default function ManagerDashboardPage() {
  const user = useAuthStore((s) => s.user);

  const kpiCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const cfg of KPI_CONFIG) {
      counts[cfg.key] = mockBookings.filter((b) => b.status === cfg.key).length;
    }
    return counts;
  }, []);

  const recentBookings = useMemo(
    () =>
      [...mockBookings]
        .sort(
          (a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
        )
        .slice(0, 5),
    [],
  );

  const greeting = user
    ? `Добро пожаловать, ${user.firstName}!`
    : 'Добро пожаловать!';

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
                      Направление
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
                  {recentBookings.map((booking) => (
                    <tr
                      key={booking.id}
                      className="border-b border-sand/60 transition-colors last:border-0 hover:bg-cream/40"
                    >
                      <td className="px-5 py-3.5 font-mono text-xs text-warm-gray">
                        #BK-{booking.id.split('-')[1]?.padStart(3, '0')}
                      </td>
                      <td className="px-5 py-3.5 font-medium text-dark">
                        {booking.clientName}
                      </td>
                      <td className="px-5 py-3.5 text-dark">
                        <span className="flex items-center gap-1.5">
                          <MapPin size={14} className="text-warm-gray" />
                          {booking.destination}, {booking.country}
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
                  ))}
                </tbody>
              </table>
            </div>
          </Card>

          {/* Mobile cards */}
          <div className="space-y-3 lg:hidden">
            {recentBookings.map((booking) => (
              <Link key={booking.id} to={`/manager/bookings/${booking.id}`}>
                <Card className="p-4">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 flex-1">
                      <p className="font-mono text-xs text-warm-gray">
                        #BK-{booking.id.split('-')[1]?.padStart(3, '0')}
                      </p>
                      <p className="mt-1 font-heading text-sm font-semibold text-dark">
                        {booking.clientName}
                      </p>
                      <p className="mt-0.5 text-sm text-warm-gray">
                        {booking.destination}, {booking.country}
                      </p>
                    </div>
                    <BookingStatusBadge status={booking.status} size="sm" />
                  </div>
                  <p className="mt-2 text-xs text-warm-gray">
                    {formatDate(booking.createdAt)}
                  </p>
                </Card>
              </Link>
            ))}
          </div>
        </div>
      </div>
    </PageTransition>
  );
}
