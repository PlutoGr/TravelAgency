import { useState, useMemo, useCallback, useEffect } from 'react';
import { Plus, Search } from 'lucide-react';
import type { Tour } from '@/types';
import {
  getToursForManager,
  createTour,
  updateTour,
  updateTourPrices,
  deleteTour,
} from '@/api/catalog';
import { Card, Button, Select } from '@/components/ui';
import { Skeleton } from '@/components/ui';
import { Breadcrumbs } from '@/components/layout';
import { PageTransition } from '@/components/common';
import TourFormModal from '@/components/manager/TourFormModal';
import TourTableRow from '@/components/manager/TourTableRow';
import TourMobileCard from '@/components/manager/TourMobileCard';
import toast from 'react-hot-toast';

const BREADCRUMBS = [
  { label: 'Панель менеджера', path: '/manager' },
  { label: 'Туры' },
];

const CATEGORIES = [
  { value: '', label: 'Все категории' },
  { value: 'Пляжный отдых', label: 'Пляжный отдых' },
  { value: 'Экзотика', label: 'Экзотика' },
  { value: 'Люкс', label: 'Люкс' },
  { value: 'Экскурсионный', label: 'Экскурсионный' },
  { value: 'Романтический', label: 'Романтический' },
  { value: 'Гастрономический', label: 'Гастрономический' },
];

const EMPTY_TOUR: Tour = {
  id: '',
  title: '',
  description: '',
  shortDescription: '',
  country: '',
  city: '',
  hotel: '',
  price: 0,
  rating: 0,
  reviewCount: 0,
  dates: [],
  duration: 7,
  photos: [],
  amenities: [],
  included: [],
  notIncluded: [],
  category: '',
  isHot: false,
  maxTravelers: 4,
};

