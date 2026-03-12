import { useState, useMemo, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ChevronDown,
  X,
  SlidersHorizontal,
  Search,
  RotateCcw,
} from 'lucide-react';
import clsx from 'clsx';
import type { TourFilters as TourFiltersType } from '@/types';
import { RangeSlider, StarRating, Button } from '@/components/ui';
import { mockTours } from '@/mocks/tours';

interface TourFiltersProps {
  filters: TourFiltersType;
  onChange: (filters: TourFiltersType) => void;
  onReset: () => void;
}

const categories = [
  'Пляжный отдых',
  'Экскурсионный',
  'Экзотика',
  'Люкс',
  'Романтический',
  'Гастрономический',
  'Активный',
];

function useUniqueValues() {
  return useMemo(() => {
    const countries = [...new Set(mockTours.map((t) => t.country))].sort();
    const amenities = [...new Set(mockTours.flatMap((t) => t.amenities))].sort();
    return { countries, amenities };
  }, []);
}

interface AccordionSectionProps {
  title: string;
  defaultOpen?: boolean;
  children: React.ReactNode;
}

function AccordionSection({ title, defaultOpen = true, children }: AccordionSectionProps) {
  const [open, setOpen] = useState(defaultOpen);

  return (
    <div className="border-b border-sand last:border-0">
      <button
        type="button"
        onClick={() => setOpen((p) => !p)}
        className="flex w-full items-center justify-between py-3 text-left text-sm font-semibold text-dark transition-colors hover:text-primary"
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
  const { countries, amenities } = useUniqueValues();

  const updateFilter = useCallback(
    <K extends keyof TourFiltersType>(key: K, value: TourFiltersType[K]) => {
      onChange({ ...filters, [key]: value });
    },
    [filters, onChange],
  );

  const toggleCountry = useCallback(
    (country: string) => {
      const current = filters.country ?? [];
      const next = current.includes(country)
        ? current.filter((c) => c !== country)
        : [...current, country];
      updateFilter('country', next.length > 0 ? next : undefined);
    },
    [filters.country, updateFilter],
  );

  const toggleAmenity = useCallback(
    (amenity: string) => {
      const current = filters.amenities ?? [];
      const next = current.includes(amenity)
        ? current.filter((a) => a !== amenity)
        : [...current, amenity];
      updateFilter('amenities', next.length > 0 ? next : undefined);
    },
    [filters.amenities, updateFilter],
  );

  const hasActiveFilters = Boolean(
    filters.search ||
    filters.country?.length ||
    filters.priceMin ||
    filters.priceMax ||
    filters.rating ||
    filters.amenities?.length ||
    filters.category,
  );

  return (
    <div className="space-y-1">
      {/* Search */}
      <div className="relative pb-3">
        <Search size={16} className="absolute left-3 top-3 text-warm-gray" />
        <input
          type="text"
          placeholder="Поиск туров..."
          value={filters.search ?? ''}
          onChange={(e) => updateFilter('search', e.target.value || undefined)}
          className="w-full rounded-[12px] border border-sand bg-white py-2.5 pl-9 pr-4 text-sm text-dark outline-none transition-all placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
        />
      </div>

      {/* Country */}
      <AccordionSection title="Страна">
        <div className="space-y-2">
          {countries.map((country) => (
            <label
              key={country}
              className="flex cursor-pointer items-center gap-2.5 text-sm text-dark transition-colors hover:text-primary"
            >
              <input
                type="checkbox"
                checked={filters.country?.includes(country) ?? false}
                onChange={() => toggleCountry(country)}
                className="h-4 w-4 rounded border-warm-gray text-primary accent-primary"
              />
              {country}
            </label>
          ))}
        </div>
      </AccordionSection>

      {/* Price */}
      <AccordionSection title="Цена">
        <RangeSlider
          min={0}
          max={300000}
          value={[filters.priceMin ?? 0, filters.priceMax ?? 300000]}
          onChange={([min, max]) => {
            onChange({
              ...filters,
              priceMin: min > 0 ? min : undefined,
              priceMax: max < 300000 ? max : undefined,
            });
          }}
          step={5000}
          formatLabel={(v) => `${(v / 1000).toFixed(0)}k ₽`}
        />
      </AccordionSection>

      {/* Rating */}
      <AccordionSection title="Рейтинг">
        <div className="space-y-2">
          {[4, 3, 2].map((r) => (
            <button
              key={r}
              type="button"
              onClick={() => updateFilter('rating', filters.rating === r ? undefined : r)}
              className={clsx(
                'flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-sm transition-colors',
                filters.rating === r
                  ? 'bg-primary/5 text-primary'
                  : 'text-dark hover:bg-sand',
              )}
            >
              <StarRating rating={r} size="sm" />
              <span>от {r}+</span>
            </button>
          ))}
        </div>
      </AccordionSection>

      {/* Amenities */}
      <AccordionSection title="Удобства" defaultOpen={false}>
        <div className="flex flex-wrap gap-2">
          {amenities.map((amenity) => (
            <button
              key={amenity}
              type="button"
              onClick={() => toggleAmenity(amenity)}
              className={clsx(
                'rounded-full border px-3 py-1 text-xs font-medium transition-all',
                filters.amenities?.includes(amenity)
                  ? 'border-primary bg-primary/5 text-primary'
                  : 'border-sand bg-white text-dark hover:border-warm-gray',
              )}
            >
              {amenity}
            </button>
          ))}
        </div>
      </AccordionSection>

      {/* Category */}
      <AccordionSection title="Категория" defaultOpen={false}>
        <div className="space-y-2">
          {categories.map((cat) => (
            <label
              key={cat}
              className="flex cursor-pointer items-center gap-2.5 text-sm text-dark transition-colors hover:text-primary"
            >
              <input
                type="radio"
                name="category"
                checked={filters.category === cat}
                onChange={() =>
                  updateFilter('category', filters.category === cat ? undefined : cat)
                }
                className="h-4 w-4 border-warm-gray text-primary accent-primary"
              />
              {cat}
            </label>
          ))}
        </div>
      </AccordionSection>

      {/* Reset */}
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

export default function TourFilters({ filters, onChange, onReset }: TourFiltersProps) {
  const [mobileOpen, setMobileOpen] = useState(false);

  const activeCount = [
    filters.search,
    filters.country?.length,
    filters.priceMin || filters.priceMax,
    filters.rating,
    filters.amenities?.length,
    filters.category,
  ].filter(Boolean).length;

  return (
    <>
      {/* Desktop sidebar */}
      <aside className="hidden lg:block">
        <div className="sticky top-24 rounded-[16px] bg-white p-5 shadow-card">
          <h3 className="mb-4 font-heading text-base font-semibold text-dark">Фильтры</h3>
          <FilterContent filters={filters} onChange={onChange} onReset={onReset} />
        </div>
      </aside>

      {/* Mobile trigger */}
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

      {/* Mobile drawer */}
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
              className="fixed inset-y-0 left-0 z-50 w-[320px] max-w-[85vw] overflow-y-auto bg-white p-5 shadow-modal lg:hidden"
            >
              <div className="mb-4 flex items-center justify-between">
                <h3 className="font-heading text-base font-semibold text-dark">Фильтры</h3>
                <button
                  onClick={() => setMobileOpen(false)}
                  className="rounded-full p-1.5 text-warm-gray transition-colors hover:bg-sand hover:text-dark"
                >
                  <X size={20} />
                </button>
              </div>
              <FilterContent filters={filters} onChange={onChange} onReset={onReset} />
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </>
  );
}
