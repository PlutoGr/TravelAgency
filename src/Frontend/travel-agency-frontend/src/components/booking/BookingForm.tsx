import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { FileText, Check, MapPin, Wallet } from 'lucide-react';
import toast from 'react-hot-toast';
import type { Tour } from '@/types';
import { Button } from '@/components/ui';
import { createBooking } from '@/api/bookings';
import { getTours, getTourById } from '@/api/catalog';
import { mediaImageUrl } from '@/utils/media';

interface BookingFormProps {
  /** Pre-selected tour (e.g. from tour detail page) */
  tourId?: string;
  /** Tours to choose from; when absent, fetches from catalog */
  tours?: Tour[];
  /** Даты выбранного предложения, чтобы менеджер увидел их в комментарии. */
  initialComment?: string;
  onSuccess?: () => void;
  onClose?: () => void;
}

const STEPS = [
  { id: 1, title: 'Выбор тура' },
  { id: 2, title: 'Комментарий' },
  { id: 3, title: 'Подтверждение' },
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

export default function BookingForm({
  tourId: initialTourId,
  tours: initialTours,
  initialComment = '',
  onSuccess,
  onClose,
}: BookingFormProps) {
  const [step, setStep] = useState(1);
  const [direction, setDirection] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [tours, setTours] = useState<Tour[]>(initialTours ?? []);
  const [selectedTour, setSelectedTour] = useState<Tour | null>(
    initialTourId && initialTours?.length
      ? initialTours.find((t) => t.id === initialTourId) ?? null
      : null,
  );
  const [comment, setComment] = useState(initialComment);
  const [isLoadingTours, setIsLoadingTours] = useState(!initialTours?.length);

  useEffect(() => {
    if (initialTours?.length) {
      setTours(initialTours);
      if (initialTourId) {
        const t = initialTours.find((t) => t.id === initialTourId);
        if (t) setSelectedTour(t);
      }
      setIsLoadingTours(false);
      return;
    }
    if (initialTourId) {
      getTourById(initialTourId)
        .then((t) => {
          setTours([t]);
          setSelectedTour(t);
        })
        .catch(() => {
          toast.error('Не удалось загрузить тур');
        })
        .finally(() => setIsLoadingTours(false));
      return;
    }
    getTours()
      .then((res) => setTours(res.items))
      .catch(() => {
        toast.error('Не удалось загрузить список туров');
      })
      .finally(() => setIsLoadingTours(false));
  }, [initialTourId, initialTours]);

  function goNext() {
    setDirection(1);
    setStep((s) => Math.min(s + 1, 3));
  }

  function goBack() {
    setDirection(-1);
    setStep((s) => Math.max(s - 1, 1));
  }

  function canProceed(): boolean {
    if (step === 1) return !!selectedTour || !!initialTourId;
    if (step === 2) return true;
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
      await createBooking({ tourId: tid, comment: comment || undefined });
      toast.success('Заявка успешно создана!');
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

  return (
    <div className="space-y-6">
      {/* Progress */}
      <div className="flex items-center justify-between">
        {STEPS.map((s, idx) => (
          <div key={s.id} className="flex items-center">
            <div className="flex items-center gap-2">
              <div
                className={`flex h-8 w-8 items-center justify-center rounded-full text-sm font-semibold transition-colors ${
                  step >= s.id ? 'bg-primary text-white' : 'bg-sand text-warm-gray'
                }`}
              >
                {step > s.id ? <Check size={16} /> : s.id}
              </div>
              <span
                className={`hidden text-sm font-medium sm:block ${
                  step >= s.id ? 'text-dark' : 'text-warm-gray'
                }`}
              >
                {s.title}
              </span>
            </div>
            {idx < STEPS.length - 1 && (
              <div
                className={`mx-3 h-0.5 w-8 rounded sm:w-12 ${
                  step > s.id ? 'bg-primary' : 'bg-sand'
                }`}
              />
            )}
          </div>
        ))}
      </div>

      {/* Steps */}
      <div className="relative min-h-[280px] overflow-hidden">
        <AnimatePresence mode="wait" custom={direction}>
          {step === 1 && (
            <motion.div
              key="step-1"
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
                  {tours.map((tour) => (
                    <button
                      key={tour.id}
                      type="button"
                      onClick={() => setSelectedTour(tour)}
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
                        <p className="mt-0.5 text-sm font-semibold text-primary">
                          {tour.price.toLocaleString('ru-RU')} ₽
                        </p>
                      </div>
                    </button>
                  ))}
                </div>
              )}
            </motion.div>
          )}

          {step === 2 && (
            <motion.div
              key="step-2"
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
                <p className="mb-2 text-xs text-warm-gray">
                  Если важны даты поездки, напишите их здесь. Отдельных полей дат пока нет.
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

          {step === 3 && (
            <motion.div
              key="step-3"
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
                  <>
                    <SummaryRow
                      icon={<MapPin size={16} />}
                      label="Тур"
                      value={`${effectiveTour.title}, ${effectiveTour.country}`}
                    />
                    <SummaryRow
                      icon={<Wallet size={16} />}
                      label="Цена"
                      value={`${effectiveTour.price.toLocaleString('ru-RU')} ₽`}
                    />
                  </>
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

      {/* Navigation */}
      <div className="flex justify-between gap-3">
        {step > 1 ? (
          <Button variant="ghost" onClick={goBack}>
            Назад
          </Button>
        ) : (
          <div />
        )}

        {step < 3 ? (
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
