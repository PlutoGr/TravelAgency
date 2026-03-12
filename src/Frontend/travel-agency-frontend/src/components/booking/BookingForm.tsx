import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { MapPin, Users, Calendar, Wallet, FileText, Check } from 'lucide-react';
import toast from 'react-hot-toast';
import { Button, Input, Select } from '@/components/ui';
import { createBooking } from '@/api/bookings';

interface BookingFormProps {
  onSuccess?: () => void;
  onClose?: () => void;
}

const COUNTRIES = [
  { value: 'Турция', label: 'Турция' },
  { value: 'Египет', label: 'Египет' },
  { value: 'Таиланд', label: 'Таиланд' },
  { value: 'Мальдивы', label: 'Мальдивы' },
  { value: 'Италия', label: 'Италия' },
  { value: 'Греция', label: 'Греция' },
  { value: 'Испания', label: 'Испания' },
  { value: 'Индонезия', label: 'Индонезия' },
  { value: 'Шри-Ланка', label: 'Шри-Ланка' },
  { value: 'ОАЭ', label: 'ОАЭ' },
  { value: 'Черногория', label: 'Черногория' },
  { value: 'Грузия', label: 'Грузия' },
];

const STEPS = [
  { id: 1, title: 'Направление' },
  { id: 2, title: 'Бюджет' },
  { id: 3, title: 'Подтверждение' },
];

type FormData = {
  country: string;
  destination: string;
  dateFrom: string;
  dateTo: string;
  travelers: number;
  budget: number;
  notes: string;
};

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

export default function BookingForm({ onSuccess, onClose }: BookingFormProps) {
  const [step, setStep] = useState(1);
  const [direction, setDirection] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [form, setForm] = useState<FormData>({
    country: '',
    destination: '',
    dateFrom: '',
    dateTo: '',
    travelers: 2,
    budget: 100000,
    notes: '',
  });

  function updateField<K extends keyof FormData>(key: K, value: FormData[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function goNext() {
    setDirection(1);
    setStep((s) => Math.min(s + 1, 3));
  }

  function goBack() {
    setDirection(-1);
    setStep((s) => Math.max(s - 1, 1));
  }

  function canProceed(): boolean {
    if (step === 1) {
      return !!form.country && !!form.destination && !!form.dateFrom && !!form.dateTo;
    }
    if (step === 2) {
      return form.budget > 0;
    }
    return true;
  }

  async function handleSubmit() {
    setIsSubmitting(true);
    try {
      await createBooking({
        country: form.country,
        destination: form.destination,
        dateFrom: form.dateFrom,
        dateTo: form.dateTo,
        travelers: form.travelers,
        budget: form.budget,
        notes: form.notes,
      });
      toast.success('Заявка успешно создана!');
      onSuccess?.();
      onClose?.();
    } catch {
      toast.error('Не удалось создать заявку. Попробуйте ещё раз.');
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="space-y-6">
      {/* Progress */}
      <div className="flex items-center justify-between">
        {STEPS.map((s, idx) => (
          <div key={s.id} className="flex items-center">
            <div className="flex items-center gap-2">
              <div
                className={`flex h-8 w-8 items-center justify-center rounded-full text-sm font-semibold transition-colors ${
                  step >= s.id
                    ? 'bg-primary text-white'
                    : 'bg-sand text-warm-gray'
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
              <Select
                label="Страна"
                options={COUNTRIES}
                value={form.country}
                onChange={(v) => updateField('country', v)}
                placeholder="Выберите страну"
              />

              <Input
                label="Город / курорт"
                icon={MapPin}
                value={form.destination}
                onChange={(e) => updateField('destination', e.target.value)}
              />

              <div className="grid grid-cols-2 gap-3">
                <Input
                  label="Дата вылета"
                  icon={Calendar}
                  type="date"
                  value={form.dateFrom}
                  onChange={(e) => updateField('dateFrom', e.target.value)}
                />
                <Input
                  label="Дата возврата"
                  icon={Calendar}
                  type="date"
                  value={form.dateTo}
                  onChange={(e) => updateField('dateTo', e.target.value)}
                />
              </div>

              <Input
                label="Количество туристов"
                icon={Users}
                type="number"
                min={1}
                max={10}
                value={form.travelers}
                onChange={(e) => updateField('travelers', Number(e.target.value))}
              />
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
              <Input
                label="Бюджет (₽)"
                icon={Wallet}
                type="number"
                min={0}
                step={10000}
                value={form.budget}
                onChange={(e) => updateField('budget', Number(e.target.value))}
              />

              <div>
                <label className="mb-1.5 block text-xs font-medium text-primary">
                  Пожелания и комментарии
                </label>
                <textarea
                  value={form.notes}
                  onChange={(e) => updateField('notes', e.target.value)}
                  rows={5}
                  placeholder="Расскажите о ваших пожеланиях..."
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
                <SummaryRow
                  icon={<MapPin size={16} />}
                  label="Направление"
                  value={`${form.destination}, ${form.country}`}
                />
                <SummaryRow
                  icon={<Calendar size={16} />}
                  label="Даты"
                  value={`${form.dateFrom} — ${form.dateTo}`}
                />
                <SummaryRow
                  icon={<Users size={16} />}
                  label="Туристов"
                  value={String(form.travelers)}
                />
                <SummaryRow
                  icon={<Wallet size={16} />}
                  label="Бюджет"
                  value={`${form.budget.toLocaleString('ru-RU')} ₽`}
                />
                {form.notes && (
                  <SummaryRow
                    icon={<FileText size={16} />}
                    label="Пожелания"
                    value={form.notes}
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
