import { useState, useMemo } from 'react';
import { motion } from 'framer-motion';
import {
  Check,
  X as XIcon,
  Wifi,
  Waves,
  Sparkles,
  Dumbbell,
  UtensilsCrossed,
  Wine,
  Eye,
  Baby,
  Anchor,
  Palmtree,
  Mountain,
  Plane,
  ShieldCheck,
  Calendar,
} from 'lucide-react';
import clsx from 'clsx';
import { format, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';
import type { Tour, Review } from '@/types';
import { Tabs, Avatar, StarRating } from '@/components/ui';
import { mockReviews } from '@/mocks/reviews';

interface TourTabsProps {
  tour: Tour;
}

const amenityIcons: Record<string, typeof Wifi> = {
  'Wi-Fi': Wifi,
  'Бассейн': Waves,
  'Инфинити-бассейн': Waves,
  'Приватный бассейн': Waves,
  'Спа': Sparkles,
  'Фитнес': Dumbbell,
  'Ресторан': UtensilsCrossed,
  'Рестораны': UtensilsCrossed,
  'Бар': Wine,
  'Бар на крыше': Wine,
  'Терраса': Eye,
  'Терраса с видом': Eye,
  'Вид на море': Eye,
  'Вид на долину': Eye,
  'Детский клуб': Baby,
  'Детская анимация': Baby,
  'Дайвинг-центр': Anchor,
  'Сноркелинг': Anchor,
  'Приватный пляж': Palmtree,
  'Аквапарк': Waves,
  'Консьерж': ShieldCheck,
  'Экскурсионное бюро': Mountain,
  'Йога-зал': Sparkles,
  'Батлер-сервис': ShieldCheck,
};

const tabItems = [
  { id: 'description', label: 'Описание' },
  { id: 'amenities', label: 'Удобства' },
  { id: 'reviews', label: 'Отзывы' },
  { id: 'dates', label: 'Цены и даты' },
];

function DescriptionPanel({ tour }: { tour: Tour }) {
  return (
    <div className="space-y-6">
      <p className="leading-relaxed text-dark/80">{tour.description}</p>

      <div className="grid gap-6 sm:grid-cols-2">
        <div>
          <h4 className="mb-3 flex items-center gap-2 font-heading text-sm font-semibold text-dark">
            <Check size={16} className="text-olive" />
            Включено в стоимость
          </h4>
          <ul className="space-y-2">
            {tour.included.map((item) => (
              <li key={item} className="flex items-start gap-2 text-sm text-dark/70">
                <Check size={14} className="mt-0.5 flex-shrink-0 text-olive" />
                {item}
              </li>
            ))}
          </ul>
        </div>

        <div>
          <h4 className="mb-3 flex items-center gap-2 font-heading text-sm font-semibold text-dark">
            <XIcon size={16} className="text-terracotta" />
            Не включено
          </h4>
          <ul className="space-y-2">
            {tour.notIncluded.map((item) => (
              <li key={item} className="flex items-start gap-2 text-sm text-dark/70">
                <XIcon size={14} className="mt-0.5 flex-shrink-0 text-terracotta" />
                {item}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}

function AmenitiesPanel({ tour }: { tour: Tour }) {
  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
      {tour.amenities.map((amenity) => {
        const Icon = amenityIcons[amenity] ?? Sparkles;
        return (
          <motion.div
            key={amenity}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            className="flex flex-col items-center gap-2 rounded-[12px] bg-sand/50 p-4 text-center"
          >
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10">
              <Icon size={20} className="text-primary" />
            </div>
            <span className="text-xs font-medium text-dark">{amenity}</span>
          </motion.div>
        );
      })}
    </div>
  );
}

function ReviewsPanel({ tour }: { tour: Tour }) {
  const reviews = useMemo(
    () => mockReviews.filter((r) => r.tourId === tour.id),
    [tour.id],
  );

  if (reviews.length === 0) {
    return (
      <div className="py-8 text-center text-sm text-warm-gray">
        Пока нет отзывов для этого тура
      </div>
    );
  }

  return (
    <div className="space-y-5">
      {reviews.map((review) => (
        <ReviewCard key={review.id} review={review} />
      ))}
    </div>
  );
}

function ReviewCard({ review }: { review: Review }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      className="rounded-[16px] border border-sand bg-white p-5"
    >
      <div className="mb-3 flex items-start justify-between">
        <div className="flex items-center gap-3">
          <Avatar src={review.userAvatar} name={review.userName} size="md" />
          <div>
            <p className="text-sm font-semibold text-dark">{review.userName}</p>
            <p className="text-xs text-warm-gray">
              {format(parseISO(review.date), 'd MMMM yyyy', { locale: ru })}
            </p>
          </div>
        </div>
        <StarRating rating={review.rating} size="sm" />
      </div>
      <p className="text-sm leading-relaxed text-dark/70">{review.text}</p>
    </motion.div>
  );
}

function DatesPanel({
  tour,
  selectedDate,
  onSelectDate,
}: {
  tour: Tour;
  selectedDate: number | null;
  onSelectDate: (index: number) => void;
}) {
  return (
    <div className="space-y-3">
      <p className="text-sm text-warm-gray">
        Выберите удобные даты для вашего путешествия
      </p>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-sand text-left">
              <th className="pb-3 font-medium text-warm-gray">Даты</th>
              <th className="pb-3 font-medium text-warm-gray">Длительность</th>
              <th className="pb-3 text-right font-medium text-warm-gray">Цена</th>
              <th className="pb-3 text-right font-medium text-warm-gray" />
            </tr>
          </thead>
          <tbody>
            {tour.dates.map((d, i) => (
              <tr
                key={i}
                className={clsx(
                  'cursor-pointer border-b border-sand/50 transition-colors',
                  selectedDate === i ? 'bg-primary/5' : 'hover:bg-sand/50',
                )}
                onClick={() => onSelectDate(i)}
              >
                <td className="py-3">
                  <div className="flex items-center gap-2">
                    <Calendar size={14} className="text-primary" />
                    <span>
                      {format(parseISO(d.start), 'd MMM', { locale: ru })} —{' '}
                      {format(parseISO(d.end), 'd MMM yyyy', { locale: ru })}
                    </span>
                  </div>
                </td>
                <td className="py-3 text-warm-gray">{tour.duration} дней</td>
                <td className="py-3 text-right font-semibold text-primary">
                  {tour.price.toLocaleString('ru-RU')} ₽
                </td>
                <td className="py-3 text-right">
                  <span
                    className={clsx(
                      'inline-flex h-5 w-5 items-center justify-center rounded-full border-2 transition-colors',
                      selectedDate === i
                        ? 'border-primary bg-primary text-white'
                        : 'border-warm-gray',
                    )}
                  >
                    {selectedDate === i && <Check size={12} />}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

export default function TourTabs({ tour }: TourTabsProps) {
  const [activeTab, setActiveTab] = useState('description');
  const [selectedDate, setSelectedDate] = useState<number | null>(null);

  return (
    <div>
      <Tabs tabs={tabItems} activeTab={activeTab} onChange={setActiveTab} />
      <div className="pt-6">
        {activeTab === 'description' && <DescriptionPanel tour={tour} />}
        {activeTab === 'amenities' && <AmenitiesPanel tour={tour} />}
        {activeTab === 'reviews' && <ReviewsPanel tour={tour} />}
        {activeTab === 'dates' && (
          <DatesPanel
            tour={tour}
            selectedDate={selectedDate}
            onSelectDate={setSelectedDate}
          />
        )}
      </div>
    </div>
  );
}
