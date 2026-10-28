import { isAllowedLanguage } from '@/domain/allowedLanguages';

export const AVATAR_ACCEPT = 'image/jpeg,image/png,image/webp';
export const AVATAR_MAX_BYTES = 5 * 1024 * 1024;
export const AVATAR_UPLOAD_ERROR = 'Не удалось загрузить аватар. Попробуйте ещё раз.';
export const PASSWORD_HINT = 'Минимум 8 символов, заглавная, строчная буква и цифра';

const AVATAR_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);
const PHONE_E164 = /^\+[1-9]\d{1,14}$/;

const NAME_ERROR = 'Укажите имя длиной от 1 до 100 символов.';
const LAST_NAME_ERROR = 'Укажите фамилию длиной от 1 до 100 символов.';
const PHONE_ERROR = 'Телефон должен быть в формате E.164, например +79991234567, или пустым.';
const CITY_ERROR = 'Город не длиннее 100 символов.';
const AVATAR_TYPE_ERROR = 'Подходят JPG, PNG или WebP.';
const AVATAR_SIZE_ERROR = 'Файл больше 5 МБ.';
const ABOUT_LIMIT = 1000;
const SPECIALIZATION_LIMIT = 300;
const MIN_TOURISM_YEAR = 1950;
const ABOUT_ERROR = 'Текст «О себе» не длиннее 1000 символов.';
const YEAR_ERROR = 'Укажите год не позже текущего.';
const SPECIALIZATION_ERROR = 'Специализация не длиннее 300 символов.';

export type ProfileField =
  | 'firstName'
  | 'lastName'
  | 'phone'
  | 'city'
  | 'about'
  | 'tourismSince'
  | 'specialization';

export type ProfileFieldErrors = Partial<Record<ProfileField, string>> & {
  languages?: string[];
};

export type ProfileCardInput = {
  firstName: string;
  lastName: string;
  phone: string;
  city: string;
  about?: string;
  tourismSince?: string;
  languages?: string[];
  specialization?: string;
};

export type ProfileCardPayload = {
  firstName: string;
  lastName: string;
  phone: string;
  city: string;
  about?: string;
  tourismSince?: number;
  languages?: string[];
  specialization?: string;
};

export function aboutCharactersLeft(value: string): number {
  return Math.max(0, ABOUT_LIMIT - value.length);
}

function languageError(code: string): string {
  return `Язык «${code}» не из списка ISO 639-1.`;
}

function normalizeLanguages(codes: string[]): { languages: string[]; errors: string[] } {
  const languages: string[] = [];
  const errors: string[] = [];
  const seen = new Set<string>();
  for (const raw of codes) {
    const trimmed = raw.trim();
    if (!trimmed) continue;
    const code = trimmed.toLowerCase();
    if (!isAllowedLanguage(code)) {
      errors.push(languageError(trimmed));
      continue;
    }
    if (seen.has(code)) continue;
    seen.add(code);
    languages.push(code);
  }
  return { languages, errors };
}

function tourismYear(value: string): { year?: number; error?: string } {
  const trimmed = value.trim();
  if (!trimmed) return {};
  if (!/^\d+$/.test(trimmed)) return { error: YEAR_ERROR };
  const year = Number(trimmed);
  if (year < MIN_TOURISM_YEAR || year > new Date().getUTCFullYear()) return { error: YEAR_ERROR };
  return { year };
}

export function profileCardPayload(
  form: ProfileCardInput,
  options?: { includeManagerFields?: boolean },
): {
  payload?: ProfileCardPayload;
  errors: ProfileFieldErrors;
} {
  const firstName = form.firstName.trim();
  const lastName = form.lastName.trim();
  const phone = form.phone.trim();
  const city = form.city.trim();
  const errors: ProfileFieldErrors = {};

  if (firstName.length < 1 || firstName.length > 100) errors.firstName = NAME_ERROR;
  if (lastName.length < 1 || lastName.length > 100) errors.lastName = LAST_NAME_ERROR;
  if (phone.length > 0 && !PHONE_E164.test(phone)) errors.phone = PHONE_ERROR;
  if (city.length > 100) errors.city = CITY_ERROR;

  let manager:
    | {
        about: string;
        specialization: string;
        languages: string[];
        tourismSince?: number;
      }
    | undefined;

  if (options?.includeManagerFields) {
    const about = (form.about ?? '').trim();
    const specialization = (form.specialization ?? '').trim();
    if (about.length > ABOUT_LIMIT) errors.about = ABOUT_ERROR;
    if (specialization.length > SPECIALIZATION_LIMIT) errors.specialization = SPECIALIZATION_ERROR;
    const year = tourismYear(form.tourismSince ?? '');
    if (year.error) errors.tourismSince = year.error;
    const languages = normalizeLanguages(form.languages ?? []);
    if (languages.errors.length > 0) errors.languages = languages.errors;
    manager = {
      about,
      specialization,
      languages: languages.languages,
      ...(year.year !== undefined ? { tourismSince: year.year } : {}),
    };
  }

  if (
    errors.firstName ||
    errors.lastName ||
    errors.phone ||
    errors.city ||
    errors.about ||
    errors.tourismSince ||
    errors.specialization ||
    (errors.languages && errors.languages.length > 0)
  ) {
    return { errors };
  }

  const payload: ProfileCardPayload = { firstName, lastName, phone, city };
  if (manager) {
    payload.about = manager.about;
    payload.specialization = manager.specialization;
    payload.languages = manager.languages;
    if (manager.tourismSince !== undefined) payload.tourismSince = manager.tourismSince;
  }
  return { payload, errors };
}

export function avatarFileError(file: File): string | undefined {
  if (file.size === 0 || !AVATAR_TYPES.has(file.type)) return AVATAR_TYPE_ERROR;
  if (file.size > AVATAR_MAX_BYTES) return AVATAR_SIZE_ERROR;
  return undefined;
}

function responseData(error: unknown): unknown {
  if (typeof error !== 'object' || error === null || !('response' in error)) return undefined;
  const response = (error as { response?: unknown }).response;
  if (typeof response !== 'object' || response === null || !('data' in response)) return undefined;
  return (response as { data?: unknown }).data;
}

function texts(value: unknown): string[] {
  if (typeof value === 'string') {
    const trimmed = value.trim();
    return trimmed ? [trimmed] : [];
  }
  if (!Array.isArray(value)) return [];
  const result: string[] = [];
  for (const item of value) {
    if (typeof item === 'string' && item.trim()) result.push(item.trim());
  }
  return result;
}

export function profileProblemFields(
  error: unknown,
): ProfileFieldErrors & { file?: string } {
  const data = responseData(error);
  if (typeof data !== 'object' || data === null || !('errors' in data)) return {};
  const errors = (data as { errors?: unknown }).errors;
  if (typeof errors !== 'object' || errors === null) return {};
  const source = errors as Record<string, unknown>;
  const result: ProfileFieldErrors & { file?: string } = {};
  for (const key of [
    'firstName',
    'lastName',
    'phone',
    'city',
    'about',
    'tourismSince',
    'specialization',
    'file',
  ] as const) {
    const text = texts(source[key])[0];
    if (text) result[key] = text;
  }
  const languageTexts = texts(source.languages);
  if (languageTexts.length > 0) result.languages = languageTexts;
  return result;
}
