import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { FileText, Check, MapPin, Wallet, Calendar } from 'lucide-react';
import toast from 'react-hot-toast';
import type { Tour } from '@/types';
import { Button } from '@/components/ui';
import { createBooking } from '@/api/bookings';
import { getTours, getTourById } from '@/api/catalog';
import { mediaImageUrl } from '@/utils/media';
import { formatFromMoney, formatMoney } from '@/utils/format';

/** Цена, которую страница тура уже показала гостю. */
export type PriceQuote = {
  amount: number;
  currency?: string | null;
  /** Нет конкретной даты: на странице это «от …». */
  from: boolean;
};

interface BookingFormProps {
  /** Pre-selected tour (e.g. from tour detail page) */
  tourId?: string;
  /** Tours to choose from; when absent, fetches from catalog */
  tours?: Tour[];
  /** Даты выбранного предложения, чтобы менеджер увидел их в комментарии. */
  initialComment?: string;
  /** Цена выбранной даты или «от …», если даты нет. */
  priceQuote?: PriceQuote;
  /** Подпись выбранных дат, как на странице тура. */
  scheduleLabel?: string;
  onSuccess?: () => void;
  onClose?: () => void;
}

/** Без точных часов: срок ответа менеджера сайт не гарантирует. */
const BOOKING_SENT_MESSAGE = 'Заявка отправлена. Менеджер свяжется с вами в рабочее время';

type StepId = 'tour' | 'comment' | 'confirm';

const CATALOG_STEPS: { id: StepId; title: string }[] = [
  { id: 'tour', title: 'Выбор тура' },
  { id: 'comment', title: 'Комментарий' },
  { id: 'confirm', title: 'Подтверждение' },
];

const TOUR_CARD_STEPS: { id: StepId; title: string }[] = [
  { id: 'comment', title: 'Комментарий' },
  { id: 'confirm', title: 'Подтверждение' },
];

const slideVariants = {
  enter: (direction: number) => ({
    x: direction > 0 ? 200 : -200,
    opacity: 0,
  }),
  center: { x: 0, opacity: 1 },
  exit: (direction: number) => ({
    x: direction > 0 ? -200 : 200,
    opacity: 0,
  }),
};

function formatQuote(quote: PriceQuote): string | null {
  if (quote.from) {
    if (quote.amount <= 0) return null;
    return formatFromMoney(quote.amount, quote.currency);
  }
  return formatMoney(quote.amount, quote.currency);
}

function formatCatalogPrice(tour: Tour): string | null {
  const amount = tour.priceFrom ?? (tour.price > 0 ? tour.price : null);
  if (amount == null || amount <= 0) return null;
  return formatFromMoney(amount, tour.currency);
}

