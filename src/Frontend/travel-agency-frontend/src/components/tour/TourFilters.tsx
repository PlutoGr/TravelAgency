import { useState, useMemo, useCallback, type ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronDown, X, SlidersHorizontal, RotateCcw } from 'lucide-react';
import clsx from 'clsx';
import type { TourFilters as TourFiltersType } from '@/types';
import { RangeSlider, Button } from '@/components/ui';
import { getDestinations } from '@/api/catalog';
import { TOUR_TYPE_OPTIONS } from '@/utils/tourLabels';

interface TourFiltersProps {
  filters: TourFiltersType;
  onChange: (filters: TourFiltersType) => void;
  onReset: () => void;
}

const PRICE_MAX = 400_000;

function useFilterOptions() {
  const { data: destinations = [] } = useQuery({
    queryKey: ['catalog', 'destinations'],
    queryFn: getDestinations,
  });

  return useMemo(() => {
    const countries = [...new Set(destinations.map((item) => item.country).filter(Boolean))].sort((a, b) =>
      a.localeCompare(b, 'ru'),
    );
    const directions = destinations
      .filter((item) => item.id && item.name)
      .slice()
      .sort((a, b) => a.name.localeCompare(b.name, 'ru'));
    return { countries, directions };
  }, [destinations]);
}

