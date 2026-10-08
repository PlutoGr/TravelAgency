import { useEffect, useState, type ReactNode } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowDown, ArrowUp, Plus, Trash2 } from 'lucide-react';
import { getDestinations } from '@/api/catalog';
import { uploadTourImage } from '@/api/mediaUpload';
import {
  createTourDraft,
  deleteManagedTour,
  getManagedTour,
  publishManagedTour,
  saveBasics,
  saveConditions,
  saveDescription,
  saveImages,
  saveOffers,
  saveProgram,
  unpublishManagedTour,
  type ManagedTour,
  type TourBasicsInput,
  type TourDayInput,
  type TourImageInput,
  type TourInclusionInput,
  type TourOfferInput,
} from '@/api/tourManage';
import {
  TOUR_MANAGE_STATUS_TEXT,
  readManageFailure,
  tourManageErrorText,
  type ManageFailure,
} from '@/domain/tourManageErrors';
import {
  MAX_IMAGES,
  emptyWizardForm,
  formFromTour,
  missingPublishCodes,
  resolveWizardStep,
  validateWizardStep,
  type WizardForm,
  type WizardImage,
} from '@/domain/tourWizard';
import { Button, Card } from '@/components/ui';
import { Breadcrumbs } from '@/components/layout';
import { PageTransition } from '@/components/common';
import { manageMediaFileUrl } from '@/utils/media';
import { TOUR_TYPE_OPTIONS, mealPlanLabel, tourStatusLabel } from '@/utils/tourLabels';
import type { Destination } from '@/types';

const STEPS = [
  'Основное',
  'Программа',
  'Включено и не включено',
  'Проживание и питание',
  'Даты и цены',
  'Фото',
  'Проверка и публикация',
] as const;

const MEAL_PLANS = ['RO', 'BB', 'HB', 'FB', 'AI', 'UAI'] as const;

const fieldClass =
  'w-full rounded-[12px] border border-sand bg-white px-4 py-2.5 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10';

interface TourWizardProps {
  tourId: string | null;
  initialStep?: number;
}

function basicsInput(form: WizardForm): TourBasicsInput {
  const duration = form.durationDays.trim() === '' ? 0 : Number(form.durationDays);
  return {
    title: form.title.trim() || null,
    shortDescription: form.shortDescription.trim() || null,
    departureCity: form.departureCity.trim() || null,
    country: form.country.trim() || null,
    tourType: form.tourType || null,
    durationDays: duration,
    directionId: form.directionId || null,
  };
}

function programInput(form: WizardForm): TourDayInput[] {
  return form.days
    .filter((day) => day.title.trim() && day.description.trim())
    .map((day) => ({
      dayNumber: day.dayNumber,
      title: day.title.trim(),
      description: day.description.trim(),
    }));
}

function conditionsInput(form: WizardForm): {
  inclusions: TourInclusionInput[];
  mealPlan: string | null;
  accommodationText: string | null;
} {
  return {
    inclusions: form.inclusions.map((item, index) => ({
      text: item.text.trim(),
      kind: item.kind,
      sortOrder: index,
    })),
    mealPlan: form.mealPlan || null,
    accommodationText: form.accommodationText.trim() || null,
  };
}

function offersInput(form: WizardForm): TourOfferInput[] {
  return form.offers.map((offer) => ({
    validFrom: `${offer.validFrom}T00:00:00Z`,
    validTo: `${offer.validTo}T00:00:00Z`,
    pricePerPerson: Number(offer.pricePerPerson),
    currency: offer.currency.trim().toUpperCase(),
    availableSeats: Number(offer.availableSeats),
  }));
}

function imagesInput(form: WizardForm): TourImageInput[] {
  return form.images.map((image, index) => ({
    mediaFileId: image.mediaFileId,
    sortOrder: index,
    isCover: image.isCover,
    alt: image.alt.trim() || null,
  }));
}

