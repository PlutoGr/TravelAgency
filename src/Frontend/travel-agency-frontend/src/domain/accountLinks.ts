import { AUTH_FORM_TEXT } from './authFormErrors';

export const ACCOUNT_TEXT = {
  resetSent:
    'Если такой адрес зарегистрирован, мы отправили на него письмо со ссылкой. Проверьте почту и папку «Спам»',
  emailSent:
    'Мы отправили письмо на новый адрес. Пока вы не подтвердите его, вход остаётся по прежнему адресу.',
  requestFailed: 'Не удалось выполнить запрос. Попробуйте ещё раз.',
  emailInvalid: 'Укажите адрес почты',
  emailUnchanged: 'Это уже ваш адрес',
  currentInvalid: 'Текущий пароль указан неверно',
  passwordEmpty: 'Укажите пароль',
  passwordMismatch: 'Пароли не совпадают',
  fillAll: 'Заполните все поля',
  passwordRule: AUTH_FORM_TEXT.passwordRule,
  passwordHint: 'Минимум 8 символов, заглавная, строчная буква и цифра',
  resetChanged: 'Пароль изменён, войдите с новым паролем',
  resetChangedOther:
    'Пароль изменён для учётной записи, которая запросила ссылку. Этот вход не затронут.',
  resetInvalid: 'Ссылка устарела или уже использована. Запросите новую',
  emailChanged: 'Адрес изменён. Вход - по новому адресу.',
  emailChangedOther:
    'Адрес подтверждён для учётной записи, которая запросила смену. Этот вход не затронут.',
  emailTaken: 'Этот адрес уже занят. Вход остаётся прежним.',
  linkInvalid: 'Ссылка устарела или уже использована.',
  emailCancelled: 'Смена почты отменена. Адрес входа остался прежним.',
  passwordChanged: 'Пароль изменён',
  emailHint: 'Смена адреса - на вкладке «Настройки».',
} as const;

const CODE_TEXT: Record<string, string> = {
  PasswordRule: ACCOUNT_TEXT.passwordRule,
  PasswordMismatch: ACCOUNT_TEXT.passwordMismatch,
  CurrentPasswordInvalid: ACCOUNT_TEXT.currentInvalid,
  InvalidEmail: ACCOUNT_TEXT.emailInvalid,
  EmailUnchanged: ACCOUNT_TEXT.emailUnchanged,
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function responseOf(error: unknown): { status?: number; data?: Record<string, unknown> } {
  if (!isRecord(error) || !isRecord(error.response)) return {};
  const status = typeof error.response.status === 'number' ? error.response.status : undefined;
  const data = isRecord(error.response.data) ? error.response.data : undefined;
  return { status, data };
}

/** Фразу выбирает code. message, если он пришёл, важнее. detail и errors на экран не попадают. */
export function accountFailureText(error: unknown): string {
  const { status, data } = responseOf(error);
  if (status === 429 || status === undefined) return ACCOUNT_TEXT.requestFailed;

  const message = data && typeof data.message === 'string' ? data.message : '';
  if (message) return message;

  const code = data && typeof data.code === 'string' ? data.code : '';
  return CODE_TEXT[code] ?? ACCOUNT_TEXT.requestFailed;
}

export function readOutcome(data: unknown): { outcome?: string; session?: string; message?: string } {
  if (!isRecord(data)) return {};
  return {
    outcome: typeof data.outcome === 'string' ? data.outcome : undefined,
    session: typeof data.session === 'string' ? data.session : undefined,
    message: typeof data.message === 'string' ? data.message : undefined,
  };
}

export function isEmailShape(value: string): boolean {
  const email = value.trim();
  if (!email || email.length > 256) return false;
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}
