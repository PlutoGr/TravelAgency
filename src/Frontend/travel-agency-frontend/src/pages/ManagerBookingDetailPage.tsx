import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import {
  User,
  MapPin,
  StickyNote,
  Clock,
  Save,
  Send,
  FileText,
  X,
  Star,
} from 'lucide-react';
import {
  formatDateFull,
  formatMoney,
  formatBookingId,
} from '@/utils/format';
import { mediaImageUrl } from '@/utils/media';
import type { Booking, BookingStatus } from '@/types';
import {
  getBookingById,
  updateBookingStatus,
  createProposal,
  bookingTourTitle,
} from '@/api/bookings';
import toast from 'react-hot-toast';
import { useAuthStore } from '@/store/authStore';
import { Card, Button, Select, Skeleton } from '@/components/ui';
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

export default function ManagerBookingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const user = useAuthStore((s) => s.user);
  const [booking, setBooking] = useState<Booking | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [managerNotes, setManagerNotes] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('');
  const [isUpdatingStatus, setIsUpdatingStatus] = useState(false);
  const [isSendingProposal, setIsSendingProposal] = useState(false);
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
      })
      .catch(() => {
        if (!cancelled) toast.error('Не удалось загрузить бронирование');
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
    } catch {
      toast.error('Не удалось обновить статус');
    } finally {
      setIsUpdatingStatus(false);
    }
  };

  const handleSendProposal = async () => {
    if (!booking || !id) return;
    setIsSendingProposal(true);
    try {
      await createProposal(booking.id, { notes: managerNotes });
      await updateBookingStatus(booking.id, 'proposal_sent');
      const refreshed = await getBookingById(id);
      setBooking(refreshed);
      setSelectedStatus(refreshed.status);
      showToast('Предложение отправлено');
    } catch {
      showToast('Ошибка при отправке предложения');
    } finally {
      setIsSendingProposal(false);
    }
  };

  const clientEmail = booking?.clientEmail ?? '—';
  const clientPhone = booking?.clientPhone ?? '—';

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
                  <p className="font-medium text-dark">
                    {booking.clientName || booking.clientId}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-warm-gray">Email</p>
                  <p className="text-dark">{clientEmail}</p>
                </div>
                <div>
                  <p className="text-xs text-warm-gray">Телефон</p>
                  <p className="text-dark">{clientPhone}</p>
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
                  label="Тур"
                  value={bookingTourTitle(booking)}
                />
                <InfoRow
                  icon={StickyNote}
                  label="Комментарий клиента"
                  value={booking.comment?.trim() || '—'}
                />
                <InfoRow
                  icon={Clock}
                  label="Дата создания"
                  value={formatDateFull(booking.createdAt)}
                />
                {booking.proposals.length > 0 && (
                  <div>
                    <p className="mb-1 text-xs text-warm-gray">Предложения</p>
                    <ul className="space-y-1 text-dark">
                      {booking.proposals.map((proposal) => (
                        <li key={proposal.id}>
                          {proposal.tourSnapshot.title || 'Тур'}
                          {proposal.isConfirmed ? ' · подтверждено' : ''}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
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
                disabled
                title="Скоро"
              >
                Сохранить (скоро)
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
                  currentUserId={user?.id ?? ''}
                  currentUserName={
                    user ? `${user.firstName} ${user.lastName}` : ''
                  }
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
                      onClick={async () => {
                        try {
                          await navigator.clipboard.writeText(text);
                          showToast('Шаблон скопирован');
                        } catch {
                          showToast('Не удалось скопировать');
                        }
                      }}
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

              {booking.status === 'new' || booking.status === 'in_progress' ? (
                <Button
                  variant="primary"
                  size="sm"
                  fullWidth
                  leftIcon={<Send size={14} />}
                  isLoading={isSendingProposal}
                  onClick={handleSendProposal}
                >
                  Отправить предложение
                </Button>
              ) : null}

              {booking.tour && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: 'auto' }}
                  className="mt-3 overflow-hidden rounded-[12px] border border-sand"
                >
                  {booking.tour.coverMediaFileId && (
                    <img
                      src={mediaImageUrl(booking.tour.coverMediaFileId, 'w800')}
                      alt={booking.tour.title}
                      className="h-28 w-full object-cover"
                    />
                  )}
                  <div className="p-3">
                    <p className="text-xs font-semibold text-dark">
                      {booking.tour.title}
                    </p>
                    <p className="mt-0.5 text-xs text-warm-gray">
                      {booking.tour.country || booking.tour.city
                        ? `${booking.tour.country}, ${booking.tour.city}`
                        : booking.tour.duration + ' дн.'}
                    </p>
                    <div className="mt-1 flex items-center gap-1">
                      <Star
                        size={12}
                        className="fill-amber-400 text-amber-400"
                      />
                      <span className="text-xs text-warm-gray">
                        {booking.tour.rating || '—'}
                      </span>
                    </div>
                    <p className="mt-1 text-sm font-bold text-primary">
                      {formatMoney(booking.tour.price, booking.tour.currency)}
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
                onClick={() => showToast('Скоро')}
                title="Скоро"
              >
                Создать счёт (скоро)
              </Button>
            </Card>
          </div>
        </div>
      </div>

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
