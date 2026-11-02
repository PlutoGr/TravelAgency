import { apiClient } from './client';

/** Ответ `GET /api/v1/users/{id}/manager-card`. Лишние поля контракта сюда не входят. */
export type ManagerPublicCard = {
  id: string;
  displayName: string;
  avatarUrl?: string;
};

type ManagerPublicCardDto = {
  id?: unknown;
  displayName?: unknown;
  avatarUrl?: unknown;
};

export async function getManagerCard(userId: string): Promise<ManagerPublicCard> {
  const { data } = await apiClient.get<ManagerPublicCardDto>(`/users/${userId}/manager-card`);
  const avatarUrl = typeof data.avatarUrl === 'string' ? data.avatarUrl : '';

  return {
    id: String(data.id ?? ''),
    displayName: typeof data.displayName === 'string' ? data.displayName : '',
    ...(avatarUrl !== '' ? { avatarUrl } : {}),
  };
}
