import { apiClient } from './client';

export type AccountOutcome = {
  outcome: string;
  session?: string;
  message?: string;
};

export async function requestPasswordReset(email: string): Promise<AccountOutcome> {
  const { data } = await apiClient.post<AccountOutcome>('/auth/password-resets/request', {
    email: email.trim(),
  });
  return data;
}

export async function inspectPasswordReset(token: string): Promise<AccountOutcome> {
  const { data } = await apiClient.post<AccountOutcome>('/auth/password-resets/inspect', { token });
  return data;
}

export async function completePasswordReset(
  token: string,
  password: string,
  passwordConfirm: string,
): Promise<AccountOutcome> {
  const { data } = await apiClient.post<AccountOutcome>('/auth/password-resets', {
    token,
    password,
    passwordConfirm,
  });
  return data;
}

export async function changePassword(
  currentPassword: string,
  newPassword: string,
  passwordConfirm: string,
): Promise<void> {
  await apiClient.post('/auth/me/password', { currentPassword, newPassword, passwordConfirm });
}

export async function requestEmailChange(email: string, currentPassword: string): Promise<AccountOutcome> {
  const { data } = await apiClient.post<AccountOutcome>('/auth/me/email', {
    email: email.trim(),
    currentPassword,
  });
  return data;
}

export async function confirmEmailChange(token: string): Promise<AccountOutcome> {
  const { data } = await apiClient.post<AccountOutcome>('/auth/email-confirmations', { token });
  return data;
}

export async function cancelEmailChange(token: string): Promise<AccountOutcome> {
  const { data } = await apiClient.post<AccountOutcome>('/auth/email-cancellations', { token });
  return data;
}
