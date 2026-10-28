import { afterEach, describe, expect, it } from 'vitest';
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { apiClient } from './client';
import { deleteAvatar, uploadAvatar } from './auth';

const originalAdapter = apiClient.defaults.adapter;

function httpResponse(
  config: InternalAxiosRequestConfig,
  data: unknown,
): AxiosResponse {
  return { data, status: 200, statusText: 'OK', headers: {}, config };
}

function contentType(config: InternalAxiosRequestConfig): string {
  const headers = config.headers;
  if (headers && typeof headers.get === 'function') {
    const value = headers.get('Content-Type');
    return value == null ? '' : String(value);
  }
  return '';
}

const dto = {
  id: 'u1',
  email: 'anna@example.com',
  firstName: 'Анна',
  lastName: 'Смирнова',
  phone: '+79991234567',
  city: 'Казань',
  role: 'Client',
  createdAt: '2026-01-01T00:00:00Z',
};

describe('avatar transport', () => {
  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
  });

  it('posts multipart without a json content type and deletes without a body', async () => {
    const file = new File([Uint8Array.from([1, 2, 3])], 'face.jpg', { type: 'image/jpeg' });
    const calls: { url: string; method: string; data: unknown; contentType: string }[] = [];

    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      const method = (config.method ?? '').toLowerCase();
      calls.push({ url, method, data: config.data, contentType: contentType(config) });
      if (url === '/auth/me/avatar' && method === 'post') {
        return httpResponse(config, {
          ...dto,
          avatarUrl: '/api/v1/media/files/abc/a256',
        });
      }
      if (url === '/auth/me/avatar' && method === 'delete') {
        return httpResponse(config, { ...dto, avatarUrl: null });
      }
      throw new Error(`unexpected ${method} ${url}`);
    };

    const uploaded = await uploadAvatar(file);
    const deleted = await deleteAvatar();

    expect(calls.map((call) => `${call.method} ${call.url}`)).toEqual([
      'post /auth/me/avatar',
      'delete /auth/me/avatar',
    ]);
    expect(calls[0].contentType.toLowerCase()).not.toContain('application/json');
    expect(calls[0].data).toBeInstanceOf(FormData);
    expect((calls[0].data as FormData).get('file')).toBe(file);
    expect(calls[1].data ?? null).toBeNull();
    expect(uploaded.avatar).toBe('/api/v1/media/files/abc/a256');
    expect(uploaded.city).toBe('Казань');
    expect(deleted.avatar).toBeUndefined();
  });
});