export default function BookingForm({
  tourId: initialTourId,
  tours: initialTours,
  initialComment = '',
  priceQuote,
  scheduleLabel,
  onSuccess,
  onClose,
}: BookingFormProps) {
  const fromTourCard = Boolean(initialTourId);
  const steps = fromTourCard ? TOUR_CARD_STEPS : CATALOG_STEPS;
  const [stepIndex, setStepIndex] = useState(0);
  const [direction, setDirection] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createdId, setCreatedId] = useState<string | null>(null);
  const [loadedTours, setLoadedTours] = useState<Tour[]>([]);
  const [pickedTour, setPickedTour] = useState<Tour | null>(null);
  const [comment, setComment] = useState(initialComment);
  const hasInitialTours = Boolean(initialTours?.length);
  const [isLoadingTours, setIsLoadingTours] = useState(!hasInitialTours);

  const tours = hasInitialTours ? (initialTours ?? []) : loadedTours;
  const selectedTour =
    pickedTour ?? (initialTourId ? tours.find((item) => item.id === initialTourId) ?? null : null);
  const step = steps[stepIndex] ?? steps[0];

  useEffect(() => {
    if (hasInitialTours) return;
    let cancelled = false;
    if (initialTourId) {
      getTourById(initialTourId)
        .then((tour) => {
          if (cancelled) return;
          setLoadedTours([tour]);
          setPickedTour(tour);
        })
        .catch(() => {
          if (!cancelled) toast.error('Не удалось загрузить тур');
        })
        .finally(() => {
          if (!cancelled) setIsLoadingTours(false);
        });
      return () => {
        cancelled = true;
      };
    }
    getTours()
      .then((res) => {
        if (!cancelled) setLoadedTours(res.items);
      })
      .catch(() => {
        if (!cancelled) toast.error('Не удалось загрузить список туров');
      })
      .finally(() => {
        if (!cancelled) setIsLoadingTours(false);
      });
    return () => {
      cancelled = true;
    };
  }, [initialTourId, hasInitialTours]);

  function goNext() {
    setDirection(1);
    setStepIndex((current) => Math.min(current + 1, steps.length - 1));
  }

  function goBack() {
    setDirection(-1);
    setStepIndex((current) => Math.max(current - 1, 0));
  }

  function canProceed(): boolean {
    if (step.id === 'tour') return !!selectedTour || !!initialTourId;
    return true;
  }

  async function handleSubmit() {
    const tid = selectedTour?.id ?? initialTourId;
    if (!tid) {
      toast.error('Выберите тур');
      return;
    }
    setIsSubmitting(true);
    try {
      const booking = await createBooking({ tourId: tid, comment: comment || undefined });
      if (fromTourCard) {
        setCreatedId(booking.id);
        return;
      }
      toast.success(BOOKING_SENT_MESSAGE);
      onSuccess?.();
      onClose?.();
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 401) toast.error('Войдите, чтобы оформить заявку');
      else toast.error('Не удалось создать заявку. Попробуйте ещё раз.');
    } finally {
      setIsSubmitting(false);
    }
  }

  const effectiveTour = selectedTour ?? (initialTourId && tours.find((t) => t.id === initialTourId));
  const priceLabel = priceQuote
    ? formatQuote(priceQuote)
    : effectiveTour
      ? formatCatalogPrice(effectiveTour)
      : null;

  if (createdId) {
    return (
      <div className="space-y-6">
        <p className="text-base leading-relaxed text-dark">{BOOKING_SENT_MESSAGE}</p>
        <div className="flex flex-col gap-3 sm:flex-row">
          <Link
            to={`/dashboard/bookings/${createdId}`}
            className="inline-flex items-center justify-center rounded-[12px] bg-primary px-6 py-2.5 text-center font-heading text-base font-semibold text-white shadow-button transition-colors hover:bg-primary-light focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          >
            Перейти к заявке
          </Link>
          <Button variant="secondary" onClick={onClose}>
            Продолжить просмотр
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        {steps.map((item, idx) => (
          <div key={item.id} className="flex items-center">
            <div className="flex items-center gap-2">
              <div
                className={`flex h-8 w-8 items-center justify-center rounded-full text-sm font-semibold transition-colors ${
                  stepIndex >= idx ? 'bg-primary text-white' : 'bg-sand text-warm-gray'
                }`}
              >
                {stepIndex > idx ? <Check size={16} /> : idx + 1}
              </div>
              <span
                className={`hidden text-sm font-medium sm:block ${
                  stepIndex >= idx ? 'text-dark' : 'text-warm-gray'
                }`}
              >
                {item.title}
              </span>
            </div>
            {idx < steps.length - 1 && (
              <div
                className={`mx-3 h-0.5 w-8 rounded sm:w-12 ${
                  stepIndex > idx ? 'bg-primary' : 'bg-sand'
                }`}
              />
            )}
          </div>
        ))}
      </div>

      <div className="relative min-h-[280px] overflow-hidden">
        <AnimatePresence mode="wait" custom={direction}>
          {step.id === 'tour' && (
            <motion.div
              key="step-tour"
              custom={direction}
              variants={slideVariants}
              initial="enter"
              animate="center"
              exit="exit"
              transition={{ duration: 0.25, ease: 'easeInOut' }}
              className="space-y-4"
            >
              {isLoadingTours ? (
                <p className="py-8 text-center text-sm text-warm-gray">
                  Загрузка туров...
                </p>
              ) : tours.length === 0 ? (
                <p className="py-8 text-center text-sm text-warm-gray">
                  Туры не найдены
                </p>
              ) : (
                <div className="max-h-[320px] space-y-2 overflow-y-auto pr-1">
                  {tours.map((tour) => {
                    const label = formatCatalogPrice(tour);
                    return (
                      <button
                        key={tour.id}
                        type="button"
                        onClick={() => setPickedTour(tour)}
                        className={`flex w-full items-center gap-3 rounded-[12px] border p-3 text-left transition-colors ${
                          selectedTour?.id === tour.id
                            ? 'border-primary bg-primary/5'
                            : 'border-sand hover:bg-cream'
                        }`}
                      >
                        {tour.coverMediaFileId && (
                          <img
                            src={mediaImageUrl(tour.coverMediaFileId, 'w200')}
                            alt={tour.title}
                            className="h-14 w-14 shrink-0 rounded-lg object-cover"
                          />
                        )}
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-medium text-dark">
                            {tour.title}
                          </p>
                          <p className="text-xs text-warm-gray">
                            {tour.country} · {tour.duration} дн.
                          </p>
                          {label && (
                            <p className="mt-0.5 text-sm font-semibold text-primary">
                              {label}
                            </p>
                          )}
                        </div>
                      </button>
                    );
                  })}
                </div>
              )}
            </motion.div>
          )}

          {step.id === 'comment' && (
            <motion.div
              key="step-comment"
              custom={direction}
              variants={slideVariants}
              initial="enter"
              animate="center"
              exit="exit"
              transition={{ duration: 0.25, ease: 'easeInOut' }}
              className="space-y-4"
            >
              <div>
                <label className="mb-1.5 block text-xs font-medium text-primary">
                  Пожелания и комментарии
                </label>
                {scheduleLabel && (
                  <p className="mb-2 text-sm text-dark">Выбранные даты: {scheduleLabel}</p>
                )}
                <p className="mb-2 text-xs text-warm-gray">
                  Укажите удобные даты и сколько человек поедет.
                </p>
                <textarea
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                  rows={5}
                  placeholder="Например: хочу поехать 1–8 июня, двое взрослых"
                  className="w-full rounded-[12px] border border-sand bg-white px-4 py-3 text-dark outline-none transition-all placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
                />
              </div>
            </motion.div>
          )}

          {step.id === 'confirm' && (
            <motion.div
              key="step-confirm"
              custom={direction}
              variants={slideVariants}
              initial="enter"
              animate="center"
              exit="exit"
              transition={{ duration: 0.25, ease: 'easeInOut' }}
              className="space-y-4"
            >
              <h3 className="font-heading text-base font-semibold text-dark">
                Проверьте данные заявки
              </h3>

              <div className="space-y-3 rounded-[12px] bg-sand/50 p-4">
                {effectiveTour && (
                  <SummaryRow
                    icon={<MapPin size={16} />}
                    label="Тур"
                    value={`${effectiveTour.title}, ${effectiveTour.country}`}
                  />
                )}
                {scheduleLabel && (
                  <SummaryRow
                    icon={<Calendar size={16} />}
                    label="Даты"
                    value={scheduleLabel}
                  />
                )}
                {priceLabel && (
                  <SummaryRow
                    icon={<Wallet size={16} />}
                    label="Цена"
                    value={priceLabel}
                  />
                )}
                {comment && (
                  <SummaryRow
                    icon={<FileText size={16} />}
                    label="Пожелания"
                    value={comment}
                  />
                )}
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      <div className="flex justify-between gap-3">
        {stepIndex > 0 ? (
          <Button variant="ghost" onClick={goBack}>
            Назад
          </Button>
        ) : (
          <div />
        )}

        {step.id !== 'confirm' ? (
          <Button onClick={goNext} disabled={!canProceed()}>
            Далее
          </Button>
        ) : (
          <Button onClick={handleSubmit} isLoading={isSubmitting}>
            Отправить заявку
          </Button>
        )}
      </div>
    </div>
  );
}

function SummaryRow({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
}) {
  return (
    <div className="flex gap-3">
      <span className="mt-0.5 text-primary">{icon}</span>
      <div>
        <span className="text-xs text-warm-gray">{label}</span>
        <p className="text-sm font-medium text-dark">{value}</p>
      </div>
    </div>
  );
}
