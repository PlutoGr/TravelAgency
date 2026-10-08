/** Коды, которые Catalog кладёт в ProblemDetails (`missing` или `code`) на управлении и публикации. */
export const TOUR_MANAGE_API_CODES = [
  'title',
  'shortDescription',
  'description',
  'program.dayCount',
  'inclusions.included',
  'mealPlan',
  'accommodation',
  'offers.future',
  'images.cover',
  'images.minCount',
  'images.coverMinWidth',
  'images.owner',
  'images.notFound',
] as const;

export type TourManageApiCode = (typeof TOUR_MANAGE_API_CODES)[number];

export const TOUR_MANAGE_ERROR_TEXT: Record<TourManageApiCode, string> = {
  title: 'Укажите название тура.',
  shortDescription: 'Добавьте краткое описание.',
  description: 'Добавьте полное описание.',
  'program.dayCount': 'В программе должно быть ровно столько дней, сколько длится тур.',
  'inclusions.included': 'Добавьте хотя бы один пункт в раздел «Включено».',
  mealPlan: 'Выберите питание.',
  accommodation: 'Опишите проживание.',
  'offers.future': 'Добавьте хотя бы одно предложение с датой заезда в будущем.',
  'images.cover': 'Выберите одну обложку.',
  'images.minCount': 'Для публикации нужно не меньше 3 фото.',
  'images.coverMinWidth': 'Обложка должна быть не уже 1280 пикселей. Обычное фото той же ширины можно оставить в галерее.',
  'images.owner': 'Фото принадлежит другому пользователю. Загрузите свои файлы.',
  'images.notFound': 'Фото не найдено. Загрузите его ещё раз.',
};

export const TOUR_MANAGE_STATUS_TEXT = {
  fileTooLarge: 'Файл больше 10 МБ.',
  preconditionFailed: 'Тур уже изменили в другой сессии. Загрузите актуальную версию и повторите шаг.',
  preconditionRequired: 'Сервер не принял шаг: нет версии тура (If-Match). Обновите страницу и повторите.',
  versionConflict: 'Тур уже изменили. Загрузите актуальную версию и повторите шаг.',
  draftOnly: 'Удалить можно только черновик.',
  unpublishOnly: 'Снять с публикации можно только опубликованный тур.',
  forbidden: 'Нет доступа к этому туру. Чужой тур изменить нельзя.',
  mediaDown: 'Сервис фото недоступен. Тур не изменён, повторите позже.',
  notFound: 'Тур не найден.',
  rejected: 'Сервер не принял тур. Вот что нужно исправить:',
  network: 'Не удалось сохранить тур. Проверьте соединение и повторите.',
  invalid: 'Проверьте поля: сервер не принял данные.',
  fileType: 'Можно загрузить только JPEG, PNG или WebP.',
  tooManyPhotos: 'В туре не больше 20 фото.',
} as const;

export interface ManageFailure {
  status: number;
  missing: string[];
  message: string;
}

export function tourManageErrorText(code: string): string {
  if (isTourManageApiCode(code)) return TOUR_MANAGE_ERROR_TEXT[code];
  return `Не выполнено условие «${code}».`;
}

export function isTourManageApiCode(code: string): code is TourManageApiCode {
  return (TOUR_MANAGE_API_CODES as readonly string[]).includes(code);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function readMissing(data: Record<string, unknown> | null): string[] {
  if (!data) return [];
  if (Array.isArray(data.missing)) {
    return data.missing.filter((item): item is string => typeof item === 'string' && item.length > 0);
  }
  if (typeof data.code === 'string' && data.code.length > 0) return [data.code];
  return [];
}

function validationText(data: Record<string, unknown> | null): string {
  if (!data || !isRecord(data.errors)) return '';
  const parts: string[] = [];
  for (const value of Object.values(data.errors)) {
    if (Array.isArray(value)) {
      for (const item of value) {
        if (typeof item === 'string' && item.trim()) parts.push(item.trim());
      }
    }
  }
  return parts.join(' ');
}

export function readManageFailure(error: unknown): ManageFailure {
  const response = isRecord(error) && isRecord(error.response) ? error.response : null;
  const status = typeof response?.status === 'number' ? response.status : 0;
  const data = isRecord(response?.data) ? response.data : null;
  const missing = readMissing(data);
  const detail = typeof data?.detail === 'string' ? data.detail : '';
  const title = typeof data?.title === 'string' ? data.title : '';

  if (missing.length > 0) {
    return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.rejected };
  }

  if (status === 413 || (status === 400 && /exceed|10 mb|file size/i.test(`${detail} ${title}`))) {
    return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.fileTooLarge };
  }

  if (status === 412) {
    return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.preconditionFailed };
  }

  if (status === 428) {
    return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.preconditionRequired };
  }

  if (status === 409) {
    if (/draft/i.test(detail)) {
      return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.draftOnly };
    }
    if (/published/i.test(detail)) {
      return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.unpublishOnly };
    }
    return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.versionConflict };
  }

  if (status === 403) {
    return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.forbidden };
  }

  if (status === 503) {
    return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.mediaDown };
  }

  if (status === 404) {
    return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.notFound };
  }

  const fields = validationText(data);
  if (fields) return { status, missing, message: fields };
  if (detail) return { status, missing, message: detail };

  return { status, missing, message: TOUR_MANAGE_STATUS_TEXT.network };
}
