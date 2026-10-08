export const TOUR_TYPE_OPTIONS = [
  { value: 'Beach', label: 'Пляж' },
  { value: 'Mountain', label: 'Горы' },
  { value: 'City', label: 'Город' },
  { value: 'Cultural', label: 'Культура' },
  { value: 'Adventure', label: 'Приключения' },
  { value: 'Cruise', label: 'Круиз' },
  { value: 'Safari', label: 'Сафари' },
] as const;

const TOUR_TYPE_LABELS: Record<string, string> = {
  Beach: 'Пляж',
  '0': 'Пляж',
  Mountain: 'Горы',
  '1': 'Горы',
  City: 'Город',
  '2': 'Город',
  Cultural: 'Культура',
  '3': 'Культура',
  Adventure: 'Приключения',
  '4': 'Приключения',
  Cruise: 'Круиз',
  '5': 'Круиз',
  Safari: 'Сафари',
  '6': 'Сафари',
};

const MEAL_PLAN_LABELS: Record<string, string> = {
  RO: 'Без питания',
  '0': 'Без питания',
  BB: 'Завтраки',
  '1': 'Завтраки',
  HB: 'Завтрак и ужин',
  '2': 'Завтрак и ужин',
  FB: 'Полный пансион',
  '3': 'Полный пансион',
  AI: 'Всё включено',
  '4': 'Всё включено',
  UAI: 'Ультра всё включено',
  '5': 'Ультра всё включено',
};

export function tourTypeLabel(value: string | number | null | undefined): string {
  if (value === null || value === undefined || value === '') return '';
  return TOUR_TYPE_LABELS[String(value)] ?? String(value);
}

export function mealPlanLabel(value: string | number | null | undefined): string | null {
  if (value === null || value === undefined || value === '') return null;
  return MEAL_PLAN_LABELS[String(value)] ?? String(value);
}

export function isTourAvailable(tour: { available?: boolean }): boolean {
  return tour.available !== false;
}

const TOUR_STATUS_LABELS: Record<string, string> = {
  Draft: 'Черновик',
  Published: 'Опубликован',
  Unpublished: 'Снят с публикации',
};

export function tourStatusLabel(status: string | null | undefined): string {
  if (!status) return '';
  return TOUR_STATUS_LABELS[status] ?? status;
}