function withStep(pathname: string, current: URLSearchParams, step: number): string {
  const params = new URLSearchParams(current);
  params.set('step', String(step));
  const query = params.toString();
  return query ? `${pathname}?${query}` : pathname;
}

export default function TourWizard({ tourId, initialStep = 1 }: TourWizardProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const step = resolveWizardStep(searchParams.get('step'), initialStep);
  const [tour, setTour] = useState<ManagedTour | null>(null);
  const [form, setForm] = useState<WizardForm>(emptyWizardForm);
  const [loading, setLoading] = useState(Boolean(tourId));
  const [saving, setSaving] = useState(false);
  const [failure, setFailure] = useState<ManageFailure | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [directions, setDirections] = useState<Destination[]>([]);

  useEffect(() => {
    let cancelled = false;
    getDestinations()
      .then((items) => {
        if (!cancelled) setDirections(items);
      })
      .catch(() => {
        if (!cancelled) setDirections([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!tourId) return;
    let cancelled = false;
    setLoading(true);
    getManagedTour(tourId)
      .then((loaded) => {
        if (cancelled) return;
        setTour(loaded);
        setForm(formFromTour(loaded));
      })
      .catch((error: unknown) => {
        if (!cancelled) setFailure(readManageFailure(error));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [tourId]);

  useEffect(() => {
    const raw = searchParams.get('step');
    const resolved = resolveWizardStep(raw, initialStep);
    if (raw === String(resolved)) return;
    navigate(withStep(location.pathname, searchParams, resolved), { replace: true });
  }, [searchParams, initialStep, location.pathname, navigate]);

  function moveToStep(next: number, history: 'push' | 'replace') {
    const clamped = resolveWizardStep(String(next));
    if (searchParams.get('step') === String(clamped)) return;
    navigate(withStep(location.pathname, searchParams, clamped), { replace: history === 'replace' });
  }

  async function persist(current: number): Promise<ManagedTour> {
    if (current === 1) {
      if (!tour) {
        return createTourDraft({
          ...basicsInput(form),
          description: form.description.trim() || null,
        });
      }
      const basics = await saveBasics(tour.id, tour.etag, basicsInput(form));
      return saveDescription(basics.id, basics.etag, form.description.trim());
    }

    if (!tour) throw new Error('Сначала сохраните основное.');
    if (current === 2) return saveProgram(tour.id, tour.etag, programInput(form));
    if (current === 3 || current === 4) return saveConditions(tour.id, tour.etag, conditionsInput(form));
    if (current === 5) return saveOffers(tour.id, tour.etag, offersInput(form));
    if (current === 6) return saveImages(tour.id, tour.etag, imagesInput(form));
    return tour;
  }

  function applySaved(saved: ManagedTour, nextStep: number) {
    setTour(saved);
    setForm(formFromTour(saved));
    const clamped = resolveWizardStep(String(nextStep));
    if (!tourId || tourId !== saved.id) {
      const created = `/manager/tours/${saved.id}`;
      if (clamped === step) {
        navigate(withStep(created, searchParams, clamped), { replace: true });
        return;
      }
      // /new не должен оставаться в истории: replace на тот же шаг с id, затем push следующего.
      navigate(withStep(created, searchParams, step), { replace: true });
      navigate(withStep(created, searchParams, clamped), { replace: false });
      return;
    }
    moveToStep(clamped, clamped === step ? 'replace' : 'push');
  }

  async function saveAnd(next: number | null) {
    const errors = validateWizardStep(step, form);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;
    setSaving(true);
    setFailure(null);
    try {
      const saved = await persist(step);
      applySaved(saved, next ?? step);
    } catch (error: unknown) {
      setFailure(readManageFailure(error));
    } finally {
      setSaving(false);
    }
  }

  async function reload() {
    if (!tour) return;
    setSaving(true);
    try {
      const fresh = await getManagedTour(tour.id);
      setTour(fresh);
      setForm(formFromTour(fresh));
      setFailure(null);
    } catch (error: unknown) {
      setFailure(readManageFailure(error));
    } finally {
      setSaving(false);
    }
  }

  async function publish() {
    if (!tour) return;
    setSaving(true);
    setFailure(null);
    try {
      const saved = await publishManagedTour(tour.id, tour.etag);
      applySaved(saved, step);
    } catch (error: unknown) {
      setFailure(readManageFailure(error));
    } finally {
      setSaving(false);
    }
  }

  async function unpublish() {
    if (!tour) return;
    setSaving(true);
    setFailure(null);
    try {
      const saved = await unpublishManagedTour(tour.id, tour.etag);
      applySaved(saved, step);
    } catch (error: unknown) {
      setFailure(readManageFailure(error));
    } finally {
      setSaving(false);
    }
  }

  async function removeDraft() {
    if (!tour || tour.status !== 'Draft') return;
    if (!window.confirm('Удалить черновик? Это нельзя отменить.')) return;
    setSaving(true);
    setFailure(null);
    try {
      await deleteManagedTour(tour.id, tour.etag);
      navigate('/manager/tours');
    } catch (error: unknown) {
      setFailure(readManageFailure(error));
    } finally {
      setSaving(false);
    }
  }

  async function onUpload(file: File | undefined) {
    if (!file) return;
    if (form.images.length >= MAX_IMAGES) {
      setFailure({ status: 0, missing: [], message: TOUR_MANAGE_STATUS_TEXT.tooManyPhotos });
      return;
    }
    const allowed = file.type === 'image/jpeg' || file.type === 'image/png' || file.type === 'image/webp';
    if (!allowed) {
      setFailure({ status: 0, missing: [], message: TOUR_MANAGE_STATUS_TEXT.fileType });
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      setFailure({ status: 0, missing: [], message: TOUR_MANAGE_STATUS_TEXT.fileTooLarge });
      return;
    }
    setSaving(true);
    setFailure(null);
    try {
      const uploaded = await uploadTourImage(file);
      const image: WizardImage = {
        mediaFileId: uploaded.id,
        alt: '',
        isCover: form.images.every((item) => !item.isCover),
        sortOrder: form.images.length,
        widthPx: typeof uploaded.width === 'number' ? uploaded.width : null,
      };
      setForm((prev) => ({ ...prev, images: [...prev.images, image] }));
    } catch (error: unknown) {
      setFailure(readManageFailure(error));
    } finally {
      setSaving(false);
    }
  }

  const missing = missingPublishCodes(form, new Date());
  const status = tour?.status ?? '';

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs
          items={[
            { label: 'Панель менеджера', path: '/manager' },
            { label: 'Туры', path: '/manager/tours' },
            { label: tour?.title?.trim() || 'Новый тур' },
          ]}
        />

        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">Мастер тура</h1>
            <p className="mt-1 text-sm text-warm-gray">
              Шаг {step} из {STEPS.length}
              {status ? ` · ${tourStatusLabel(status)}` : ''}
            </p>
          </div>
          {tour?.status === 'Draft' && (
            <Button variant="danger" size="sm" onClick={removeDraft} disabled={saving} leftIcon={<Trash2 size={16} />}>
              Удалить черновик
            </Button>
          )}
          {tour && tour.status !== 'Draft' && (
            <p className="text-sm text-warm-gray">{TOUR_MANAGE_STATUS_TEXT.draftOnly}</p>
          )}
        </div>

        <nav aria-label="Шаги мастера">
          <ol className="flex gap-2 overflow-x-auto pb-1">
            {STEPS.map((label, index) => {
              const number = index + 1;
              const current = number === step;
              return (
                <li key={label}>
                  <button
                    type="button"
                    aria-current={current ? 'step' : undefined}
                    disabled={number > step || saving}
                    onClick={() => {
                      if (number < step) moveToStep(number, 'push');
                    }}
                    className={
                      current
                        ? 'rounded-full bg-primary px-3 py-1.5 text-sm font-medium text-white'
                        : 'rounded-full bg-sand px-3 py-1.5 text-sm font-medium text-dark disabled:opacity-50'
                    }
                  >
                    {number}. {label}
                  </button>
                </li>
              );
            })}
          </ol>
        </nav>

        {loading && <p className="text-sm text-warm-gray">Загрузка тура</p>}

        {!loading && (
          <Card className="space-y-5 p-5 sm:p-6">
            <h2 className="font-heading text-xl font-semibold text-dark">{STEPS[step - 1]}</h2>
            {failure && <FailureBanner failure={failure} onReload={tour ? reload : undefined} />}
            {step === 1 && (
              <BasicsStep
                form={form}
                errors={fieldErrors}
                directions={directions}
                onChange={(patch) => setForm((prev) => ({ ...prev, ...patch }))}
              />
            )}
            {step === 2 && (
              <ProgramStep form={form} errors={fieldErrors} onChange={setForm} />
            )}
            {step === 3 && <InclusionsStep form={form} errors={fieldErrors} onChange={setForm} />}
            {step === 4 && (
              <StayStep
                form={form}
                errors={fieldErrors}
                onChange={(patch) => setForm((prev) => ({ ...prev, ...patch }))}
              />
            )}
            {step === 5 && <OffersStep form={form} errors={fieldErrors} onChange={setForm} />}
            {step === 6 && (
              <PhotosStep
                form={form}
                errors={fieldErrors}
                saving={saving}
                onUpload={onUpload}
                onChange={setForm}
              />
            )}
            {step === 7 && (
              <ReviewStep
                missing={missing}
                status={status}
                saving={saving}
                onPublish={publish}
                onUnpublish={unpublish}
              />
            )}

            <div className="flex flex-col-reverse gap-3 border-t border-sand pt-4 sm:flex-row sm:justify-between">
              <Button variant="ghost" onClick={() => moveToStep(step - 1, 'push')} disabled={step === 1 || saving}>
                Назад
              </Button>
              <div className="flex flex-col gap-3 sm:flex-row">
                {step < 7 && (
                  <Button variant="secondary" onClick={() => saveAnd(null)} disabled={saving} isLoading={saving}>
                    Сохранить черновик
                  </Button>
                )}
                {step < 7 && (
                  <Button onClick={() => saveAnd(step + 1)} disabled={saving} isLoading={saving}>
                    Далее
                  </Button>
                )}
              </div>
            </div>
          </Card>
        )}

        <p className="text-sm text-warm-gray">
          <Link to="/manager/tours" className="text-primary underline-offset-2 hover:underline">
            К списку туров
          </Link>
        </p>
      </div>
    </PageTransition>
  );
}

function FailureBanner({ failure, onReload }: { failure: ManageFailure; onReload?: () => void }) {
  const canReload = failure.status === 412 || failure.status === 428 || failure.status === 409;
  return (
    <div role="alert" className="rounded-[12px] border border-red-200 bg-red-50 p-4 text-sm text-red-700">
      <p>{failure.message}</p>
      {failure.missing.length > 0 && (
        <ul className="mt-2 list-disc space-y-1 pl-5">
          {failure.missing.map((code) => (
            <li key={code}>{tourManageErrorText(code)}</li>
          ))}
        </ul>
      )}
      {canReload && onReload && (
        <button type="button" className="mt-3 font-medium text-primary" onClick={onReload}>
          Загрузить актуальную версию
        </button>
      )}
    </div>
  );
}

function Field({
  id,
  label,
  error,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  children: ReactNode;
}) {
  return (
    <div>
      <label htmlFor={id} className="mb-1.5 block text-sm font-medium text-dark">
        {label}
      </label>
      {children}
      {error && <p className="mt-1 text-sm text-red-600">{error}</p>}
    </div>
  );
}

function BasicsStep({
  form,
  errors,
  directions,
  onChange,
}: {
  form: WizardForm;
  errors: Record<string, string>;
  directions: Destination[];
  onChange: (patch: Partial<WizardForm>) => void;
}) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Field id="tour-title" label="Название" error={errors.title}>
        <input id="tour-title" className={fieldClass} value={form.title} onChange={(event) => onChange({ title: event.target.value })} />
      </Field>
      <Field id="tour-short" label="Кратко" error={errors.shortDescription}>
        <input
          id="tour-short"
          className={fieldClass}
          value={form.shortDescription}
          onChange={(event) => onChange({ shortDescription: event.target.value })}
        />
      </Field>
      <div className="sm:col-span-2">
        <Field id="tour-description" label="Описание" error={errors.description}>
          <textarea
            id="tour-description"
            rows={5}
            className={fieldClass}
            value={form.description}
            onChange={(event) => onChange({ description: event.target.value })}
          />
        </Field>
      </div>
      <Field id="tour-departure" label="Город вылета" error={errors.departureCity}>
        <input
          id="tour-departure"
          className={fieldClass}
          value={form.departureCity}
          onChange={(event) => onChange({ departureCity: event.target.value })}
        />
      </Field>
      <Field id="tour-country" label="Страна" error={errors.country}>
        <input id="tour-country" className={fieldClass} value={form.country} onChange={(event) => onChange({ country: event.target.value })} />
      </Field>
      <Field id="tour-type" label="Тип тура" error={errors.tourType}>
        <select id="tour-type" className={fieldClass} value={form.tourType} onChange={(event) => onChange({ tourType: event.target.value })}>
          {TOUR_TYPE_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </Field>
      <Field id="tour-duration" label="Дней в туре" error={errors.durationDays}>
        <input
          id="tour-duration"
          inputMode="numeric"
          className={fieldClass}
          value={form.durationDays}
          onChange={(event) => onChange({ durationDays: event.target.value })}
        />
      </Field>
      <div className="sm:col-span-2">
        <Field id="tour-direction" label="Направление">
          <select
            id="tour-direction"
            className={fieldClass}
            value={form.directionId}
            onChange={(event) => onChange({ directionId: event.target.value })}
          >
            <option value="">Без направления</option>
            {directions.map((direction) => (
              <option key={direction.id} value={direction.id}>
                {direction.name}
              </option>
            ))}
          </select>
        </Field>
      </div>
    </div>
  );
}

function ProgramStep({
  form,
  errors,
  onChange,
}: {
  form: WizardForm;
  errors: Record<string, string>;
  onChange: (form: WizardForm) => void;
}) {
  if (form.days.length === 0) {
    return (
      <p className="text-sm text-warm-gray">
        Укажите длительность на шаге «Основное», чтобы разложить программу по дням. Пустую программу можно сохранить и вернуться позже.
      </p>
    );
  }

  return (
    <div className="space-y-4">
      {errors.days && <p className="text-sm text-red-600">{errors.days}</p>}
      {form.days.map((day) => (
        <div key={day.dayNumber} className="space-y-3 rounded-[12px] border border-sand p-4">
          <Field id={`day-${day.dayNumber}-title`} label={`День ${day.dayNumber}, заголовок`} error={errors[`day-${day.dayNumber}-title`]}>
            <input
              id={`day-${day.dayNumber}-title`}
              className={fieldClass}
              value={day.title}
              onChange={(event) =>
                onChange({
                  ...form,
                  days: form.days.map((item) =>
                    item.dayNumber === day.dayNumber ? { ...item, title: event.target.value } : item,
                  ),
                })
              }
            />
          </Field>
          <Field id={`day-${day.dayNumber}-text`} label={`День ${day.dayNumber}, описание`} error={errors[`day-${day.dayNumber}-text`]}>
            <textarea
              id={`day-${day.dayNumber}-text`}
              rows={3}
              className={fieldClass}
              value={day.description}
              onChange={(event) =>
                onChange({
                  ...form,
                  days: form.days.map((item) =>
                    item.dayNumber === day.dayNumber ? { ...item, description: event.target.value } : item,
                  ),
                })
              }
            />
          </Field>
        </div>
      ))}
    </div>
  );
}

function InclusionsStep({
  form,
  errors,
  onChange,
}: {
  form: WizardForm;
  errors: Record<string, string>;
  onChange: (form: WizardForm) => void;
}) {
  function add(kind: 'Included' | 'NotIncluded') {
    onChange({ ...form, inclusions: [...form.inclusions, { text: '', kind }] });
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        <Button type="button" variant="secondary" size="sm" leftIcon={<Plus size={16} />} onClick={() => add('Included')}>
          Добавить «включено»
        </Button>
        <Button type="button" variant="ghost" size="sm" leftIcon={<Plus size={16} />} onClick={() => add('NotIncluded')}>
          Добавить «не включено»
        </Button>
      </div>
      {form.inclusions.length === 0 && (
        <p className="text-sm text-warm-gray">Для публикации нужен хотя бы один пункт «включено».</p>
      )}
      {form.inclusions.map((item, index) => (
        <div key={`${item.kind}-${index}`} className="flex items-start gap-2">
          <div className="flex-1">
            <Field
              id={`inclusion-${index}`}
              label={item.kind === 'Included' ? `Включено ${index + 1}` : `Не включено ${index + 1}`}
              error={errors[`inclusion-${index}`]}
            >
              <input
                id={`inclusion-${index}`}
                className={fieldClass}
                value={item.text}
                onChange={(event) =>
                  onChange({
                    ...form,
                    inclusions: form.inclusions.map((row, rowIndex) =>
                      rowIndex === index ? { ...row, text: event.target.value } : row,
                    ),
                  })
                }
              />
            </Field>
          </div>
          <button
            type="button"
            className="mt-8 text-sm text-red-600"
            onClick={() => onChange({ ...form, inclusions: form.inclusions.filter((_, rowIndex) => rowIndex !== index) })}
          >
            Убрать
          </button>
        </div>
      ))}
    </div>
  );
}

function StayStep({
  form,
  errors,
  onChange,
}: {
  form: WizardForm;
  errors: Record<string, string>;
  onChange: (patch: Partial<WizardForm>) => void;
}) {
  return (
    <div className="space-y-4">
      <Field id="meal-plan" label="Питание" error={errors.mealPlan}>
        <select id="meal-plan" className={fieldClass} value={form.mealPlan} onChange={(event) => onChange({ mealPlan: event.target.value })}>
          <option value="">Не выбрано</option>
          {MEAL_PLANS.map((plan) => (
            <option key={plan} value={plan}>
              {mealPlanLabel(plan)}
            </option>
          ))}
        </select>
      </Field>
      <Field id="accommodation" label="Проживание" error={errors.accommodationText}>
        <textarea
          id="accommodation"
          rows={5}
          className={fieldClass}
          placeholder="Название отеля, район, что входит в номер"
          value={form.accommodationText}
          onChange={(event) => onChange({ accommodationText: event.target.value })}
        />
      </Field>
    </div>
  );
}

function OffersStep({
  form,
  errors,
  onChange,
}: {
  form: WizardForm;
  errors: Record<string, string>;
  onChange: (form: WizardForm) => void;
}) {
  return (
    <div className="space-y-4">
      <Button
        type="button"
        variant="secondary"
        size="sm"
        leftIcon={<Plus size={16} />}
        onClick={() =>
          onChange({
            ...form,
            offers: [
              ...form.offers,
              { validFrom: '', validTo: '', pricePerPerson: '', currency: 'RUB', availableSeats: '1' },
            ],
          })
        }
      >
        Добавить даты
      </Button>
      {form.offers.length === 0 && (
        <p className="text-sm text-warm-gray">Нужно хотя бы одно предложение с датой заезда в будущем.</p>
      )}
      {form.offers.map((offer, index) => (
        <div key={index} className="grid gap-3 rounded-[12px] border border-sand p-4 sm:grid-cols-2">
          <Field id={`offer-${index}-from`} label={`Заезд ${index + 1}`} error={errors[`offer-${index}`]}>
            <input
              id={`offer-${index}-from`}
              type="date"
              className={fieldClass}
              value={offer.validFrom}
              onChange={(event) =>
                onChange({
                  ...form,
                  offers: form.offers.map((row, rowIndex) =>
                    rowIndex === index ? { ...row, validFrom: event.target.value } : row,
                  ),
                })
              }
            />
          </Field>
          <Field id={`offer-${index}-to`} label={`Выезд ${index + 1}`}>
            <input
              id={`offer-${index}-to`}
              type="date"
              className={fieldClass}
              value={offer.validTo}
              onChange={(event) =>
                onChange({
                  ...form,
                  offers: form.offers.map((row, rowIndex) =>
                    rowIndex === index ? { ...row, validTo: event.target.value } : row,
                  ),
                })
              }
            />
          </Field>
          <Field id={`offer-${index}-price`} label="Цена за человека" error={errors[`offer-${index}-price`]}>
            <input
              id={`offer-${index}-price`}
              inputMode="decimal"
              className={fieldClass}
              value={offer.pricePerPerson}
              onChange={(event) =>
                onChange({
                  ...form,
                  offers: form.offers.map((row, rowIndex) =>
                    rowIndex === index ? { ...row, pricePerPerson: event.target.value } : row,
                  ),
                })
              }
            />
          </Field>
          <Field id={`offer-${index}-currency`} label="Валюта" error={errors[`offer-${index}-currency`]}>
            <input
              id={`offer-${index}-currency`}
              className={fieldClass}
              value={offer.currency}
              onChange={(event) =>
                onChange({
                  ...form,
                  offers: form.offers.map((row, rowIndex) =>
                    rowIndex === index ? { ...row, currency: event.target.value } : row,
                  ),
                })
              }
            />
          </Field>
          <Field id={`offer-${index}-seats`} label="Мест" error={errors[`offer-${index}-seats`]}>
            <input
              id={`offer-${index}-seats`}
              inputMode="numeric"
              className={fieldClass}
              value={offer.availableSeats}
              onChange={(event) =>
                onChange({
                  ...form,
                  offers: form.offers.map((row, rowIndex) =>
                    rowIndex === index ? { ...row, availableSeats: event.target.value } : row,
                  ),
                })
              }
            />
          </Field>
          <div className="flex items-end">
            <button
              type="button"
              className="text-sm text-red-600"
              onClick={() => onChange({ ...form, offers: form.offers.filter((_, rowIndex) => rowIndex !== index) })}
            >
              Убрать даты
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}

function PhotosStep({
  form,
  errors,
  saving,
  onUpload,
  onChange,
}: {
  form: WizardForm;
  errors: Record<string, string>;
  saving: boolean;
  onUpload: (file: File | undefined) => void;
  onChange: (form: WizardForm) => void;
}) {
  function move(index: number, delta: number) {
    const target = index + delta;
    if (target < 0 || target >= form.images.length) return;
    const images = [...form.images];
    const [item] = images.splice(index, 1);
    images.splice(target, 0, item);
    onChange({ ...form, images: images.map((image, sortOrder) => ({ ...image, sortOrder })) });
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-warm-gray">
        JPEG, PNG или WebP, до 10 МБ и не больше 20 фото. Обложка — от 1280 пикселей по ширине. Превью черновика
        открывается по служебной ссылке и появится в каталоге после публикации.
      </p>
      <Field id="tour-photo" label="Загрузить фото">
        <input
          id="tour-photo"
          aria-label="Загрузить фото"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          disabled={saving}
          className={fieldClass}
          onChange={(event) => {
            const file = event.target.files?.[0];
            event.target.value = '';
            onUpload(file);
          }}
        />
      </Field>
      {errors.images && <p className="text-sm text-red-600">{errors.images}</p>}
      <ul className="grid gap-3 sm:grid-cols-2">
        {form.images.map((image, index) => {
          const narrow = image.isCover && (image.widthPx === null || image.widthPx < 1280);
          return (
            <li key={image.mediaFileId} className="space-y-2 rounded-[12px] border border-sand p-3">
              <img
                src={manageMediaFileUrl(image.mediaFileId, 'w200')}
                alt={image.alt.trim() || 'Фото тура'}
                className="h-28 w-full rounded-[12px] object-cover"
              />
              {image.isCover && <p className="text-xs font-medium text-primary">Обложка</p>}
              {narrow && <p className="text-sm text-red-600">{tourManageErrorText('images.coverMinWidth')}</p>}
              <Field id={`image-${index}-alt`} label="Подпись" error={errors[`image-${index}-alt`]}>
                <input
                  id={`image-${index}-alt`}
                  className={fieldClass}
                  value={image.alt}
                  onChange={(event) =>
                    onChange({
                      ...form,
                      images: form.images.map((item, itemIndex) =>
                        itemIndex === index ? { ...item, alt: event.target.value } : item,
                      ),
                    })
                  }
                />
              </Field>
              <div className="flex flex-wrap gap-2">
                {!image.isCover && (
                  <button
                    type="button"
                    className="text-sm font-medium text-primary"
                    onClick={() =>
                      onChange({
                        ...form,
                        images: form.images.map((item) => ({ ...item, isCover: item.mediaFileId === image.mediaFileId })),
                      })
                    }
                  >
                    Сделать обложкой
                  </button>
                )}
                <button type="button" className="rounded-lg p-1 text-dark hover:bg-sand" aria-label="Раньше" onClick={() => move(index, -1)}>
                  <ArrowUp size={16} />
                </button>
                <button type="button" className="rounded-lg p-1 text-dark hover:bg-sand" aria-label="Позже" onClick={() => move(index, 1)}>
                  <ArrowDown size={16} />
                </button>
                <button
                  type="button"
                  className="text-sm text-red-600"
                  onClick={() =>
                    onChange({
                      ...form,
                      images: form.images.filter((item) => item.mediaFileId !== image.mediaFileId),
                    })
                  }
                >
                  Убрать
                </button>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

function ReviewStep({
  missing,
  status,
  saving,
  onPublish,
  onUnpublish,
}: {
  missing: string[];
  status: string;
  saving: boolean;
  onPublish: () => void;
  onUnpublish: () => void;
}) {
  return (
    <div className="space-y-4">
      <div>
        <h3 className="text-sm font-medium text-dark">Чего не хватает для публикации</h3>
        {missing.length === 0 ? (
          <p className="mt-2 text-sm text-olive-dark">Сейчас обязательные пункты на месте. Можно публиковать.</p>
        ) : (
          <ul aria-label="Чего не хватает для публикации" className="mt-2 list-disc space-y-1 pl-5 text-sm text-dark">
            {missing.map((code) => (
              <li key={code}>{tourManageErrorText(code)}</li>
            ))}
          </ul>
        )}
      </div>
      <div className="flex flex-wrap items-center gap-3">
        {status === 'Published' ? (
          <p role="status" className="text-sm font-medium text-dark">
            {tourStatusLabel(status)}
          </p>
        ) : (
          <Button onClick={onPublish} disabled={saving} isLoading={saving}>
            Опубликовать
          </Button>
        )}
        {status === 'Published' && (
          <Button variant="secondary" onClick={onUnpublish} disabled={saving}>
            Снять с публикации
          </Button>
        )}
      </div>
    </div>
  );
}
