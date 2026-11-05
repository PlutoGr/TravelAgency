export const POSITION_MAX = 60;
export const ABOUT_MAX = 1000;
export const TAG_MAX = 30;
export const TAGS_MAX = 10;
export const TOURISM_SINCE_MIN = 1980;

export const POSITION_LENGTH_ERROR = 'Должность не длиннее 60 символов.';
export const TOURISM_SINCE_ERROR = 'Укажите год с 1980 по текущий.';
export const ABOUT_LENGTH_ERROR = 'Текст «О себе» не длиннее 1000 символов.';
export const TAG_LENGTH_ERROR = 'Не длиннее 30 символов.';
export const TAGS_COUNT_ERROR = 'Можно не больше 10.';
export const TAG_DUPLICATE_ERROR = 'Такой тег уже есть.';
export const TAG_EMPTY_ERROR = 'Укажите тег.';

export type ManagerCardField = 'position' | 'tourismSince' | 'about' | 'tags';

export type ManagerCardFieldErrors = Partial<Record<ManagerCardField, string>>;

export type ManagerCardDraft = {
  position: string;
  tourismSince: number | null;
  about: string;
  tags: string[];
};

export type ManagerCardWrite = {
  position: string | null;
  tourismSince: number | null;
  about: string | null;
  tags: string[];
};

export function tourismYearCeiling(now = new Date()): number {
  return now.getUTCFullYear();
}

export function tourismYears(now = new Date()): number[] {
  const years: number[] = [];
  for (let year = tourismYearCeiling(now); year >= TOURISM_SINCE_MIN; year -= 1) years.push(year);
  return years;
}

function pluralYears(count: number): string {
  const mod10 = count % 10;
  const mod100 = count % 100;
  if (mod10 === 1 && mod100 !== 11) return 'год';
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 'года';
  return 'лет';
}

export function tenureLabel(since: number | null | undefined, now = new Date()): string | null {
  if (since == null) return null;
  const years = tourismYearCeiling(now) - since;
  if (years < 1) return 'Стаж меньше года';
  return `Стаж ${years} ${pluralYears(years)}`;
}

export function normalizeTag(raw: string): string {
  return raw.trim().replace(/\s+/g, ' ');
}

export function tagProblem(raw: string, tags: string[]): string | undefined {
  const tag = normalizeTag(raw);
  if (!tag) return TAG_EMPTY_ERROR;
  if (tag.length > TAG_MAX) return TAG_LENGTH_ERROR;
  if (tags.length >= TAGS_MAX) return TAGS_COUNT_ERROR;
  if (tags.some((item) => item.localeCompare(tag, undefined, { sensitivity: 'accent' }) === 0)) {
    return TAG_DUPLICATE_ERROR;
  }
  return undefined;
}

export function managerCardPayload(
  draft: ManagerCardDraft,
  now = new Date(),
): { payload?: ManagerCardWrite; errors: ManagerCardFieldErrors } {
  const position = draft.position.trim();
  const about = draft.about.trim();
  const errors: ManagerCardFieldErrors = {};
  if (position.length > POSITION_MAX) errors.position = POSITION_LENGTH_ERROR;
  const ceiling = tourismYearCeiling(now);
  if (
    draft.tourismSince != null &&
    (!Number.isInteger(draft.tourismSince) || draft.tourismSince < TOURISM_SINCE_MIN || draft.tourismSince > ceiling)
  ) {
    errors.tourismSince = TOURISM_SINCE_ERROR;
  }
  if (about.length > ABOUT_MAX) errors.about = ABOUT_LENGTH_ERROR;
  if (draft.tags.length > TAGS_MAX) errors.tags = TAGS_COUNT_ERROR;

  if (errors.position || errors.tourismSince || errors.about || errors.tags) return { errors };

  return {
    payload: {
      position: position.length > 0 ? position : null,
      tourismSince: draft.tourismSince,
      about: about.length > 0 ? about : null,
      tags: draft.tags,
    },
    errors,
  };
}

export function sameManagerCard(left: ManagerCardWrite, right: ManagerCardWrite): boolean {
  return (
    left.position === right.position &&
    left.tourismSince === right.tourismSince &&
    left.about === right.about &&
    left.tags.length === right.tags.length &&
    left.tags.every((tag, index) => tag === right.tags[index])
  );
}

function responseData(error: unknown): unknown {
  if (typeof error !== 'object' || error === null || !('response' in error)) return undefined;
  const response = (error as { response?: unknown }).response;
  if (typeof response !== 'object' || response === null || !('data' in response)) return undefined;
  return (response as { data?: unknown }).data;
}

function firstText(value: unknown): string | undefined {
  if (typeof value === 'string' && value.trim()) return value.trim();
  if (!Array.isArray(value)) return undefined;
  for (const item of value) {
    if (typeof item === 'string' && item.trim()) return item.trim();
  }
  return undefined;
}

export function managerCardProblemFields(error: unknown): ManagerCardFieldErrors {
  const data = responseData(error);
  if (typeof data !== 'object' || data === null || !('errors' in data)) return {};
  const errors = (data as { errors?: unknown }).errors;
  if (typeof errors !== 'object' || errors === null) return {};
  const source = errors as Record<string, unknown>;
  const result: ManagerCardFieldErrors = {};
  for (const key of ['position', 'tourismSince', 'about', 'tags'] as const) {
    const text = firstText(source[key]);
    if (text) result[key] = text;
  }
  return result;
}
