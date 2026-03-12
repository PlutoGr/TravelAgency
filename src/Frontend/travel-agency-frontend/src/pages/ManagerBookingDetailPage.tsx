import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  User,
  MapPin,
  Calendar,
  Users,
  Wallet,
  StickyNote,
  Clock,
  Save,
  Link2,
  FileText,
  Search,
  X,
  Star,
} from 'lucide-react';
import { format } from 'date-fns';
import { ru } from 'date-fns/locale';
import type { Booking, BookingStatus, Tour } from '@/types';
import { getBookingById, updateBookingStatus } from '@/api/bookings';
import { mockTours } from '@/mocks/tours';
import { mockManager } from '@/mocks/users';
import { Card, Button, Select, Modal, Skeleton } from '@/components/ui';
import { BookingStatusBadge } from '@/components/booking';
import ChatWindow from '@/components/booking/ChatWindow.tsx';
import { Breadcrumbs } from '@/components/layout';
import { PageTransition } from '@/components/common';

const STATUS_OPTIONS = [
  { value: 'new', label: 'Новая' },
  { value: 'in_progress', label: 'В работе' },
  { value: 'proposal_sent', label: 'Предложение отправлено' },
  { value: 'confirmed', label: 'Подтверждена' },
  { value: 'closed', label: 'Закрыта' },
];

const QUICK_REPLIES = [
  'Добрый день! Мы подобрали для вас тур.',
  'Прикрепляю предложение по туру.',
  'Тур подтверждён! Ожидайте оплату.',
];

const MOCK_CLIENT_DETAILS: Record<string, { email: string; phone: string }> = {
  'user-1': { email: 'ivan.ivanov@mail.ru', phone: '+7 (999) 123-45-67' },
  'user-2': { email: 'maria.lebedeva@gmail.com', phone: '+7 (916) 555-12-34' },
  'user-3': { email: 'alexey.novikov@yandex.ru', phone: '+7 (903) 777-88-99' },
  'user-4': { email: 'elena.smirnova@mail.ru', phone: '+7 (926) 333-44-55' },
  'user-5': { email: 'dmitry.kozlov@gmail.com', phone: '+7 (905) 111-22-33' },
  'user-6': { email: 'natalia.sokolova@yandex.ru', phone: '+7 (917) 666-77-88' },
};

function formatDate(dateStr: string): string {
  return format(new Date(dateStr), 'd MMMM yyyy', { locale: ru });
}

function formatBudget(amount: number): string {
  return amount.toLocaleString('ru-RU') + ' ₽';
}

