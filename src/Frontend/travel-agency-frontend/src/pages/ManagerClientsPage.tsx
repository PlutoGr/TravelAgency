import { useState, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Search,
  ChevronDown,
  Mail,
  Phone,
  Briefcase,
  Calendar,
} from 'lucide-react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import clsx from 'clsx';
import type { Booking } from '@/types';
import { mockBookings } from '@/mocks/bookings';
import { Card, Avatar } from '@/components/ui';
import { BookingCard } from '@/components/booking';
import { Breadcrumbs } from '@/components/layout';
import { PageTransition } from '@/components/common';

const BREADCRUMBS = [
  { label: 'Панель менеджера', path: '/manager' },
  { label: 'Клиенты' },
];

interface ClientInfo {
  id: string;
  name: string;
  email: string;
  phone: string;
  avatar?: string;
  bookings: Booking[];
  lastBookingDate: string;
}

const MOCK_CLIENT_EMAILS: Record<string, string> = {
  'user-1': 'ivan.ivanov@mail.ru',
  'user-2': 'maria.lebedeva@gmail.com',
  'user-3': 'alexey.novikov@yandex.ru',
  'user-4': 'elena.smirnova@mail.ru',
  'user-5': 'dmitry.kozlov@gmail.com',
  'user-6': 'natalia.sokolova@yandex.ru',
};

const MOCK_CLIENT_PHONES: Record<string, string> = {
  'user-1': '+7 (999) 123-45-67',
  'user-2': '+7 (916) 555-12-34',
  'user-3': '+7 (903) 777-88-99',
  'user-4': '+7 (926) 333-44-55',
  'user-5': '+7 (905) 111-22-33',
  'user-6': '+7 (917) 666-77-88',
};

function buildClients(): ClientInfo[] {
  const clientMap = new Map<string, ClientInfo>();

  for (const booking of mockBookings) {
    const existing = clientMap.get(booking.clientId);
    if (existing) {
      existing.bookings.push(booking);
      if (booking.createdAt > existing.lastBookingDate) {
        existing.lastBookingDate = booking.createdAt;
      }
    } else {
      clientMap.set(booking.clientId, {
        id: booking.clientId,
        name: booking.clientName,
        email: MOCK_CLIENT_EMAILS[booking.clientId] ?? `${booking.clientId}@email.com`,
        phone: MOCK_CLIENT_PHONES[booking.clientId] ?? '+7 (900) 000-00-00',
        bookings: [booking],
        lastBookingDate: booking.createdAt,
      });
    }
  }

  return Array.from(clientMap.values()).sort(
    (a, b) =>
      new Date(b.lastBookingDate).getTime() -
      new Date(a.lastBookingDate).getTime(),
  );
}

function formatDate(dateStr: string): string {
  return format(new Date(dateStr), 'd MMM yyyy', { locale: ru });
}

export default function ManagerClientsPage() {
  const clients = useMemo(buildClients, []);
  const [searchQuery, setSearchQuery] = useState('');
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const filteredClients = useMemo(() => {
    if (!searchQuery) return clients;
    const q = searchQuery.toLowerCase();
    return clients.filter(
      (c) =>
        c.name.toLowerCase().includes(q) ||
        c.email.toLowerCase().includes(q),
    );
  }, [clients, searchQuery]);

  const toggleExpanded = (clientId: string) => {
    setExpandedId((prev) => (prev === clientId ? null : clientId));
  };

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">
          Клиенты
        </h1>

        {/* Search */}
        <Card className="p-4 sm:p-5">
          <div className="relative max-w-md">
            <Search
              size={18}
              className="absolute left-3.5 top-1/2 -translate-y-1/2 text-warm-gray"
            />
            <input
              type="text"
              placeholder="Поиск по имени или email..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full rounded-[12px] border border-sand bg-white py-2.5 pl-10 pr-4 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
            />
          </div>
        </Card>

        {/* Client list */}
        <div className="space-y-3">
          {filteredClients.map((client, idx) => {
            const isExpanded = expandedId === client.id;

            return (
              <motion.div
                key={client.id}
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: idx * 0.05 }}
              >
                <Card className="overflow-hidden">
                  {/* Client header row */}
                  <button
                    onClick={() => toggleExpanded(client.id)}
                    className="flex w-full items-center gap-4 p-4 text-left transition-colors hover:bg-cream/40 sm:p-5"
                  >
                    <Avatar name={client.name} size="lg" />

                    <div className="min-w-0 flex-1">
                      <h3 className="font-heading text-base font-semibold text-dark">
                        {client.name}
                      </h3>
                      <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-warm-gray">
                        <span className="flex items-center gap-1">
                          <Mail size={13} />
                          {client.email}
                        </span>
                        <span className="hidden items-center gap-1 sm:flex">
                          <Phone size={13} />
                          {client.phone}
                        </span>
                      </div>
                    </div>

                    <div className="hidden flex-shrink-0 text-right sm:block">
                      <div className="flex items-center gap-1.5 text-sm text-warm-gray">
                        <Briefcase size={14} />
                        <span>
                          {client.bookings.length}{' '}
                          {pluralizeBookings(client.bookings.length)}
                        </span>
                      </div>
                      <p className="mt-0.5 flex items-center gap-1 text-xs text-warm-gray">
                        <Calendar size={12} />
                        {formatDate(client.lastBookingDate)}
                      </p>
                    </div>

                    <ChevronDown
                      size={20}
                      className={clsx(
                        'shrink-0 text-warm-gray transition-transform duration-200',
                        isExpanded && 'rotate-180',
                      )}
                    />
                  </button>

                  {/* Mobile stats */}
                  <div className="flex items-center gap-4 border-t border-sand px-5 py-2.5 text-xs text-warm-gray sm:hidden">
                    <span className="flex items-center gap-1">
                      <Phone size={12} />
                      {client.phone}
                    </span>
                    <span className="flex items-center gap-1">
                      <Briefcase size={12} />
                      {client.bookings.length}{' '}
                      {pluralizeBookings(client.bookings.length)}
                    </span>
                  </div>

                  {/* Expanded booking history */}
                  <AnimatePresence>
                    {isExpanded && (
                      <motion.div
                        initial={{ height: 0, opacity: 0 }}
                        animate={{ height: 'auto', opacity: 1 }}
                        exit={{ height: 0, opacity: 0 }}
                        transition={{ duration: 0.25 }}
                        className="overflow-hidden"
                      >
                        <div className="border-t border-sand bg-cream/30 p-4 sm:p-5">
                          <h4 className="mb-3 text-sm font-semibold text-dark">
                            История бронирований
                          </h4>
                          <div className="space-y-3">
                            {client.bookings.map((booking) => (
                              <BookingCard
                                key={booking.id}
                                booking={booking}
                              />
                            ))}
                          </div>
                        </div>
                      </motion.div>
                    )}
                  </AnimatePresence>
                </Card>
              </motion.div>
            );
          })}

          {filteredClients.length === 0 && (
            <div className="py-16 text-center text-warm-gray">
              <p className="text-base font-medium">Клиенты не найдены</p>
              <p className="mt-1 text-sm">
                Попробуйте изменить параметры поиска
              </p>
            </div>
          )}
        </div>
      </div>
    </PageTransition>
  );
}

function pluralizeBookings(count: number): string {
  const mod10 = count % 10;
  const mod100 = count % 100;

  if (mod100 >= 11 && mod100 <= 19) return 'бронирований';
  if (mod10 === 1) return 'бронирование';
  if (mod10 >= 2 && mod10 <= 4) return 'бронирования';
  return 'бронирований';
}
