import { useState, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Plus,
  Search,
  Edit3,
  Trash2,
  Star,
  MapPin,
  Flame,
  X,
} from 'lucide-react';
import clsx from 'clsx';
import type { Tour } from '@/types';
import { mockTours } from '@/mocks/tours';
import { Card, Button, Modal, Tabs, Badge, Select } from '@/components/ui';
import { Breadcrumbs } from '@/components/layout';
import { PageTransition } from '@/components/common';

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

const FORM_TABS = [
  { id: 'basic', label: 'Основное' },
  { id: 'description', label: 'Описание' },
  { id: 'photos', label: 'Фотографии' },
  { id: 'pricing', label: 'Цены и даты' },
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

function formatPrice(price: number): string {
  return price.toLocaleString('ru-RU') + ' ₽';
}

export default function ManagerToursPage() {
  const [tours, setTours] = useState<Tour[]>(mockTours);
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [activeTab, setActiveTab] = useState('basic');
  const [editingTour, setEditingTour] = useState<Tour>(EMPTY_TOUR);
  const [toastMessage, setToastMessage] = useState('');

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
    setEditingTour({ ...EMPTY_TOUR, id: `tour-${Date.now()}` });
    setActiveTab('basic');
    setShowModal(true);
  };

  const openEditModal = (tour: Tour) => {
    setEditingTour({ ...tour });
    setActiveTab('basic');
    setShowModal(true);
  };

  const handleSave = () => {
    const existing = tours.find((t) => t.id === editingTour.id);
    if (existing) {
      setTours((prev) =>
        prev.map((t) => (t.id === editingTour.id ? editingTour : t)),
      );
      showToast('Тур обновлён');
    } else {
      setTours((prev) => [editingTour, ...prev]);
      showToast('Тур создан');
    }
    setShowModal(false);
  };

  const handleDelete = (tourId: string) => {
    setTours((prev) => prev.filter((t) => t.id !== tourId));
    showToast('Тур удалён');
  };

  const showToast = (message: string) => {
    setToastMessage(message);
    setTimeout(() => setToastMessage(''), 3000);
  };

  const updateField = <K extends keyof Tour>(key: K, value: Tour[K]) => {
    setEditingTour((prev) => ({ ...prev, [key]: value }));
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

        {/* Tours — Desktop table */}
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
                  <motion.tr
                    key={tour.id}
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: idx * 0.03 }}
                    className="border-b border-sand/60 transition-colors last:border-0 hover:bg-cream/40"
                  >
                    <td className="px-5 py-3">
                      <img
                        src={tour.photos[0]}
                        alt={tour.title}
                        className="h-12 w-16 rounded-lg object-cover"
                      />
                    </td>
                    <td className="px-5 py-3">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-dark">
                          {tour.title}
                        </span>
                        {tour.isHot && (
                          <Badge variant="red" size="sm">
                            <span className="flex items-center gap-0.5">
                              <Flame size={10} />
                              Горящий
                            </span>
                          </Badge>
                        )}
                      </div>
                    </td>
                    <td className="px-5 py-3 text-warm-gray">
                      <span className="flex items-center gap-1">
                        <MapPin size={14} />
                        {tour.country}
                      </span>
                    </td>
                    <td className="px-5 py-3 font-semibold text-primary">
                      {formatPrice(tour.price)}
                    </td>
                    <td className="px-5 py-3">
                      <span className="flex items-center gap-1 text-warm-gray">
                        <Star
                          size={14}
                          className="fill-amber-400 text-amber-400"
                        />
                        {tour.rating}
                      </span>
                    </td>
                    <td className="px-5 py-3">
                      <Badge variant="gray" size="sm">
                        {tour.category}
                      </Badge>
                    </td>
                    <td className="px-5 py-3">
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => openEditModal(tour)}
                          className="rounded-lg p-1.5 text-warm-gray transition-colors hover:bg-sand hover:text-primary"
                          title="Редактировать"
                        >
                          <Edit3 size={16} />
                        </button>
                        <button
                          onClick={() => handleDelete(tour.id)}
                          className="rounded-lg p-1.5 text-warm-gray transition-colors hover:bg-red-50 hover:text-red-500"
                          title="Удалить"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </td>
                  </motion.tr>
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

        {/* Tours — Mobile cards */}
        <div className="space-y-3 lg:hidden">
          {filteredTours.map((tour, idx) => (
            <motion.div
              key={tour.id}
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: idx * 0.04 }}
            >
              <Card className="overflow-hidden">
                <div className="flex gap-3 p-4">
                  <img
                    src={tour.photos[0]}
                    alt={tour.title}
                    className="h-20 w-20 shrink-0 rounded-[12px] object-cover"
                  />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-start gap-2">
                      <h3 className="line-clamp-1 text-sm font-semibold text-dark">
                        {tour.title}
                      </h3>
                      {tour.isHot && (
                        <Badge variant="red" size="sm">
                          <Flame size={10} />
                        </Badge>
                      )}
                    </div>
                    <p className="mt-0.5 flex items-center gap-1 text-xs text-warm-gray">
                      <MapPin size={12} />
                      {tour.country}, {tour.city}
                    </p>
                    <div className="mt-1 flex items-center gap-3">
                      <span className="text-sm font-bold text-primary">
                        {formatPrice(tour.price)}
                      </span>
                      <span className="flex items-center gap-0.5 text-xs text-warm-gray">
                        <Star
                          size={12}
                          className="fill-amber-400 text-amber-400"
                        />
                        {tour.rating}
                      </span>
                    </div>
                  </div>
                </div>
                <div className="flex border-t border-sand">
                  <button
                    onClick={() => openEditModal(tour)}
                    className="flex flex-1 items-center justify-center gap-1.5 py-2.5 text-sm text-warm-gray transition-colors hover:text-primary"
                  >
                    <Edit3 size={14} />
                    Изменить
                  </button>
                  <div className="w-px bg-sand" />
                  <button
                    onClick={() => handleDelete(tour.id)}
                    className="flex flex-1 items-center justify-center gap-1.5 py-2.5 text-sm text-warm-gray transition-colors hover:text-red-500"
                  >
                    <Trash2 size={14} />
                    Удалить
                  </button>
                </div>
              </Card>
            </motion.div>
          ))}

          {filteredTours.length === 0 && (
            <div className="py-16 text-center text-warm-gray">
              <p className="text-base font-medium">Туры не найдены</p>
            </div>
          )}
        </div>
      </div>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        title={
          tours.some((t) => t.id === editingTour.id)
            ? 'Редактировать тур'
            : 'Создать тур'
        }
        size="lg"
      >
        <div className="space-y-5">
          <Tabs tabs={FORM_TABS} activeTab={activeTab} onChange={setActiveTab} />

          <div className="min-h-[320px]">
            {activeTab === 'basic' && (
              <div className="space-y-4">
                <FormField
                  label="Название"
                  value={editingTour.title}
                  onChange={(v) => updateField('title', v)}
                />
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <FormField
                    label="Страна"
                    value={editingTour.country}
                    onChange={(v) => updateField('country', v)}
                  />
                  <FormField
                    label="Город"
                    value={editingTour.city}
                    onChange={(v) => updateField('city', v)}
                  />
                </div>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <FormField
                    label="Отель"
                    value={editingTour.hotel}
                    onChange={(v) => updateField('hotel', v)}
                  />
                  <FormField
                    label="Категория"
                    value={editingTour.category}
                    onChange={(v) => updateField('category', v)}
                  />
                </div>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <FormField
                    label="Макс. туристов"
                    type="number"
                    value={String(editingTour.maxTravelers)}
                    onChange={(v) => updateField('maxTravelers', Number(v))}
                  />
                  <div className="flex items-center gap-3 pt-5">
                    <label className="flex cursor-pointer items-center gap-2">
                      <input
                        type="checkbox"
                        checked={editingTour.isHot}
                        onChange={(e) =>
                          updateField('isHot', e.target.checked)
                        }
                        className="h-4 w-4 rounded border-sand text-primary accent-primary"
                      />
                      <span className="text-sm text-dark">
                        Горящий тур
                      </span>
                    </label>
                  </div>
                </div>
              </div>
            )}

            {activeTab === 'description' && (
              <div className="space-y-4">
                <FormField
                  label="Краткое описание"
                  value={editingTour.shortDescription}
                  onChange={(v) => updateField('shortDescription', v)}
                />
                <div>
                  <label className="mb-1.5 block text-xs font-medium text-warm-gray">
                    Полное описание
                  </label>
                  <textarea
                    value={editingTour.description}
                    onChange={(e) =>
                      updateField('description', e.target.value)
                    }
                    rows={4}
                    className="w-full resize-none rounded-[12px] border border-sand bg-white px-4 py-3 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
                  />
                </div>
                <ListEditor
                  label="Включено в стоимость"
                  items={editingTour.included}
                  onChange={(items) => updateField('included', items)}
                />
                <ListEditor
                  label="Не включено"
                  items={editingTour.notIncluded}
                  onChange={(items) => updateField('notIncluded', items)}
                />
              </div>
            )}

            {activeTab === 'photos' && (
              <div className="space-y-4">
                <ListEditor
                  label="URL фотографий"
                  items={editingTour.photos}
                  onChange={(items) => updateField('photos', items)}
                  placeholder="https://..."
                />
                {editingTour.photos.length > 0 && (
                  <div className="grid grid-cols-3 gap-2">
                    {editingTour.photos.map((url, i) => (
                      <img
                        key={i}
                        src={url}
                        alt={`Фото ${i + 1}`}
                        className="h-24 w-full rounded-lg object-cover"
                      />
                    ))}
                  </div>
                )}
              </div>
            )}

            {activeTab === 'pricing' && (
              <div className="space-y-4">
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <FormField
                    label="Цена (₽)"
                    type="number"
                    value={String(editingTour.price)}
                    onChange={(v) => updateField('price', Number(v))}
                  />
                  <FormField
                    label="Старая цена (₽)"
                    type="number"
                    value={String(editingTour.originalPrice ?? '')}
                    onChange={(v) =>
                      updateField(
                        'originalPrice',
                        v ? Number(v) : undefined,
                      )
                    }
                  />
                </div>
                <FormField
                  label="Длительность (дней)"
                  type="number"
                  value={String(editingTour.duration)}
                  onChange={(v) => updateField('duration', Number(v))}
                />
                <DatesEditor
                  dates={editingTour.dates}
                  onChange={(dates) => updateField('dates', dates)}
                />
              </div>
            )}
          </div>

          <div className="flex justify-end gap-3 border-t border-sand pt-4">
            <Button
              variant="ghost"
              onClick={() => setShowModal(false)}
            >
              Отмена
            </Button>
            <Button onClick={handleSave}>Сохранить</Button>
          </div>
        </div>
      </Modal>

      {/* Toast */}
      <AnimatePresence>
        {toastMessage && (
          <motion.div
            initial={{ opacity: 0, y: 50 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 50 }}
            className="fixed bottom-6 left-1/2 z-50 -translate-x-1/2"
          >
            <div className="flex items-center gap-2 rounded-xl bg-dark px-5 py-3 text-sm font-medium text-white shadow-modal">
              {toastMessage}
              <button onClick={() => setToastMessage('')}>
                <X size={16} className="text-white/60 hover:text-white" />
              </button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </PageTransition>
  );
}

