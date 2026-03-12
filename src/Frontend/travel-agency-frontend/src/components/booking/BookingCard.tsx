import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { MapPin, Users, Calendar, ArrowRight } from 'lucide-react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import type { Booking } from '@/types';
import BookingStatusBadge from './BookingStatusBadge';

interface BookingCardProps {
  booking: Booking;
}

function formatBudget(amount: number): string {
  return 'от ' + amount.toLocaleString('ru-RU') + ' ₽';
}

function formatDate(dateStr: string): string {
  return format(new Date(dateStr), 'd MMM yyyy', { locale: ru });
}

export default function BookingCard({ booking }: BookingCardProps) {
  const navigate = useNavigate();

  return (
    <motion.div
      whileHover={{ y: -2, boxShadow: '0 8px 30px rgba(0, 0, 0, 0.08)' }}
      transition={{ type: 'spring', stiffness: 300, damping: 25 }}
      onClick={() => navigate(`/dashboard/bookings/${booking.id}`)}
      className="cursor-pointer rounded-[16px] bg-white p-4 shadow-card sm:p-5"
    >
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex flex-1 gap-4">
          {booking.tour?.photos[0] && (
            <img
              src={booking.tour.photos[0]}
              alt={booking.destination}
              className="hidden h-16 w-16 flex-shrink-0 rounded-[12px] object-cover sm:block"
            />
          )}

          <div className="min-w-0 flex-1">
            <h3 className="font-heading text-base font-semibold text-dark">
              {booking.destination}, {booking.country}
            </h3>

            <div className="mt-1.5 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-warm-gray">
              <span className="flex items-center gap-1">
                <Calendar size={14} />
                {formatDate(booking.dateFrom)} — {formatDate(booking.dateTo)}
              </span>
              <span className="flex items-center gap-1">
                <Users size={14} />
                {booking.travelers}{' '}
                {booking.travelers === 1 ? 'турист' : booking.travelers < 5 ? 'туриста' : 'туристов'}
              </span>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-3 sm:flex-col sm:items-end sm:gap-2">
          <BookingStatusBadge status={booking.status} size="sm" />
          <span className="font-heading text-sm font-semibold text-primary">
            {formatBudget(booking.budget)}
          </span>
        </div>
      </div>

      <div className="mt-3 flex items-center justify-between border-t border-sand pt-3">
        <span className="text-xs text-warm-gray">
          Обновлено: {formatDate(booking.updatedAt)}
        </span>
        <span className="flex items-center gap-1 text-sm font-medium text-primary transition-colors hover:text-primary-light">
          Подробнее
          <ArrowRight size={14} />
        </span>
      </div>
    </motion.div>
  );
}
