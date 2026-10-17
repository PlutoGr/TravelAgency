import { useState, useMemo, useCallback } from 'react';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronRight, X as XIcon, Loader2 } from 'lucide-react';
import { Link, useSearchParams } from 'react-router-dom';
import type { Tour, TourFilters as TourFiltersType } from '@/types';
import { PageTransition } from '@/components/common';
import { Select, Button } from '@/components/ui';
import { TourGrid, TourFilters } from '@/components/tour';
import { getDestinations, getTours } from '@/api/catalog';
import { tourTypeLabel } from '@/utils/tourLabels';
import {
  applyCatalogFilters,
  DEPARTURE_RANGE_HINT,
  filtersFromSearchParams,
  filtersToSearchParams,
  formatCatalogDateChip,
  hasCatalogQuery,
  isDepartureRangeInverted,
} from '@/utils/catalogQuery';

const sortOptions = [
  { value: 'date', label: 'Сначала новые' },
  { value: 'price_asc', label: 'Сначала дешевле' },
  { value: 'price_desc', label: 'Сначала дороже' },
  { value: 'title', label: 'По названию' },
  { value: 'duration_asc', label: 'Короче по сроку' },
  { value: 'duration_desc', label: 'Длиннее по сроку' },
];

function filterLabel(key: string, val: unknown): string {
  const labels: Record<string, string> = {
    country: String(val),
    priceMin: `от ${Number(val).toLocaleString('ru-RU')} ₽`,
    priceMax: `до ${Number(val).toLocaleString('ru-RU')} ₽`,
    category: String(val),
    dateFrom: formatCatalogDateChip('с', String(val)),
    dateTo: formatCatalogDateChip('по', String(val)),
  };
  return labels[key] ?? String(val);
}

function getActiveFilterPills(filters: TourFiltersType, directionLabel?: string) {
  const pills: { key: string; label: string; remove: (f: TourFiltersType) => TourFiltersType }[] = [];

  filters.country?.forEach((c) => {
    pills.push({
      key: `country-${c}`,
      label: c,
      remove: (f) => ({
        ...f,
        country: f.country?.filter((x) => x !== c),
      }),
    });
  });

  if (filters.priceMin) {
    pills.push({
      key: 'priceMin',
      label: filterLabel('priceMin', filters.priceMin),
      remove: (f) => ({ ...f, priceMin: undefined }),
    });
  }

  if (filters.priceMax) {
    pills.push({
      key: 'priceMax',
      label: filterLabel('priceMax', filters.priceMax),
      remove: (f) => ({ ...f, priceMax: undefined }),
    });
  }

  if (filters.dateFrom) {
    pills.push({
      key: 'dateFrom',
      label: filterLabel('dateFrom', filters.dateFrom),
      remove: (f) => ({ ...f, dateFrom: undefined }),
    });
  }

  if (filters.dateTo) {
    pills.push({
      key: 'dateTo',
      label: filterLabel('dateTo', filters.dateTo),
      remove: (f) => ({ ...f, dateTo: undefined }),
    });
  }

  if (filters.directionId) {
    pills.push({
      key: 'direction',
      label: directionLabel || 'Направление',
      remove: (f) => ({ ...f, directionId: undefined }),
    });
  }

  if (filters.category) {
    pills.push({
      key: 'category',
      label: tourTypeLabel(filters.category),
      remove: (f) => ({ ...f, category: undefined }),
    });
  }

  return pills;
}

