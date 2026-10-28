import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient } from './client';
import { getManagerCard } from './managerCard';

describe('getManagerCard', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('читает краткую карточку через общий клиент', async () => {
    const get = vi.spyOn(apiClient, 'get').mockResolvedValue({
      data: {
        id: 'user-1',
        displayName: 'Марина Иванова',
        avatarUrl: '/api/v1/media/files/file-1/a256',
        about: '  Первая строка  ',
        tourismSince: 2018,
        languages: ['ru', 'en'],
        email: 'secret@example.com',
        phone: '+79990000000',
        city: 'Сочи',
        specialization: 'Море',
      },
    });

    const card = await getManagerCard('user-1');

    expect(apiClient.defaults.baseURL).toBe('/api/v1');
    expect(get).toHaveBeenCalledWith('/users/user-1/manager-card');
    expect(card).toEqual({
      id: 'user-1',
      displayName: 'Марина Иванова',
      avatarUrl: '/api/v1/media/files/file-1/a256',
      about: 'Первая строка',
      tourismSince: 2018,
      languages: ['ru', 'en'],
    });
    expect(card).not.toHaveProperty('email');
    expect(card).not.toHaveProperty('phone');
    expect(card).not.toHaveProperty('specialization');
  });
});