function AccordionSection({
  title,
  defaultOpen = true,
  children,
}: {
  title: string;
  defaultOpen?: boolean;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(defaultOpen);

  return (
    <div className="border-b border-sand last:border-0">
      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        className="flex w-full items-center justify-between py-3 text-left text-sm font-semibold text-dark transition-colors hover:text-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      >
        {title}
        <ChevronDown
          size={16}
          className={clsx('text-warm-gray transition-transform', open && 'rotate-180')}
        />
      </button>
      <AnimatePresence initial={false}>
        {open && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2, ease: 'easeInOut' }}
            className="overflow-hidden"
          >
            <div className="pb-4">{children}</div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

function FilterContent({ filters, onChange, onReset }: TourFiltersProps) {
  const { countries, directions } = useFilterOptions();

  const updateFilter = useCallback(
    <K extends keyof TourFiltersType>(key: K, value: TourFiltersType[K]) => {
      onChange({ ...filters, [key]: value });
    },
    [filters, onChange],
  );

  const hasActiveFilters = Boolean(
    filters.country?.length ||
      filters.priceMin ||
      filters.priceMax ||
      filters.dateFrom ||
      filters.dateTo ||
      filters.category ||
      filters.directionId,
  );

  return (
    <div className="space-y-1">
      <AccordionSection title="Страна">
        <div className="space-y-2">
          {countries.length === 0 && (
            <p className="text-xs text-warm-gray">Страны появятся вместе с направлениями</p>
          )}
          {countries.map((country) => (
            <label
              key={country}
              className="flex cursor-pointer items-center gap-2.5 text-sm text-dark transition-colors hover:text-primary"
            >
              <input
                type="radio"
                name="catalog-country"
                checked={filters.country?.[0] === country}
                onChange={() => updateFilter('country', [country])}
                className="h-4 w-4 border-warm-gray text-primary accent-primary"
              />
              {country}
            </label>
          ))}
        </div>
      </AccordionSection>

      <AccordionSection title="Направление" defaultOpen={false}>
        <div className="space-y-2">
          {directions.map((direction) => (
            <label
              key={direction.id}
              className="flex cursor-pointer items-center gap-2.5 text-sm text-dark transition-colors hover:text-primary"
            >
              <input
                type="radio"
                name="catalog-direction"
                checked={filters.directionId === direction.id}
                onChange={() => updateFilter('directionId', direction.id)}
                className="h-4 w-4 border-warm-gray text-primary accent-primary"
              />
              {direction.name}
            </label>
          ))}
        </div>
      </AccordionSection>

      <AccordionSection title="Цена">
        <RangeSlider
          min={0}
          max={PRICE_MAX}
          value={[filters.priceMin ?? 0, filters.priceMax ?? PRICE_MAX]}
          onChange={([min, max]) => {
            onChange({
              ...filters,
              priceMin: min > 0 ? min : undefined,
              priceMax: max < PRICE_MAX ? max : undefined,
            });
          }}
          step={5000}
          formatLabel={(value) => `${(value / 1000).toFixed(0)}k ₽`}
        />
      </AccordionSection>

      <AccordionSection title="Даты выезда">
        <div className="grid grid-cols-1 gap-3">
          <label className="block text-xs font-medium text-primary">
            С
            <input
              type="date"
              value={filters.dateFrom ?? ''}
              onChange={(event) => updateFilter('dateFrom', event.target.value || undefined)}
              className="mt-1 w-full rounded-[12px] border border-sand bg-white px-3 py-2 text-sm text-dark outline-none focus:border-primary focus:ring-2 focus:ring-primary/10"
            />
          </label>
          <label className="block text-xs font-medium text-primary">
            По
            <input
              type="date"
              value={filters.dateTo ?? ''}
              onChange={(event) => updateFilter('dateTo', event.target.value || undefined)}
              className="mt-1 w-full rounded-[12px] border border-sand bg-white px-3 py-2 text-sm text-dark outline-none focus:border-primary focus:ring-2 focus:ring-primary/10"
            />
          </label>
        </div>
      </AccordionSection>

      <AccordionSection title="Тип тура" defaultOpen={false}>
        <div className="space-y-2">
          {TOUR_TYPE_OPTIONS.map((type) => (
            <label
              key={type.value}
              className="flex cursor-pointer items-center gap-2.5 text-sm text-dark transition-colors hover:text-primary"
            >
              <input
                type="radio"
                name="catalog-tour-type"
                checked={filters.category === type.value}
                onChange={() =>
                  updateFilter('category', filters.category === type.value ? undefined : type.value)
                }
                className="h-4 w-4 border-warm-gray text-primary accent-primary"
              />
              {type.label}
            </label>
          ))}
        </div>
      </AccordionSection>

      {hasActiveFilters && (
        <div className="pt-3">
          <Button variant="ghost" size="sm" fullWidth leftIcon={<RotateCcw size={14} />} onClick={onReset}>
            Сбросить фильтры
          </Button>
        </div>
      )}
    </div>
  );
}

function countActiveFilters(filters: TourFiltersType): number {
  return [
    filters.country?.length,
    filters.priceMin || filters.priceMax,
    filters.dateFrom || filters.dateTo,
    filters.category,
    filters.directionId,
  ].filter(Boolean).length;
}

export default function TourFilters({ filters, onChange, onReset }: TourFiltersProps) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const activeCount = countActiveFilters(filters);

  return (
    <>
      <aside className="hidden lg:block">
        <div className="sticky top-24 rounded-[16px] bg-white p-5 shadow-card">
          <h2 className="mb-4 font-heading text-base font-semibold text-dark">Фильтры</h2>
          <FilterContent filters={filters} onChange={onChange} onReset={onReset} />
        </div>
      </aside>

      <div className="lg:hidden">
        <Button
          variant="secondary"
          size="sm"
          leftIcon={<SlidersHorizontal size={16} />}
          onClick={() => setMobileOpen(true)}
        >
          Фильтры{activeCount > 0 && ` (${activeCount})`}
        </Button>
      </div>

      <AnimatePresence>
        {mobileOpen && (
          <>
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="fixed inset-0 z-40 bg-dark/40 backdrop-blur-sm lg:hidden"
              onClick={() => setMobileOpen(false)}
            />
            <motion.div
              initial={{ x: '-100%' }}
              animate={{ x: 0 }}
              exit={{ x: '-100%' }}
              transition={{ type: 'spring', stiffness: 300, damping: 30 }}
              className="fixed inset-y-0 left-0 z-50 w-[min(100%,20rem)] overflow-y-auto bg-white p-5 shadow-modal lg:hidden"
            >
              <div className="mb-4 flex items-center justify-between">
                <h2 className="font-heading text-base font-semibold text-dark">Фильтры</h2>
                <button
                  type="button"
                  onClick={() => setMobileOpen(false)}
                  aria-label="Закрыть фильтры"
                  className="rounded-full p-1.5 text-warm-gray transition-colors hover:bg-sand hover:text-dark focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                >
                  <X size={20} />
                </button>
              </div>
              <FilterContent
                filters={filters}
                onChange={(next) => {
                  onChange(next);
                }}
                onReset={() => {
                  onReset();
                  setMobileOpen(false);
                }}
              />
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </>
  );
}
