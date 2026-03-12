import { useState, useMemo, useCallback } from 'react';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronRight, X as XIcon, Loader2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { TourFilters as TourFiltersType } from '@/types';
import { PageTransition } from '@/components/common';
import { Select, Button } from '@/components/ui';
import { TourGrid, TourFilters } from '@/components/tour';
import { getTours } from '@/api/catalog';

const INITIAL_FILTERS: TourFiltersType = {};

const sortOptions = [
  { value: 'popularity', label: 'По популярности' },
  { value: 'price_asc', label: 'Сначала дешевле' },
  { value: 'price_desc', label: 'Сначала дороже' },
  { value: 'rating', label: 'По рейтингу' },
  { value: 'date', label: 'По дате' },
];

function filterLabel(key: string, val: unknown): string {
  const labels: Record<string, string> = {
    search: `«${val}»`,
    country: String(val),
    priceMin: `от ${Number(val).toLocaleString('ru-RU')} ₽`,
    priceMax: `до ${Number(val).toLocaleString('ru-RU')} ₽`,
    rating: `★ ${val}+`,
    amenities: String(val),
    category: String(val),
  };
  return labels[key] ?? String(val);
}

function getActiveFilterPills(filters: TourFiltersType) {
  const pills: { key: string; label: string; remove: (f: TourFiltersType) => TourFiltersType }[] = [];

  if (filters.search) {
    pills.push({
      key: 'search',
      label: filterLabel('search', filters.search),
      remove: (f) => ({ ...f, search: undefined }),
    });
  }

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

  if (filters.rating) {
    pills.push({
      key: 'rating',
      label: filterLabel('rating', filters.rating),
      remove: (f) => ({ ...f, rating: undefined }),
    });
  }

  filters.amenities?.forEach((a) => {
    pills.push({
      key: `amenity-${a}`,
      label: a,
      remove: (f) => ({
        ...f,
        amenities: f.amenities?.filter((x) => x !== a),
      }),
    });
  });

  if (filters.category) {
    pills.push({
      key: 'category',
      label: filters.category,
      remove: (f) => ({ ...f, category: undefined }),
    });
  }

  return pills;
}

export default function TourCatalogPage() {
  const [filters, setFilters] = useState<TourFiltersType>(INITIAL_FILTERS);
  const [page, setPage] = useState(1);

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['tours', filters, page],
    queryFn: () => getTours(filters, page),
    placeholderData: keepPreviousData,
  });

  const allTours = useMemo(() => {
    if (!data) return [];
    return data.items;
  }, [data]);

  const [loadedPages, setLoadedPages] = useState<typeof allTours[]>([]);

  const displayedTours = useMemo(() => {
    if (page === 1) return allTours;
    return [...loadedPages.flat(), ...allTours];
  }, [allTours, loadedPages, page]);

  const handleLoadMore = useCallback(() => {
    setLoadedPages((prev) => [...prev, allTours]);
    setPage((p) => p + 1);
  }, [allTours]);

  const handleFilterChange = useCallback((newFilters: TourFiltersType) => {
    setFilters(newFilters);
    setPage(1);
    setLoadedPages([]);
  }, []);

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
    setFilters(INITIAL_FILTERS);
    setPage(1);
    setLoadedPages([]);
  }, []);

  const handleRemovePill = useCallback(
    (removeFn: (f: TourFiltersType) => TourFiltersType) => {
      const updated = removeFn(filters);
      handleFilterChange(updated);
    },
    [filters, handleFilterChange],
  );

  const pills = useMemo(() => getActiveFilterPills(filters), [filters]);
  const hasMore = data ? page < data.totalPages : false;

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

          <div className="flex items-center gap-3">
            <div className="lg:hidden">
              <TourFilters
                filters={filters}
                onChange={handleFilterChange}
                onReset={handleReset}
              />
            </div>
            <div className="w-48">
              <Select
                options={sortOptions}
                value={filters.sortBy ?? 'popularity'}
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
            <TourGrid tours={displayedTours} isLoading={isLoading} />

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
