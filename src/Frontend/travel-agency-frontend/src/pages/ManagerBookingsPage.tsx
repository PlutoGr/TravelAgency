import { useState, useEffect, useMemo, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  Search,
  Eye,
  Calendar,
  MapPin,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import clsx from 'clsx';
import type { Booking, BookingStatus } from '@/types';
import { getAllBookings } from '@/api/bookings';
import { Card, Select, Skeleton } from '@/components/ui';
import { BookingStatusBadge } from '@/components/booking';
import { Breadcrumbs } from '@/components/layout';
import { PageTransition } from '@/components/common';

const STATUS_OPTIONS = [
  { value: '', label: 'Все статусы' },
  { value: 'new', label: 'Новые' },
  { value: 'in_progress', label: 'В работе' },
  { value: 'proposal_sent', label: 'Предложение' },
  { value: 'confirmed', label: 'Подтверждённые' },
  { value: 'closed', label: 'Закрытые' },
];

const PAGE_SIZE = 5;

const BREADCRUMBS = [
  { label: 'Панель менеджера', path: '/manager' },
  { label: 'Бронирования' },
];

function formatBookingId(id: string): string {
  return '#BK-' + (id.split('-')[1]?.padStart(3, '0') ?? id);
}

function formatDate(dateStr: string): string {
  return format(new Date(dateStr), 'd MMM yyyy', { locale: ru });
}

function formatDateRange(from: string, to: string): string {
  return `${format(new Date(from), 'd MMM', { locale: ru })} — ${format(new Date(to), 'd MMM yyyy', { locale: ru })}`;
}

export default function ManagerBookingsPage() {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [currentPage, setCurrentPage] = useState(1);

  const loadBookings = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await getAllBookings({
        status: (statusFilter || undefined) as BookingStatus | undefined,
        search: searchQuery || undefined,
      });
      setBookings(data);
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter, searchQuery]);

  useEffect(() => {
    loadBookings();
  }, [loadBookings]);

  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, statusFilter, dateFrom, dateTo]);

  const filteredBookings = useMemo(() => {
    let result = bookings;

    if (dateFrom) {
      result = result.filter((b) => b.dateFrom >= dateFrom);
    }
    if (dateTo) {
      result = result.filter((b) => b.dateTo <= dateTo);
    }

    return result.sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    );
  }, [bookings, dateFrom, dateTo]);

  const totalPages = Math.max(1, Math.ceil(filteredBookings.length / PAGE_SIZE));
  const paginatedBookings = filteredBookings.slice(
    (currentPage - 1) * PAGE_SIZE,
    currentPage * PAGE_SIZE,
  );

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">
          Бронирования
        </h1>

        {/* Filters */}
        <Card className="p-4 sm:p-5">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {/* Search */}
            <div className="relative sm:col-span-2 lg:col-span-1">
              <Search
                size={18}
                className="absolute left-3.5 top-1/2 -translate-y-1/2 text-warm-gray"
              />
              <input
                type="text"
                placeholder="Поиск по клиенту или направлению..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full rounded-[12px] border border-sand bg-white py-2.5 pl-10 pr-4 text-sm text-dark outline-none transition-all placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
              />
            </div>

            {/* Status */}
            <Select
              options={STATUS_OPTIONS}
              value={statusFilter}
              onChange={setStatusFilter}
              placeholder="Все статусы"
            />

            {/* Date from */}
            <div>
              <label className="mb-1.5 block text-xs font-medium text-warm-gray">
                Дата от
              </label>
              <input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="w-full rounded-[12px] border border-sand bg-white px-4 py-2.5 text-sm text-dark outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/10"
              />
            </div>

            {/* Date to */}
            <div>
              <label className="mb-1.5 block text-xs font-medium text-warm-gray">
                Дата до
              </label>
              <input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="w-full rounded-[12px] border border-sand bg-white px-4 py-2.5 text-sm text-dark outline-none transition-all focus:border-primary focus:ring-2 focus:ring-primary/10"
              />
            </div>
          </div>
        </Card>

        {/* Loading */}
        {isLoading && (
          <Card className="divide-y divide-sand">
            {Array.from({ length: PAGE_SIZE }).map((_, i) => (
              <div key={i} className="flex items-center gap-4 p-5">
                <Skeleton width={60} height={16} />
                <Skeleton width={120} height={16} />
                <Skeleton width={140} height={16} className="hidden sm:block" />
                <Skeleton width={100} height={16} className="hidden lg:block" />
                <Skeleton width={80} height={24} variant="rectangular" />
              </div>
            ))}
          </Card>
        )}

        {/* Desktop table */}
        {!isLoading && (
          <>
            <Card className="hidden overflow-hidden lg:block">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-sand bg-cream/60 text-left">
                      <th className="px-5 py-3.5 font-medium text-warm-gray">
                        ID
                      </th>
                      <th className="px-5 py-3.5 font-medium text-warm-gray">
                        Клиент
                      </th>
                      <th className="px-5 py-3.5 font-medium text-warm-gray">
                        Направление
                      </th>
                      <th className="px-5 py-3.5 font-medium text-warm-gray">
                        Даты
                      </th>
                      <th className="px-5 py-3.5 font-medium text-warm-gray">
                        Статус
                      </th>
                      <th className="px-5 py-3.5 font-medium text-warm-gray">
                        Обновлено
                      </th>
                      <th className="px-5 py-3.5 font-medium text-warm-gray">
                        Действия
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {paginatedBookings.map((booking, idx) => (
                      <motion.tr
                        key={booking.id}
                        initial={{ opacity: 0, y: 8 }}
                        animate={{ opacity: 1, y: 0 }}
                        transition={{ delay: idx * 0.04 }}
                        className="border-b border-sand/60 transition-colors last:border-0 hover:bg-cream/40"
                      >
                        <td className="px-5 py-4 font-mono text-xs text-warm-gray">
                          {formatBookingId(booking.id)}
                        </td>
                        <td className="px-5 py-4 font-medium text-dark">
                          {booking.clientName}
                        </td>
                        <td className="px-5 py-4 text-dark">
                          <span className="flex items-center gap-1.5">
                            <MapPin size={14} className="shrink-0 text-warm-gray" />
                            {booking.destination}, {booking.country}
                          </span>
                        </td>
                        <td className="px-5 py-4 text-warm-gray">
                          <span className="flex items-center gap-1.5">
                            <Calendar size={14} className="shrink-0" />
                            {formatDateRange(booking.dateFrom, booking.dateTo)}
                          </span>
                        </td>
                        <td className="px-5 py-4">
                          <BookingStatusBadge
                            status={booking.status}
                            size="sm"
                          />
                        </td>
                        <td className="px-5 py-4 text-xs text-warm-gray">
                          {formatDate(booking.updatedAt)}
                        </td>
                        <td className="px-5 py-4">
                          <Link
                            to={`/manager/bookings/${booking.id}`}
                            className="inline-flex items-center gap-1.5 text-sm font-medium text-primary transition-colors hover:text-primary-light"
                          >
                            <Eye size={16} />
                            Открыть
                          </Link>
                        </td>
                      </motion.tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {filteredBookings.length === 0 && (
                <div className="py-16 text-center text-warm-gray">
                  <p className="text-base font-medium">
                    Бронирования не найдены
                  </p>
                  <p className="mt-1 text-sm">
                    Попробуйте изменить параметры поиска
                  </p>
                </div>
              )}
            </Card>

            {/* Mobile cards */}
            <div className="space-y-3 lg:hidden">
              {paginatedBookings.map((booking, idx) => (
                <motion.div
                  key={booking.id}
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ delay: idx * 0.05 }}
                >
                  <Link to={`/manager/bookings/${booking.id}`}>
                    <Card className="p-4 transition-shadow hover:shadow-card-hover">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0 flex-1">
                          <p className="font-mono text-xs text-warm-gray">
                            {formatBookingId(booking.id)}
                          </p>
                          <p className="mt-1 font-heading text-sm font-semibold text-dark">
                            {booking.clientName}
                          </p>
                          <p className="mt-0.5 flex items-center gap-1 text-sm text-warm-gray">
                            <MapPin size={13} />
                            {booking.destination}, {booking.country}
                          </p>
                        </div>
                        <BookingStatusBadge status={booking.status} size="sm" />
                      </div>
                      <div className="mt-3 flex items-center justify-between border-t border-sand pt-3 text-xs text-warm-gray">
                        <span className="flex items-center gap-1">
                          <Calendar size={13} />
                          {formatDateRange(booking.dateFrom, booking.dateTo)}
                        </span>
                        <span>Обн: {formatDate(booking.updatedAt)}</span>
                      </div>
                    </Card>
                  </Link>
                </motion.div>
              ))}

              {filteredBookings.length === 0 && (
                <div className="py-16 text-center text-warm-gray">
                  <p className="text-base font-medium">
                    Бронирования не найдены
                  </p>
                </div>
              )}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-center gap-2">
                <button
                  onClick={() =>
                    setCurrentPage((p) => Math.max(1, p - 1))
                  }
                  disabled={currentPage === 1}
                  className="rounded-lg p-2 text-warm-gray transition-colors hover:bg-sand disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  <ChevronLeft size={20} />
                </button>

                {Array.from({ length: totalPages }).map((_, i) => {
                  const page = i + 1;
                  return (
                    <button
                      key={page}
                      onClick={() => setCurrentPage(page)}
                      className={clsx(
                        'h-9 w-9 rounded-lg text-sm font-medium transition-colors',
                        page === currentPage
                          ? 'bg-primary text-white'
                          : 'text-warm-gray hover:bg-sand',
                      )}
                    >
                      {page}
                    </button>
                  );
                })}

                <button
                  onClick={() =>
                    setCurrentPage((p) => Math.min(totalPages, p + 1))
                  }
                  disabled={currentPage === totalPages}
                  className="rounded-lg p-2 text-warm-gray transition-colors hover:bg-sand disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  <ChevronRight size={20} />
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </PageTransition>
  );
}
