import type { ManagedTour } from '@/api/tourManage';

export const COVER_MIN_WIDTH_PX = 1280;
export const MIN_IMAGES_TO_PUBLISH = 3;
export const MAX_IMAGES = 20;

export interface WizardImage {
  mediaFileId: string;
  alt: string;
  isCover: boolean;
  sortOrder: number;
  widthPx: number | null;
}

export interface WizardDay {
  dayNumber: number;
  title: string;
  description: string;
}

export interface WizardInclusion {
  text: string;
  kind: 'Included' | 'NotIncluded';
}

export interface WizardOffer {
  validFrom: string;
  validTo: string;
  pricePerPerson: string;
  currency: string;
  availableSeats: string;
}

export interface WizardForm {
  title: string;
  shortDescription: string;
  description: string;
  departureCity: string;
  country: string;
  tourType: string;
  durationDays: string;
  directionId: string;
  days: WizardDay[];
  inclusions: WizardInclusion[];
  mealPlan: string;
  accommodationText: string;
  offers: WizardOffer[];
  images: WizardImage[];
}

const TOUR_TYPES = new Set(['Beach', 'Mountain', 'City', 'Cultural', 'Adventure', 'Cruise', 'Safari']);
const MEAL_PLANS = new Set(['RO', 'BB', 'HB', 'FB', 'AI', 'UAI']);

export function emptyWizardForm(): WizardForm {
  return {
    title: '',
    shortDescription: '',
    description: '',
    departureCity: '',
    country: '',
    tourType: 'Beach',
    durationDays: '',
    directionId: '',
    days: [],
    inclusions: [],
    mealPlan: '',
    accommodationText: '',
    offers: [],
    images: [],
  };
}

function dateInput(value: string | null | undefined): string {
  if (!value) return '';
  return value.slice(0, 10);
}

function alignDays(days: WizardDay[], duration: number): WizardDay[] {
  if (duration < 1) return days;
  const byNumber = new Map(days.map((day) => [day.dayNumber, day]));
  return Array.from({ length: duration }, (_, index) => {
    const dayNumber = index + 1;
    return byNumber.get(dayNumber) ?? { dayNumber, title: '', description: '' };
  });
}

export function formFromTour(tour: ManagedTour): WizardForm {
  const duration = tour.durationDays ?? 0;
  const days = [...(tour.days ?? [])]
    .sort((a, b) => a.dayNumber - b.dayNumber)
    .map((day) => ({
      dayNumber: day.dayNumber,
      title: day.title ?? '',
      description: day.description ?? '',
    }));
  const images = [...(tour.images ?? [])]
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((image, index) => ({
      mediaFileId: image.mediaFileId,
      alt: image.alt ?? '',
      isCover: image.isCover,
      sortOrder: index,
      widthPx: image.widthPx ?? null,
    }));

  return {
    title: tour.title ?? '',
    shortDescription: tour.shortDescription ?? '',
    description: tour.description ?? '',
    departureCity: tour.departureCity ?? '',
    country: tour.country ?? '',
    tourType: tour.tourType || 'Beach',
    durationDays: duration > 0 ? String(duration) : '',
    directionId: tour.directionId ?? '',
    days: alignDays(days, duration),
    inclusions: [...(tour.inclusions ?? [])]
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map((item) => ({
        text: item.text ?? '',
        kind: item.kind === 'Included' ? 'Included' : 'NotIncluded',
      })),
    mealPlan: tour.mealPlan ?? '',
    accommodationText: tour.accommodationText ?? '',
    offers: [...(tour.offers ?? [])].map((offer) => ({
      validFrom: dateInput(offer.validFrom),
      validTo: dateInput(offer.validTo),
      pricePerPerson: String(offer.pricePerPerson ?? ''),
      currency: offer.currency || 'RUB',
      availableSeats: String(offer.availableSeats ?? 0),
    })),
    images,
  };
}

export function durationValue(form: WizardForm): number {
  if (form.durationDays.trim() === '') return 0;
  const value = Number(form.durationDays);
  return Number.isInteger(value) ? value : Number.NaN;
}

function parseInstant(value: string): Date {
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) return new Date(`${value}T00:00:00Z`);
  return new Date(value);
}

function completeDays(form: WizardForm): WizardDay[] {
  return form.days.filter((day) => day.title.trim() && day.description.trim());
}

function hasExactProgram(form: WizardForm): boolean {
  const duration = durationValue(form);
  if (!Number.isInteger(duration) || duration < 1) return false;
  const days = completeDays(form);
  if (days.length !== duration) return false;
  const numbers = new Set(days.map((day) => day.dayNumber));
  if (numbers.size !== duration) return false;
  for (let day = 1; day <= duration; day += 1) {
    if (!numbers.has(day)) return false;
  }
  return true;
}