export default function ManagerBookingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [booking, setBooking] = useState<Booking | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [managerNotes, setManagerNotes] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('');
  const [isUpdatingStatus, setIsUpdatingStatus] = useState(false);
  const [showTourModal, setShowTourModal] = useState(false);
  const [tourSearch, setTourSearch] = useState('');
  const [attachedTour, setAttachedTour] = useState<Tour | null>(null);
  const [toastMessage, setToastMessage] = useState('');

  const showToast = useCallback((message: string) => {
    setToastMessage(message);
    setTimeout(() => setToastMessage(''), 3000);
  }, []);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;

    setIsLoading(true);
    getBookingById(id)
      .then((data) => {
        if (cancelled) return;
        setBooking(data);
        setSelectedStatus(data.status);
        setManagerNotes(data.notes);
        if (data.tour) setAttachedTour(data.tour);
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [id]);

  const handleStatusUpdate = async () => {
    if (!booking || !selectedStatus || selectedStatus === booking.status) return;
    setIsUpdatingStatus(true);
    try {
      const updated = await updateBookingStatus(
        booking.id,
        selectedStatus as BookingStatus,
      );
      setBooking(updated);
      showToast('Статус обновлён');
    } finally {
      setIsUpdatingStatus(false);
    }
  };

  const filteredTours = mockTours.filter((t) => {
    if (!tourSearch) return true;
    const q = tourSearch.toLowerCase();
    return (
      t.title.toLowerCase().includes(q) ||
      t.country.toLowerCase().includes(q) ||
      t.city.toLowerCase().includes(q)
    );
  });

  const clientDetails = booking
    ? MOCK_CLIENT_DETAILS[booking.clientId] ?? {
        email: 'client@email.com',
        phone: '+7 (900) 000-00-00',
      }
    : null;

  if (isLoading) {
    return (
      <PageTransition>
        <div className="space-y-6">
          <Skeleton width={300} height={20} />
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
            <div className="space-y-4 lg:col-span-4">
              <Skeleton height={200} variant="rectangular" />
              <Skeleton height={200} variant="rectangular" />
            </div>
            <div className="lg:col-span-5">
              <Skeleton height={500} variant="rectangular" />
            </div>
            <div className="lg:col-span-3">
              <Skeleton height={300} variant="rectangular" />
            </div>
          </div>
        </div>
      </PageTransition>
    );
  }

  if (!booking) {
    return (
      <PageTransition>
        <div className="py-20 text-center text-warm-gray">
          <p className="text-lg font-medium">Заявка не найдена</p>
        </div>
      </PageTransition>
    );
  }

  const breadcrumbs = [
    { label: 'Панель менеджера', path: '/manager' },
    { label: 'Бронирования', path: '/manager/bookings' },
    { label: `Заявка ${formatBookingId(booking.id)}` },
  ];

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={breadcrumbs} />

        <h1 className="font-heading text-xl font-bold text-dark sm:text-2xl">
          Заявка {formatBookingId(booking.id)}
        </h1>

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
          {/* LEFT COLUMN */}
          <div className="space-y-5 lg:col-span-4 xl:col-span-3">
            {/* Client info */}
            <Card className="p-5">
              <h3 className="mb-4 flex items-center gap-2 font-heading text-sm font-semibold text-dark">
                <User size={16} className="text-primary" />
                Данные клиента
              </h3>
              <div className="space-y-3 text-sm">
                <div>
                  <p className="text-xs text-warm-gray">Имя</p>
                  <p className="font-medium text-dark">{booking.clientName}</p>
                </div>
                <div>
                  <p className="text-xs text-warm-gray">Email</p>
                  <p className="text-dark">{clientDetails!.email}</p>
                </div>
                <div>
                  <p className="text-xs text-warm-gray">Телефон</p>
                  <p className="text-dark">{clientDetails!.phone}</p>
                </div>
              </div>
            </Card>

            {/* Booking info */}
            <Card className="p-5">
              <h3 className="mb-4 flex items-center gap-2 font-heading text-sm font-semibold text-dark">
                <FileText size={16} className="text-primary" />
                Информация о заявке
              </h3>
              <div className="space-y-3 text-sm">
                <InfoRow
                  icon={MapPin}
                  label="Направление"
                  value={`${booking.destination}, ${booking.country}`}
                />
                <InfoRow
                  icon={Calendar}
                  label="Даты"
                  value={`${formatDate(booking.dateFrom)} — ${formatDate(booking.dateTo)}`}
                />
                <InfoRow
                  icon={Users}
                  label="Туристы"
                  value={String(booking.travelers)}
                />
                <InfoRow
                  icon={Wallet}
                  label="Бюджет"
                  value={formatBudget(booking.budget)}
                />
                <InfoRow
                  icon={StickyNote}
                  label="Заметки клиента"
                  value={booking.notes || '—'}
                />
                <InfoRow
                  icon={Clock}
                  label="Дата создания"
                  value={formatDate(booking.createdAt)}
                />
              </div>
            </Card>

            {/* Manager notes */}
            <Card className="p-5">
              <h3 className="mb-3 flex items-center gap-2 font-heading text-sm font-semibold text-dark">
                <StickyNote size={16} className="text-primary" />
                Заметки менеджера
              </h3>
              <textarea
                value={managerNotes}
                onChange={(e) => setManagerNotes(e.target.value)}
                rows={4}
                placeholder="Внутренние заметки по заявке..."
                className="w-full resize-none rounded-[12px] border border-sand bg-cream px-4 py-3 text-sm text-dark outline-none transition-all placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
              />
              <Button
                variant="secondary"
                size="sm"
                leftIcon={<Save size={14} />}
                className="mt-3"
                onClick={() => showToast('Заметки сохранены')}
              >
                Сохранить
              </Button>
            </Card>
          </div>

          {/* MIDDLE COLUMN — Chat */}
          <div className="lg:col-span-5 xl:col-span-6">
            <Card className="flex h-[600px] flex-col overflow-hidden">
              <div className="border-b border-sand px-5 py-3.5">
                <h3 className="font-heading text-sm font-semibold text-dark">
                  Чат с клиентом
                </h3>
              </div>
              <div className="flex-1 overflow-hidden">
                <ChatWindow
                  bookingId={booking.id}
                  currentUserId={mockManager.id}
                  currentUserName={`${mockManager.firstName} ${mockManager.lastName}`}
                  currentUserRole="manager"
                />
              </div>
              {/* Quick replies */}
              <div className="border-t border-sand px-4 py-3">
                <p className="mb-2 text-xs font-medium text-warm-gray">
                  Быстрые ответы:
                </p>
                <div className="flex flex-wrap gap-2">
                  {QUICK_REPLIES.map((text) => (
                    <button
                      key={text}
                      className="rounded-lg bg-cream px-3 py-1.5 text-xs text-dark transition-colors hover:bg-sand"
                      onClick={() => showToast('Шаблон скопирован')}
                    >
                      {text}
                    </button>
                  ))}
                </div>
              </div>
            </Card>
          </div>

          {/* RIGHT COLUMN — Actions */}
          <div className="space-y-5 lg:col-span-3">
            {/* Status / Actions */}
            <Card className="p-5">
              <h3 className="mb-4 font-heading text-sm font-semibold text-dark">
                Действия
              </h3>

              <div className="mb-4">
                <p className="mb-1.5 text-xs text-warm-gray">
                  Текущий статус
                </p>
                <BookingStatusBadge status={booking.status} />
              </div>

              <div className="mb-3">
                <Select
                  options={STATUS_OPTIONS}
                  value={selectedStatus}
                  onChange={setSelectedStatus}
                  label="Изменить статус"
                />
              </div>

              <Button
                variant="primary"
                size="sm"
                fullWidth
                isLoading={isUpdatingStatus}
                onClick={handleStatusUpdate}
                disabled={selectedStatus === booking.status}
              >
                Применить
              </Button>

              <div className="my-4 border-t border-sand" />

              <Button
                variant="secondary"
                size="sm"
                fullWidth
                leftIcon={<Link2 size={14} />}
                onClick={() => setShowTourModal(true)}
              >
                Прикрепить тур
              </Button>

              {attachedTour && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: 'auto' }}
                  className="mt-3 overflow-hidden rounded-[12px] border border-sand"
                >
                  <img
                    src={attachedTour.photos[0]}
                    alt={attachedTour.title}
                    className="h-28 w-full object-cover"
                  />
                  <div className="p-3">
                    <p className="text-xs font-semibold text-dark">
                      {attachedTour.title}
                    </p>
                    <p className="mt-0.5 text-xs text-warm-gray">
                      {attachedTour.country}, {attachedTour.city}
                    </p>
                    <div className="mt-1 flex items-center gap-1">
                      <Star
                        size={12}
                        className="fill-amber-400 text-amber-400"
                      />
                      <span className="text-xs text-warm-gray">
                        {attachedTour.rating}
                      </span>
                    </div>
                    <p className="mt-1 text-sm font-bold text-primary">
                      {formatBudget(attachedTour.price)}
                    </p>
                  </div>
                </motion.div>
              )}

              <div className="my-4 border-t border-sand" />

              <Button
                variant="ghost"
                size="sm"
                fullWidth
                leftIcon={<FileText size={14} />}
                onClick={() => showToast('Счёт создан и отправлен клиенту')}
              >
                Создать счёт
              </Button>
            </Card>
          </div>
        </div>
      </div>

      {/* Tour selection modal */}
      <Modal
        isOpen={showTourModal}
        onClose={() => setShowTourModal(false)}
        title="Прикрепить тур"
        size="lg"
      >
        <div className="space-y-4">
          <div className="relative">
            <Search
              size={18}
              className="absolute left-3.5 top-1/2 -translate-y-1/2 text-warm-gray"
            />
            <input
              type="text"
              placeholder="Поиск по названию, стране..."
              value={tourSearch}
              onChange={(e) => setTourSearch(e.target.value)}
              className="w-full rounded-[12px] border border-sand bg-white py-2.5 pl-10 pr-4 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
            />
          </div>

          <div className="max-h-[400px] space-y-2 overflow-y-auto pr-1">
            {filteredTours.map((tour) => (
              <button
                key={tour.id}
                onClick={() => {
                  setAttachedTour(tour);
                  setShowTourModal(false);
                  showToast(`Тур "${tour.title}" прикреплён`);
                }}
                className="flex w-full items-center gap-3 rounded-[12px] p-3 text-left transition-colors hover:bg-cream"
              >
                <img
                  src={tour.photos[0]}
                  alt={tour.title}
                  className="h-14 w-14 shrink-0 rounded-lg object-cover"
                />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-dark">
                    {tour.title}
                  </p>
                  <p className="text-xs text-warm-gray">
                    {tour.country}, {tour.city}
                  </p>
                  <p className="mt-0.5 text-sm font-semibold text-primary">
                    {formatBudget(tour.price)}
                  </p>
                </div>
                <div className="flex items-center gap-1">
                  <Star
                    size={12}
                    className="fill-amber-400 text-amber-400"
                  />
                  <span className="text-xs text-warm-gray">{tour.rating}</span>
                </div>
              </button>
            ))}

            {filteredTours.length === 0 && (
              <p className="py-8 text-center text-sm text-warm-gray">
                Туры не найдены
              </p>
            )}
          </div>
        </div>
      </Modal>

      {/* Toast */}
      <AnimatePresence>
        {toastMessage && (
          <motion.div
            initial={{ opacity: 0, y: 50 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 50 }}
            className="fixed bottom-6 left-1/2 z-50 -translate-x-1/2"
          >
            <div className="flex items-center gap-2 rounded-xl bg-dark px-5 py-3 text-sm font-medium text-white shadow-modal">
              {toastMessage}
              <button onClick={() => setToastMessage('')}>
                <X size={16} className="text-white/60 hover:text-white" />
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </PageTransition>
  );
}

function formatBookingId(id: string): string {
  return '#BK-' + (id.split('-')[1]?.padStart(3, '0') ?? id);
}

function InfoRow({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof MapPin;
  label: string;
  value: string;
}) {
  return (
    <div>
      <p className="mb-0.5 flex items-center gap-1.5 text-xs text-warm-gray">
        <Icon size={13} />
        {label}
      </p>
      <p className="text-dark">{value}</p>
    </div>
  );
}
