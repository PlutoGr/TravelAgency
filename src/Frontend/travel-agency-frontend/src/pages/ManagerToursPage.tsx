import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Search } from 'lucide-react';
import {
  deleteManagedTour,
  listManagedTours,
  publishManagedTour,
  unpublishManagedTour,
  type ManagedTour,
} from '@/api/tourManage';
import { TOUR_MANAGE_STATUS_TEXT, readManageFailure, tourManageErrorText, type ManageFailure } from '@/domain/tourManageErrors';
import { Card, Skeleton } from '@/components/ui';
import { Breadcrumbs } from '@/components/layout';
import { PageTransition } from '@/components/common';
import { manageMediaFileUrl } from '@/utils/media';
import { tourStatusLabel } from '@/utils/tourLabels';

const BREADCRUMBS = [
  { label: 'Панель менеджера', path: '/manager' },
  { label: 'Туры' },
];

export default function ManagerToursPage() {
  const [tours, setTours] = useState<ManagedTour[]>([]);
  const [loading, setLoading] = useState(true);
  const [failure, setFailure] = useState<ManageFailure | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [busyId, setBusyId] = useState<string | null>(null);

  const loadTours = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      setTours(await listManagedTours());
    } catch (error: unknown) {
      setFailure(readManageFailure(error));
      setTours([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadTours();
  }, [loadTours]);

  const filtered = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    if (!query) return tours;
    return tours.filter(
      (tour) => tour.title.toLowerCase().includes(query) || tour.country.toLowerCase().includes(query),
    );
  }, [tours, searchQuery]);

  async function run(id: string, action: () => Promise<void>) {
    setBusyId(id);
    setFailure(null);
    try {
      await action();
    } catch (error: unknown) {
      setFailure(readManageFailure(error));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={BREADCRUMBS} />
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">Управление турами</h1>
          <Link
            to="/manager/tours/new"
            className="inline-flex items-center justify-center gap-2 rounded-[12px] bg-primary px-6 py-2.5 text-base font-medium text-white shadow-button hover:bg-primary-light"
          >
            <Plus size={18} />
            Создать тур
          </Link>
        </div>

        <Card className="p-4 sm:p-5">
          <div className="relative">
            <Search size={18} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-warm-gray" />
            <input
              type="text"
              aria-label="Поиск туров"
              placeholder="Поиск по названию или стране..."
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              className="w-full rounded-[12px] border border-sand bg-white py-2.5 pl-10 pr-4 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
            />
          </div>
        </Card>

        {failure && (
          <div role="alert" className="rounded-[12px] border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            <p>{failure.message}</p>
            {failure.missing.length > 0 && (
              <ul className="mt-2 list-disc pl-5">
                {failure.missing.map((code) => (
                  <li key={code}>{tourManageErrorText(code)}</li>
                ))}
              </ul>
            )}
          </div>
        )}

        {loading && (
          <Card className="space-y-4 p-6">
            <Skeleton height={48} variant="rectangular" className="w-full" />
            <Skeleton height={48} variant="rectangular" className="w-full" />
          </Card>
        )}

        {!loading && (
          <Card className="overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-sand bg-cream/60 text-left">
                    <th className="px-5 py-3.5 font-medium text-warm-gray">Фото</th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">Название</th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">Страна</th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">Статус</th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">Дней</th>
                    <th className="px-5 py-3.5 font-medium text-warm-gray">Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((tour) => {
                    const cover = tour.images.find((image) => image.isCover) ?? tour.images[0];
                    return (
                      <tr key={tour.id} className="border-b border-sand/60 last:border-0">
                        <td className="px-5 py-3">
                          {cover ? (
                            <img
                              src={manageMediaFileUrl(cover.mediaFileId, 'w200')}
                              alt={tour.title || 'Фото тура'}
                              className="h-12 w-16 rounded-lg object-cover"
                            />
                          ) : (
                            <div className="h-12 w-16 rounded-lg bg-sand" aria-hidden />
                          )}
                        </td>
                        <td className="px-5 py-3 font-medium text-dark">{tour.title || 'Без названия'}</td>
                        <td className="px-5 py-3 text-dark/80">{tour.country || '—'}</td>
                        <td className="px-5 py-3">{tourStatusLabel(tour.status)}</td>
                        <td className="px-5 py-3">{tour.durationDays || '—'}</td>
                        <td className="px-5 py-3">
                          <div className="flex flex-wrap gap-2">
                            <Link to={`/manager/tours/${tour.id}`} className="font-medium text-primary">
                              Редактировать
                            </Link>
                            {tour.status === 'Draft' && (
                              <button
                                type="button"
                                className="font-medium text-red-600"
                                disabled={busyId === tour.id}
                                onClick={() =>
                                  run(tour.id, async () => {
                                    if (!window.confirm('Удалить черновик? Это нельзя отменить.')) return;
                                    await deleteManagedTour(tour.id, tour.etag);
                                    setTours((prev) => prev.filter((item) => item.id !== tour.id));
                                  })
                                }
                              >
                                Удалить
                              </button>
                            )}
                            {tour.status === 'Published' && (
                              <button
                                type="button"
                                className="font-medium text-dark"
                                disabled={busyId === tour.id}
                                onClick={() =>
                                  run(tour.id, async () => {
                                    const updated = await unpublishManagedTour(tour.id, tour.etag);
                                    setTours((prev) => prev.map((item) => (item.id === tour.id ? updated : item)));
                                  })
                                }
                              >
                                Снять с публикации
                              </button>
                            )}
                            {tour.status === 'Unpublished' && (
                              <button
                                type="button"
                                className="font-medium text-primary"
                                disabled={busyId === tour.id}
                                onClick={() =>
                                  run(tour.id, async () => {
                                    const updated = await publishManagedTour(tour.id, tour.etag);
                                    setTours((prev) => prev.map((item) => (item.id === tour.id ? updated : item)));
                                  })
                                }
                              >
                                Опубликовать снова
                              </button>
                            )}
                          </div>
                          {tour.status !== 'Draft' && (
                            <p className="mt-1 text-xs text-warm-gray">{TOUR_MANAGE_STATUS_TEXT.draftOnly}</p>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            {filtered.length === 0 && <p className="py-16 text-center text-warm-gray">Туры не найдены</p>}
          </Card>
        )}
      </div>
    </PageTransition>
  );
}