function FormField({
  label,
  value,
  onChange,
  type = 'text',
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  placeholder?: string;
}) {
  return (
    <div>
      <label className="mb-1.5 block text-xs font-medium text-warm-gray">
        {label}
      </label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full rounded-[12px] border border-sand bg-white px-4 py-2.5 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
      />
    </div>
  );
}

function ListEditor({
  label,
  items,
  onChange,
  placeholder = 'Новый пункт...',
}: {
  label: string;
  items: string[];
  onChange: (items: string[]) => void;
  placeholder?: string;
}) {
  const [newItem, setNewItem] = useState('');

  const addItem = () => {
    if (!newItem.trim()) return;
    onChange([...items, newItem.trim()]);
    setNewItem('');
  };

  const removeItem = (index: number) => {
    onChange(items.filter((_, i) => i !== index));
  };

  return (
    <div>
      <label className="mb-1.5 block text-xs font-medium text-warm-gray">
        {label}
      </label>
      <div className="space-y-1.5">
        {items.map((item, i) => (
          <div
            key={i}
            className="flex items-center gap-2 rounded-lg bg-cream px-3 py-1.5 text-sm text-dark"
          >
            <span className="flex-1">{item}</span>
            <button
              onClick={() => removeItem(i)}
              className="text-warm-gray hover:text-red-500"
            >
              <X size={14} />
            </button>
          </div>
        ))}
      </div>
      <div className="mt-2 flex gap-2">
        <input
          type="text"
          value={newItem}
          onChange={(e) => setNewItem(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && addItem()}
          placeholder={placeholder}
          className="flex-1 rounded-[12px] border border-sand bg-white px-4 py-2 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
        />
        <Button variant="secondary" size="sm" onClick={addItem}>
          Добавить
        </Button>
      </div>
    </div>
  );
}

function DatesEditor({
  dates,
  onChange,
}: {
  dates: { start: string; end: string }[];
  onChange: (dates: { start: string; end: string }[]) => void;
}) {
  const [newStart, setNewStart] = useState('');
  const [newEnd, setNewEnd] = useState('');

  const addDate = () => {
    if (!newStart || !newEnd) return;
    onChange([...dates, { start: newStart, end: newEnd }]);
    setNewStart('');
    setNewEnd('');
  };

  const removeDate = (index: number) => {
    onChange(dates.filter((_, i) => i !== index));
  };

  return (
    <div>
      <label className="mb-1.5 block text-xs font-medium text-warm-gray">
        Даты заездов
      </label>
      <div className="space-y-1.5">
        {dates.map((d, i) => (
          <div
            key={i}
            className="flex items-center gap-2 rounded-lg bg-cream px-3 py-1.5 text-sm text-dark"
          >
            <span className="flex-1">
              {d.start} — {d.end}
            </span>
            <button
              onClick={() => removeDate(i)}
              className="text-warm-gray hover:text-red-500"
            >
              <X size={14} />
            </button>
          </div>
        ))}
      </div>
      <div className="mt-2 grid grid-cols-[1fr_1fr_auto] gap-2">
        <input
          type="date"
          value={newStart}
          onChange={(e) => setNewStart(e.target.value)}
          className="rounded-[12px] border border-sand bg-white px-3 py-2 text-sm text-dark outline-none focus:border-primary focus:ring-2 focus:ring-primary/10"
        />
        <input
          type="date"
          value={newEnd}
          onChange={(e) => setNewEnd(e.target.value)}
          className="rounded-[12px] border border-sand bg-white px-3 py-2 text-sm text-dark outline-none focus:border-primary focus:ring-2 focus:ring-primary/10"
        />
        <Button variant="secondary" size="sm" onClick={addDate}>
          +
        </Button>
      </div>
    </div>
  );
}