export default function ManagerToursPage() {
  const [tours, setTours] = useState<Tour[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [editingTour, setEditingTour] = useState<Tour>(EMPTY_TOUR);

  const loadTours = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getToursForManager();
      setTours(result.items);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Не удалось загрузить туры';
      setError(message);
      toast.error(message);
      setTours([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadTours();
  }, [loadTours]);

  const filteredTours = useMemo(() => {
    let result = tours;

    if (searchQuery) {
      const q = searchQuery.toLowerCase();
      result = result.filter(
        (t) =>
          t.title.toLowerCase().includes(q) ||
          t.country.toLowerCase().includes(q) ||
          t.city.toLowerCase().includes(q),
      );
    }

    if (categoryFilter) {
      result = result.filter((t) => t.category === categoryFilter);
    }

    return result;
  }, [tours, searchQuery, categoryFilter]);

  const openCreateModal = () => {
    setEditingTour({ ...EMPTY_TOUR, id: `tour-new-${Date.now()}` });
    setShowModal(true);
  };

  const openEditModal = (tour: Tour) => {
    setEditingTour({ ...tour });
    setShowModal(true);
  };

  const handleSave = async (tour: Tour) => {
    try {
      const isEdit = tours.some((t) => t.id === tour.id);

      if (isEdit) {
        await updateTour(tour.id, {
          title: tour.title,
          description: tour.description || tour.shortDescription,
          tourType: tour.category,
          country: tour.country,
          durationDays: tour.duration,
          imageUrl: tour.photos?.[0] ?? null,
          directionId: null,
        });
        if (tour.dates.length > 0 && tour.price > 0) {
          await updateTourPrices(
            tour.id,
            tour.dates.map((d) => ({
              validFrom: d.start,
              validTo: d.end,
              pricePerPerson: tour.price,
              currency: 'RUB',
              availableSeats: tour.maxTravelers,
            })),
          );
        }
        const updated = await getToursForManager();
        setTours(updated.items);
        toast.success('Тур обновлён');
      } else {
        const created = await createTour({
          title: tour.title,
          description: tour.description || tour.shortDescription,
          tourType: tour.category,
          country: tour.country,
          durationDays: tour.duration,
          imageUrl: tour.photos?.[0] ?? null,
          directionId: null,
        });
        if (tour.dates.length > 0 && tour.price > 0) {
          await updateTourPrices(
            created.id,
            tour.dates.map((d) => ({
              validFrom: d.start,
              validTo: d.end,
              pricePerPerson: tour.price,
              currency: 'RUB',
              availableSeats: tour.maxTravelers,
            })),
          );
        }
        const updated = await getToursForManager();
        setTours(updated.items);
        toast.success('Тур создан');
      }
      setShowModal(false);
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Не удалось сохранить тур';
      toast.error(message);
    }
  };

  const handleDelete = async (tourId: string) => {
    try {
      await deleteTour(tourId);
      setTours((prev) => prev.filter((t) => t.id !== tourId));
      toast.success('Тур удалён');
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Не удалось удалить тур';
      toast.error(message);
    }
  };

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />

        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">
            Управление турами
          </h1>
          <Button
            leftIcon={<Plus size={18} />}
            onClick={openCreateModal}
            disabled={loading}
          >
            Создать тур
          </Button>
        </div>

        {/* Filters */}
        <Card className="p-4 sm:p-5">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div className="relative">
              <Search
                size={18}
                className="absolute left-3.5 top-1/2 -translate-y-1/2 text-warm-gray"
              />
              <input
                type="text"
                placeholder="Поиск по названию или стране..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full rounded-[12px] border border-sand bg-white py-2.5 pl-10 pr-4 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
              />
            </div>
            <Select
              options={CATEGORIES}
              value={categoryFilter}
              onChange={setCategoryFilter}
              placeholder="Все категории"
            />
          </div>
        </Card>

        {/* Loading */}
        {loading && (
          <Card className="space-y-4 p-6">
            <Skeleton height={48} variant="rectangular" className="w-full" />
            <Skeleton height={48} variant="rectangular" className="w-full" />
            <Skeleton height={48} variant="rectangular" className="w-full" />
          </Card>
        )}

        {/* Error */}
        {error && !loading && (
          <Card className="p-6">
            <p className="text-center text-red-500">{error}</p>
            <div className="mt-4 flex justify-center">
              <Button variant="secondary" onClick={() => loadTours()}>
                Повторить
              </Button>
            </div>
          </Card>
        )}

        {/* Tours — Desktop table */}
        {!loading && !error && (
          <Card className="hidden overflow-hidden lg:block">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-sand bg-cream/60 text-left">
                    <th className="px-5 py-3.5 font-medium text-warm-gray">
                      Фото
                    </th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">
                      Название
                    </th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">
                      Страна
                    </th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">
                      Цена
                    </th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">
                      Рейтинг
                    </th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">
                      Категория
                    </th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">
                      Действия
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {filteredTours.map((tour, idx) => (
                    <TourTableRow
                      key={tour.id}
                      tour={tour}
                      index={idx}
                      onEdit={openEditModal}
                      onDelete={handleDelete}
                    />
                  ))}
                </tbody>
              </table>
            </div>

            {filteredTours.length === 0 && (
              <div className="py-16 text-center text-warm-gray">
                <p className="text-base font-medium">Туры не найдены</p>
              </div>
            )}
          </Card>
        )}

        {/* Tours — Mobile cards */}
        {!loading && !error && (
          <div className="space-y-3 lg:hidden">
            {filteredTours.map((tour, idx) => (
              <TourMobileCard
                key={tour.id}
                tour={tour}
                index={idx}
                onEdit={openEditModal}
                onDelete={handleDelete}
              />
            ))}

            {filteredTours.length === 0 && (
              <div className="py-16 text-center text-warm-gray">
                <p className="text-base font-medium">Туры не найдены</p>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Create/Edit Modal */}
      <TourFormModal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        tour={editingTour}
        isEditing={tours.some((t) => t.id === editingTour.id)}
        onSave={handleSave}
      />
    </PageTransition>
  );
}
