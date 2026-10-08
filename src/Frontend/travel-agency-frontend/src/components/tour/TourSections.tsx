import { Check, X as XIcon, BedDouble, Calendar } from 'lucide-react';
import clsx from 'clsx';
import type { Tour, TourOffer } from '@/types';
import { formatDateRange, formatMoney } from '@/utils/format';

export function TourProgram({ tour }: { tour: Tour }) {
  const days = tour.days ?? [];
  return (
    <section aria-labelledby="tour-program">
      <h2 id="tour-program" className="mb-4 font-heading text-xl font-bold text-dark">
        Программа
      </h2>
      {days.length === 0 ? (
        <p className="text-sm text-warm-gray">Программа появится вместе с опубликованным туром.</p>
      ) : (
        <ol className="space-y-5">
          {days.map((day) => (
            <li key={day.dayNumber} className="grid grid-cols-[2.5rem_minmax(0,1fr)] gap-3">
              <span className="font-heading text-sm font-bold text-primary">
                {String(day.dayNumber).padStart(2, '0')}
              </span>
              <div className="min-w-0">
                <h3 className="font-heading text-base font-semibold text-dark">{day.title}</h3>
                <p className="mt-1 text-sm leading-relaxed text-dark/75">{day.description}</p>
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

export function TourInclusions({ tour }: { tour: Tour }) {
  return (
    <section aria-labelledby="tour-inclusions" className="grid gap-6 sm:grid-cols-2">
      <div>
        <h2 id="tour-inclusions" className="mb-3 flex items-center gap-2 font-heading text-base font-semibold text-dark">
          <Check size={16} className="text-olive" />
          Включено
        </h2>
        <InclusionList items={tour.included} empty="Состав тура уточняется" included />
      </div>
      <div>
        <h2 className="mb-3 flex items-center gap-2 font-heading text-base font-semibold text-dark">
          <XIcon size={16} className="text-terracotta" />
          Не включено
        </h2>
        <InclusionList items={tour.notIncluded} empty="Доплаты не указаны" included={false} />
      </div>
    </section>
  );
}

function InclusionList({
  items,
  empty,
  included,
}: {
  items: string[];
  empty: string;
  included: boolean;
}) {
  if (items.length === 0) {
    return <p className="text-sm text-warm-gray">{empty}</p>;
  }
  return (
    <ul className="space-y-2">
      {items.map((item) => (
        <li key={item} className="flex items-start gap-2 text-sm text-dark/80">
          {included ? (
            <Check size={14} className="mt-0.5 shrink-0 text-olive" />
          ) : (
            <XIcon size={14} className="mt-0.5 shrink-0 text-terracotta" />
          )}
          <span>{item}</span>
        </li>
      ))}
    </ul>
  );
}

export function TourAccommodation({ tour }: { tour: Tour }) {
  return (
    <section aria-labelledby="tour-stay">
      <h2 id="tour-stay" className="mb-3 flex items-center gap-2 font-heading text-xl font-bold text-dark">
        <BedDouble size={18} className="text-primary" />
        Проживание
      </h2>
      <p className="whitespace-pre-line text-sm leading-relaxed text-dark/80">
        {tour.accommodation || 'Проживание уточняется у менеджера.'}
      </p>
      {tour.mealPlan && (
        <p className="mt-3 text-sm text-dark">
          <span className="text-warm-gray">Питание: </span>
          {tour.mealPlan}
        </p>
      )}
    </section>
  );
}

export function TourOffers({
  tour,
  selectedId,
  onSelect,
}: {
  tour: Tour;
  selectedId: string | null;
  onSelect: (offer: TourOffer) => void;
}) {
  const offers = tour.offers ?? [];
  return (
    <section aria-labelledby="tour-offers">
      <h2 id="tour-offers" className="mb-2 font-heading text-xl font-bold text-dark">
        Предложения
      </h2>
      <p className="mb-4 text-sm text-warm-gray">Даты и цена за человека. Выберите выезд для заявки.</p>
      {offers.length === 0 ? (
        <p className="text-sm text-warm-gray">Ближайших дат пока нет.</p>
      ) : (
        <ul className="space-y-3">
          {offers.map((offer) => {
            const selected = offer.id === selectedId;
            return (
              <li key={offer.id}>
                <button
                  type="button"
                  aria-pressed={selected}
                  onClick={() => onSelect(offer)}
                  className={clsx(
                    'flex w-full flex-col gap-2 rounded-[16px] border px-4 py-3 text-left transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary sm:flex-row sm:items-center sm:justify-between',
                    selected
                      ? 'border-primary bg-primary/5'
                      : 'border-sand bg-white hover:border-primary/40',
                  )}
                >
                  <span className="flex items-start gap-2 text-sm text-dark">
                    <Calendar size={16} className="mt-0.5 shrink-0 text-primary" />
                    <span>
                      {formatDateRange(offer.start, offer.end)}
                      <span className="mt-0.5 block text-xs text-warm-gray">
                        {tour.duration} дн.
                        {offer.seats > 0 ? ` · ${offer.seats} мест` : ''}
                      </span>
                    </span>
                  </span>
                  <span className="font-heading text-lg font-bold text-primary">
                    {formatMoney(offer.price, offer.currency)}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
