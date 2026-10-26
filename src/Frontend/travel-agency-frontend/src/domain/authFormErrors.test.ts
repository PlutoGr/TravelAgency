import { describe, expect, it } from 'vitest';
import { AUTH_FORM_TEXT, authFormError } from './authFormErrors';

const englishDetail = 'A user with this email already exists.';

describe('authFormError', () => {
  it('maps register 409 to the taken-email text and drops the English detail', () => {
    expect(
      authFormError({ response: { status: 409, data: { detail: englishDetail } } }, 'register'),
    ).toEqual({ place: 'form', text: AUTH_FORM_TEXT.registerEmailTaken });
  });

  it('shows the blocked-account text with the support phone', () => {
    const text = 'Доступ к аккаунту ограничен. Свяжитесь с поддержкой: +79991234567';
    expect(
      authFormError(
        { response: { status: 401, data: { code: 'AccountBlocked', detail: text } } },
        'login',
      ),
    ).toEqual({ place: 'form', text });
  });

  it('maps login 401 to the credentials text and drops the English detail', () => {
    expect(
      authFormError(
        { response: { status: 401, data: { detail: 'Invalid credentials.' } } },
        'login',
      ),
    ).toEqual({ place: 'form', text: AUTH_FORM_TEXT.loginInvalid });
  });

  it('falls back when the 400 code is unknown and does not return the English detail', () => {
    const failure = authFormError(
      {
        response: {
          status: 400,
          data: {
            code: 'NotAKnownCode',
            detail: 'Password must contain at least one uppercase letter.',
            errors: { Password: ['Minimum 8 characters'] },
          },
        },
      },
      'register',
    );

    expect(failure).toEqual({ place: 'form', text: AUTH_FORM_TEXT.registerFallback });
    expect(JSON.stringify(failure)).not.toContain('Password');
    expect(JSON.stringify(failure)).not.toContain('Minimum');
  });

  it('maps register 400 DuplicateEmail the same way as 409', () => {
    expect(
      authFormError(
        { response: { status: 400, data: { code: 'DuplicateEmail', detail: englishDetail } } },
        'register',
      ),
    ).toEqual({ place: 'form', text: AUTH_FORM_TEXT.registerEmailTaken });
  });

  it('maps a 400 password code to the single password sentence', () => {
    expect(
      authFormError(
        {
          response: {
            status: 400,
            data: {
              code: 'PasswordTooShort',
              detail: 'Password must be at least 8 characters.',
            },
          },
        },
        'register',
      ),
    ).toEqual({ place: 'password', text: AUTH_FORM_TEXT.passwordRule });
  });

  it('keeps the login 429 text', () => {
    expect(
      authFormError(
        { response: { status: 429, data: { detail: 'Too many requests.' } } },
        'login',
      ),
    ).toEqual({ place: 'form', text: AUTH_FORM_TEXT.loginRateLimited });
  });

  it('uses the shared fallback on the profile form', () => {
    expect(
      authFormError(
        { response: { status: 404, data: { detail: 'User not found.' } } },
        'profile',
      ),
    ).toEqual({ place: 'form', text: AUTH_FORM_TEXT.requestFallback });
  });
});
