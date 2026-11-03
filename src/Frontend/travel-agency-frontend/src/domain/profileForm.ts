export const AVATAR_ACCEPT = 'image/jpeg,image/png,image/webp';
export const AVATAR_MAX_BYTES = 5 * 1024 * 1024;
export const AVATAR_UPLOAD_ERROR = 'Не удалось загрузить аватар. Попробуйте ещё раз.';
export const AVATAR_OPEN_ERROR = 'Не удалось открыть фото. Попробуйте ещё раз.';
export const PASSWORD_HINT = 'Минимум 8 символов, заглавная, строчная буква и цифра';

const AVATAR_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);
const PHONE_E164 = /^\+[1-9]\d{1,14}$/;
const CYRILLIC = /[\u0400-\u04FF]/;

const NAME_ERROR = 'Укажите имя длиной от 1 до 100 символов.';
const LAST_NAME_ERROR = 'Укажите фамилию длиной от 1 до 100 символов.';
const PHONE_ERROR = 'Телефон должен быть в формате E.164, например +79991234567, или пустым.';
export const AVATAR_TYPE_ERROR = 'Подходят JPG, PNG или WebP.';
export const AVATAR_SIZE_ERROR = 'Файл больше 5 МБ.';
export const AVATAR_RATIO_ERROR =
  'Фото слишком вытянутое. Длинная сторона не должна быть больше короткой более чем в 3 раза.';

export type ProfileField = 'firstName' | 'lastName' | 'phone';

export type ProfileFieldErrors = Partial<Record<ProfileField, string>>;

export type ProfileCardInput = {
  firstName: string;
  lastName: string;
  phone: string;
};

export type ProfileCardPayload = {
  firstName: string;
  lastName: string;
  phone: string;
};

export function profileCardPayload(form: ProfileCardInput): {
  payload?: ProfileCardPayload;
  errors: ProfileFieldErrors;
} {
  const firstName = form.firstName.trim();
  const lastName = form.lastName.trim();
  const phone = form.phone.trim();
  const errors: ProfileFieldErrors = {};

  if (firstName.length < 1 || firstName.length > 100) errors.firstName = NAME_ERROR;
  if (lastName.length < 1 || lastName.length > 100) errors.lastName = LAST_NAME_ERROR;
  if (phone.length > 0 && !PHONE_E164.test(phone)) errors.phone = PHONE_ERROR;

  if (errors.firstName || errors.lastName || errors.phone) return { errors };

  return { payload: { firstName, lastName, phone }, errors };
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

/** Первая непустая кириллическая строка из `errors` (порядок ключей ответа), затем из `detail`. */
export function avatarUploadMessage(error: unknown): string {
  const data = responseData(error);
  return firstCyrillic(data) ?? AVATAR_UPLOAD_ERROR;
}

function firstCyrillic(data: unknown): string | undefined {
  if (typeof data !== 'object' || data === null) return undefined;
  if ('errors' in data) {
    const errors = (data as { errors?: unknown }).errors;
    if (typeof errors === 'object' && errors !== null && !Array.isArray(errors)) {
      for (const value of Object.values(errors)) {
        for (const text of texts(value)) {
          if (CYRILLIC.test(text)) return text;
        }
      }
    }
  }
  if ('detail' in data) {
    for (const text of texts((data as { detail?: unknown }).detail)) {
      if (CYRILLIC.test(text)) return text;
    }
  }
  return undefined;
}

export function profileProblemFields(error: unknown): ProfileFieldErrors & { file?: string } {
  const data = responseData(error);
  if (typeof data !== 'object' || data === null || !('errors' in data)) return {};
  const errors = (data as { errors?: unknown }).errors;
  if (typeof errors !== 'object' || errors === null) return {};
  const source = errors as Record<string, unknown>;
  const result: ProfileFieldErrors & { file?: string } = {};
  for (const key of ['firstName', 'lastName', 'phone', 'file'] as const) {
    const text = texts(source[key])[0];
    if (text) result[key] = text;
  }
  return result;
}
