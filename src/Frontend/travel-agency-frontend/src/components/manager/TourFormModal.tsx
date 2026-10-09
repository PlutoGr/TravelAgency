import { useState } from 'react';
import type { Tour } from '@/types';
import { Modal, Button, Tabs } from '@/components/ui';
import FormField from './FormField';
import ListEditor from './ListEditor';
import DatesEditor from './DatesEditor';

const FORM_TABS = [
  { id: 'basic', label: 'Основное' },
  { id: 'description', label: 'Описание' },
  { id: 'photos', label: 'Фотографии' },
  { id: 'pricing', label: 'Цены и даты' },
];

interface TourFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  tour: Tour;
  isEditing: boolean;
  onSave: (tour: Tour) => void;
}

export default function TourFormModal({
  isOpen,
  onClose,
  tour,
  isEditing,
  onSave,
}: TourFormModalProps) {
  const [activeTab, setActiveTab] = useState('basic');
  const [formTour, setFormTour] = useState<Tour>(tour);

  const updateField = <K extends keyof Tour>(key: K, value: Tour[K]) => {
    setFormTour((prev) => ({ ...prev, [key]: value }));
  };

  const handleSave = () => {
    onSave(formTour);
    onClose();
  };

  const [openedTour, setOpenedTour] = useState<Tour | null>(isOpen ? tour : null);
  if (isOpen && openedTour !== tour) {
    setOpenedTour(tour);
    setFormTour(tour);
    setActiveTab('basic');
  } else if (!isOpen && openedTour !== null) {
    setOpenedTour(null);
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEditing ? 'Редактировать тур' : 'Создать тур'}
      size="lg"
    >
      <div className="space-y-5">
        <Tabs tabs={FORM_TABS} activeTab={activeTab} onChange={setActiveTab} />

        <div className="min-h-[320px]">
          {activeTab === 'basic' && (
            <div className="space-y-4">
              <FormField
                label="Название"
                value={formTour.title}
                onChange={(v) => updateField('title', v)}
              />
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormField
                  label="Страна"
                  value={formTour.country}
                  onChange={(v) => updateField('country', v)}
                />
                <FormField
                  label="Город"
                  value={formTour.city}
                  onChange={(v) => updateField('city', v)}
                />
              </div>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormField
                  label="Отель"
                  value={formTour.hotel}
                  onChange={(v) => updateField('hotel', v)}
                />
                <FormField
                  label="Категория"
                  value={formTour.category}
                  onChange={(v) => updateField('category', v)}
                />
              </div>
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <FormField
                  label="Макс. туристов"
                  type="number"
                  value={String(formTour.maxTravelers)}
                  onChange={(v) => updateField('maxTravelers', Number(v))}
                />
                <div className="flex items-center gap-3 pt-5">
                  <label className="flex cursor-pointer items-center gap-2">
                    <input
                      type="checkbox"
                      checked={formTour.isHot}
                      onChange={(e) => updateField('isHot', e.target.checked)}
                      className="h-4 w-4 rounded border-sand text-primary accent-primary"
                    />
                    <span className="text-sm text-dark">Горящий тур</span>
                  </label>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'description' && (
            <div className="space-y-4">
              <FormField
                label="Краткое описание"
                value={formTour.shortDescription}
                onChange={(v) => updateField('shortDescription', v)}
              />
              <div>
                <label className="mb-1.5 block text-xs font-medium text-warm-gray">
                  Полное описание
                </label>
                <textarea
                  value={formTour.description}
                  onChange={(e) =>
                    updateField('description', e.target.value)
                  }
                  rows={4}
                  className="w-full resize-none rounded-[12px] border border-sand bg-white px-4 py-3 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
                />
              </div>
              <ListEditor
                label="Включено в стоимость"
                items={formTour.included}
                onChange={(items) => updateField('included', items)}
              />
              <ListEditor
                label="Не включено"
                items={formTour.notIncluded}
                onChange={(items) => updateField('notIncluded', items)}
              />
            </div>
          )}

          {activeTab === 'photos' && (
            <div className="space-y-4">
              <ListEditor
                label="URL фотографий"
                items={formTour.photos}
                onChange={(items) => updateField('photos', items)}
                placeholder="https://..."
              />
              {formTour.photos.length > 0 && (
                <div className="grid grid-cols-3 gap-2">
                  {formTour.photos.map((url, i) => (
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
                  value={String(formTour.price)}
                  onChange={(v) => updateField('price', Number(v))}
                />
                <FormField
                  label="Старая цена (₽)"
                  type="number"
                  value={String(formTour.originalPrice ?? '')}
                  onChange={(v) =>
                    updateField('originalPrice', v ? Number(v) : undefined)
                  }
                />
              </div>
              <FormField
                label="Длительность (дней)"
                type="number"
                value={String(formTour.duration)}
                onChange={(v) => updateField('duration', Number(v))}
              />
              <DatesEditor
                dates={formTour.dates}
                onChange={(dates) => updateField('dates', dates)}
              />
            </div>
          )}
        </div>

        <div className="flex justify-end gap-3 border-t border-sand pt-4">
          <Button variant="ghost" onClick={onClose}>
            Отмена
          </Button>
          <Button onClick={handleSave}>Сохранить</Button>
        </div>
      </div>
    </Modal>
  );
}
