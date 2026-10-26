export const AUTH_FORM_TEXT = {
  loginInvalid: 'Неверный email или пароль.',
  loginRateLimited: 'Слишком много попыток входа. Подождите немного и попробуйте снова.',
  loginUnavailable: 'Не удалось войти. Попробуйте ещё раз через минуту',
  registerEmailTaken: 'Пользователь с таким email уже зарегистрирован. Войдите в аккаунт.',
  registerFallback: 'Ошибка регистрации. Попробуйте ещё раз.',
  registerUnavailable: 'Не удалось зарегистрироваться. Попробуйте ещё раз через минуту',
  requestFallback: 'Не удалось выполнить запрос. Попробуйте ещё раз.',
  passwordRule:
    'Пароль должен быть не короче 8 символов и содержать хотя бы одну заглавную букву и одну цифру.',
} as const;

/** Коды правил пароля Identity. Любой из них даёт одну и ту же фразу под полем. */
const PASSWORD_RULE_CODES = new Set([
  'PasswordTooShort',
  'PasswordTooLong',
  'PasswordRequiresDigit',
  'PasswordRequiresLower',
  'PasswordRequiresUpper',
  'PasswordRequiresNonAlphanumeric',
  'PasswordRequiresUniqueChars',
]);

export type AuthFormKind = 'login' | 'register' | 'profile';

export type AuthFormError =
  | { place: 'toast'; text: string }
  | { place: 'form'; text: string }
  | { place: 'password'; text: string };

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function readStatus(error: unknown): number | undefined {
  if (!isRecord(error) || !isRecord(error.response)) return undefined;
  return typeof error.response.status === 'number' ? error.response.status : undefined;
}

function readCode(error: unknown): string | undefined {
  if (!isRecord(error) || !isRecord(error.response) || !isRecord(error.response.data)) return undefined;
  const code = error.response.data.code;
  return typeof code === 'string' && code.length > 0 ? code : undefined;
}

function readDetail(error: unknown): string | undefined {
  if (!isRecord(error) || !isRecord(error.response) || !isRecord(error.response.data)) return undefined;
  const detail = error.response.data.detail;
  return typeof detail === 'string' && detail.length > 0 ? detail : undefined;
}

/**
 * Текст формы по статусу HTTP и, для 400, по `code` из ProblemDetails.
 * `detail` и список `errors` сюда не попадают: неизвестный код даёт запасную фразу формы.
 */
export function authFormError(error: unknown, form: AuthFormKind): AuthFormError {
  const status = readStatus(error);
  const code = readCode(error);

  if (status === 502 && form === 'login') {
    return { place: 'toast', text: AUTH_FORM_TEXT.loginUnavailable };
  }
  if (status === 502 && form === 'register') {
    return { place: 'form', text: AUTH_FORM_TEXT.registerUnavailable };
  }
  if (status === 429 && form === 'login') {
    return { place: 'form', text: AUTH_FORM_TEXT.loginRateLimited };
  }
  if (status === 401 && code === 'AccountBlocked' && form === 'login') {
    const detail = readDetail(error);
    if (detail?.startsWith('Доступ к аккаунту ограничен. Свяжитесь с поддержкой')) {
      return { place: 'form', text: detail };
    }
    return { place: 'form', text: 'Доступ к аккаунту ограничен. Свяжитесь с поддержкой.' };
  }
  if (status === 401 && form === 'login') {
    return { place: 'form', text: AUTH_FORM_TEXT.loginInvalid };
  }
  if (form === 'register' && (status === 409 || (status === 400 && code === 'DuplicateEmail'))) {
    return { place: 'form', text: AUTH_FORM_TEXT.registerEmailTaken };
  }
  if (status === 400 && code !== undefined && PASSWORD_RULE_CODES.has(code) && form !== 'profile') {
    return { place: 'password', text: AUTH_FORM_TEXT.passwordRule };
  }

  return {
    place: 'form',
    text: form === 'register' ? AUTH_FORM_TEXT.registerFallback : AUTH_FORM_TEXT.requestFallback,
  };
}