/** Те же коды, что Catalog.GetMissingPublishRequirements. Ширина обложки берётся из метаданных файла. */
export function missingPublishCodes(form: WizardForm, now: Date): string[] {
  const missing: string[] = [];
  if (!form.title.trim()) missing.push('title');
  if (!form.shortDescription.trim()) missing.push('shortDescription');
  if (!form.description.trim()) missing.push('description');
  if (!hasExactProgram(form)) missing.push('program.dayCount');
  if (!form.inclusions.some((item) => item.kind === 'Included' && item.text.trim())) {
    missing.push('inclusions.included');
  }
  if (!form.mealPlan) missing.push('mealPlan');
  if (!form.accommodationText.trim()) missing.push('accommodation');
  const hasFuture = form.offers.some((offer) => {
    const start = parseInstant(offer.validFrom);
    return !Number.isNaN(start.getTime()) && start > now;
  });
  if (!hasFuture) missing.push('offers.future');

  const cover = form.images.find((image) => image.isCover);
  if (!cover) missing.push('images.cover');
  if (form.images.length < MIN_IMAGES_TO_PUBLISH) missing.push('images.minCount');
  const width = cover?.widthPx ?? null;
  if (width === null || width < COVER_MIN_WIDTH_PX) missing.push('images.coverMinWidth');
  return missing;
}

export function validateWizardStep(step: number, form: WizardForm): Record<string, string> {
  const errors: Record<string, string> = {};

  if (step === 1) {
    if (form.title.trim().length > 200) errors.title = 'Название не длиннее 200 символов.';
    if (form.shortDescription.trim().length > 300) errors.shortDescription = 'Краткое описание не длиннее 300 символов.';
    if (form.description.trim().length > 2000) errors.description = 'Описание не длиннее 2000 символов.';
    if (form.departureCity.trim().length > 100) errors.departureCity = 'Город вылета не длиннее 100 символов.';
    if (form.country.trim().length > 100) errors.country = 'Страна не длиннее 100 символов.';
    if (form.tourType && !TOUR_TYPES.has(form.tourType)) errors.tourType = 'Выберите тип тура из списка.';
    const duration = durationValue(form);
    if (form.durationDays.trim() !== '' && (!Number.isInteger(duration) || duration < 0)) {
      errors.durationDays = 'Длительность — целое число от 0.';
    }
  }

  if (step === 2) {
    const numbers = form.days.map((day) => day.dayNumber);
    if (new Set(numbers).size !== numbers.length) errors.days = 'Номера дней не должны повторяться.';
    form.days.forEach((day) => {
      const started = day.title.trim() || day.description.trim();
      if (!started) return;
      if (!day.title.trim()) errors[`day-${day.dayNumber}-title`] = 'Укажите заголовок дня.';
      if (day.title.trim().length > 200) errors[`day-${day.dayNumber}-title`] = 'Заголовок дня не длиннее 200 символов.';
      if (!day.description.trim()) errors[`day-${day.dayNumber}-text`] = 'Опишите день.';
      if (day.description.trim().length > 4000) {
        errors[`day-${day.dayNumber}-text`] = 'Описание дня не длиннее 4000 символов.';
      }
    });
  }

  if (step === 3) {
    form.inclusions.forEach((item, index) => {
      if (!item.text.trim()) errors[`inclusion-${index}`] = 'Пункт не может быть пустым.';
      if (item.text.trim().length > 500) errors[`inclusion-${index}`] = 'Пункт не длиннее 500 символов.';
    });
  }

  if (step === 4) {
    if (form.mealPlan && !MEAL_PLANS.has(form.mealPlan)) errors.mealPlan = 'Выберите питание из списка.';
    if (form.accommodationText.trim().length > 4000) errors.accommodationText = 'Проживание не длиннее 4000 символов.';
  }

  if (step === 5) {
    form.offers.forEach((offer, index) => {
      const prefix = `offer-${index}`;
      if (!offer.validFrom || !offer.validTo) errors[prefix] = 'Укажите даты заезда и выезда.';
      else if (offer.validFrom >= offer.validTo) errors[prefix] = 'Дата заезда должна быть раньше даты выезда.';
      const price = Number(offer.pricePerPerson);
      if (!(price > 0)) errors[`${prefix}-price`] = 'Цена за человека должна быть больше нуля.';
      if (!/^[A-Za-z]{3}$/.test(offer.currency.trim())) errors[`${prefix}-currency`] = 'Валюта — три буквы, например RUB.';
      const seats = Number(offer.availableSeats);
      if (!Number.isInteger(seats) || seats < 0) errors[`${prefix}-seats`] = 'Число мест — целое, от нуля.';
    });
  }

  if (step === 6) {
    if (form.images.length > MAX_IMAGES) errors.images = 'В туре не больше 20 фото.';
    if (form.images.filter((image) => image.isCover).length > 1) errors.images = 'Обложка может быть только одна.';
    const ids = form.images.map((image) => image.mediaFileId);
    if (new Set(ids).size !== ids.length) errors.images = 'Одно и то же фото нельзя добавить дважды.';
    form.images.forEach((image, index) => {
      if (image.alt.trim().length > 300) errors[`image-${index}-alt`] = 'Подпись не длиннее 300 символов.';
    });
  }

  return errors;
}