export default function TourCatalogPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const search = searchParams.toString();
  const filters = useMemo(
    () => filtersFromSearchParams(new URLSearchParams(search)),
    [search],
  );
  const filterKey = filtersToSearchParams(filters).toString();

  const [page, setPage] = useState(1);
  const [loadedPages, setLoadedPages] = useState<Tour[][]>([]);
  const [appliedFilterKey, setAppliedFilterKey] = useState(filterKey);
  const filtersChanged = appliedFilterKey !== filterKey;
  if (filtersChanged) {
    setAppliedFilterKey(filterKey);
    setPage(1);
    setLoadedPages([]);
  }
  const currentPage = filtersChanged ? 1 : page;
  const rangeInverted = isDepartureRangeInverted(filters.dateFrom, filters.dateTo);

  const { data: destinations = [] } = useQuery({
    queryKey: ['catalog', 'destinations'],
    queryFn: getDestinations,
  });

  const { data, isLoading, isFetching, isError, refetch } = useQuery({
    queryKey: ['tours', filters, currentPage],
    queryFn: () => getTours(filters, currentPage),
    enabled: !rangeInverted,
    placeholderData: rangeInverted ? undefined : keepPreviousData,
  });

  const allTours = useMemo(() => {
    if (!data) return [];
    return data.items;
  }, [data]);

  const displayedTours = useMemo(() => {
    if (currentPage === 1) return allTours;
    return [...loadedPages.flat(), ...allTours];
  }, [allTours, loadedPages, currentPage]);

  const handleLoadMore = useCallback(() => {
    setLoadedPages((prev) => [...prev, allTours]);
    setPage((p) => p + 1);
  }, [allTours]);

  const handleFilterChange = useCallback(
    (newFilters: TourFiltersType) => {
      setSearchParams((current) => applyCatalogFilters(current, newFilters), { replace: true });
    },
    [setSearchParams],
  );

  const handleSortChange = useCallback(
    (val: string) => {
      handleFilterChange({
        ...filters,
        sortBy: val as TourFiltersType['sortBy'],
      });
    },
    [filters, handleFilterChange],
  );

  const handleReset = useCallback(() => {
    setSearchParams((current) => applyCatalogFilters(current, {}), { replace: true });
  }, [setSearchParams]);

  const handleRemovePill = useCallback(
    (removeFn: (f: TourFiltersType) => TourFiltersType) => {
      handleFilterChange(removeFn(filters));
    },
    [filters, handleFilterChange],
  );

  const directionLabel = destinations.find((item) => item.id === filters.directionId)?.name;
  const pills = useMemo(
    () => getActiveFilterPills(filters, directionLabel),
    [filters, directionLabel],
  );
  const hasMore = data ? currentPage < data.totalPages : false;

  return (
    <PageTransition>
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        {/* Breadcrumbs */}
        <nav className="mb-6 flex items-center gap-2 text-sm text-warm-gray">
          <Link to="/" className="transition-colors hover:text-primary">
            Главная
          </Link>
          <ChevronRight size={14} />
          <span className="text-dark">Каталог туров</span>
        </nav>

        {/* Header */}
        <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">
              Каталог туров
            </h1>
            {data && (
              <p className="mt-1 text-sm text-warm-gray">
                Найдено {data.total}{' '}
                {data.total === 1 ? 'тур' : data.total < 5 ? 'тура' : 'туров'}
              </p>
            )}
          </div>

          <div className="flex w-full flex-col gap-3 sm:w-auto sm:flex-row sm:items-center">
            <div className="lg:hidden">
              <TourFilters
                filters={filters}
                onChange={handleFilterChange}
                onReset={handleReset}
              />
            </div>
            <div className="w-full sm:w-52">
              <Select
                options={sortOptions}
                value={filters.sortBy ?? 'date'}
                onChange={handleSortChange}
                placeholder="Сортировка"
              />
            </div>
          </div>
        </div>

        {/* Active Filter Pills */}
        <AnimatePresence>
          {pills.length > 0 && (
            <motion.div
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: 'auto' }}
              exit={{ opacity: 0, height: 0 }}
              className="mb-6 flex flex-wrap gap-2"
            >
              {pills.map((pill) => (
                <motion.button
                  key={pill.key}
                  initial={{ opacity: 0, scale: 0.9 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.9 }}
                  onClick={() => handleRemovePill(pill.remove)}
                  className="flex items-center gap-1.5 rounded-full border border-primary/20 bg-primary/5 px-3 py-1 text-xs font-medium text-primary transition-colors hover:bg-primary/10"
                >
                  {pill.label}
                  <XIcon size={12} />
                </motion.button>
              ))}
              <button
                onClick={handleReset}
                className="text-xs text-warm-gray transition-colors hover:text-terracotta"
              >
                Сбросить все
              </button>
            </motion.div>
          )}
        </AnimatePresence>

        {/* Main Content */}
        <div className="flex gap-8">
          {/* Desktop Sidebar */}
          <div className="hidden w-[280px] flex-shrink-0 lg:block">
            <TourFilters
              filters={filters}
              onChange={handleFilterChange}
              onReset={handleReset}
            />
          </div>

          {/* Tour Grid */}
          <div className="min-w-0 flex-1">
            {isError && !rangeInverted && (
              <div role="alert" className="mb-6 rounded-[16px] border border-terracotta/30 bg-terracotta/5 px-4 py-3 text-sm text-dark">
                <p>Не удалось загрузить каталог</p>
                <div className="mt-2 flex flex-wrap gap-4">
                  <button
                    type="button"
                    onClick={() => refetch()}
                    className="font-medium text-primary underline-offset-2 hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                  >
                    Повторить
                  </button>
                  <button
                    type="button"
                    onClick={handleReset}
                    className="font-medium text-primary underline-offset-2 hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                  >
                    Сбросить фильтры
                  </button>
                </div>
              </div>
            )}
            {rangeInverted && (
              <p className="mb-6 text-sm text-terracotta">{DEPARTURE_RANGE_HINT}</p>
            )}
            {!rangeInverted && (data || isLoading) && (
              <TourGrid
                tours={displayedTours}
                isLoading={isLoading && !data}
                emptyVariant={hasCatalogQuery(filters) ? 'filtered' : 'default'}
                onResetFilters={handleReset}
              />
            )}

            {/* Load More */}
            {hasMore && !isLoading && (
              <div className="mt-10 text-center">
                <Button
                  variant="secondary"
                  size="lg"
                  onClick={handleLoadMore}
                  isLoading={isFetching}
                  leftIcon={isFetching ? undefined : <Loader2 size={18} />}
                >
                  Показать ещё
                </Button>
              </div>
            )}
          </div>
        </div>
      </div>
    </PageTransition>
  );
}
