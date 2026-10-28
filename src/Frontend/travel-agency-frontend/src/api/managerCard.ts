import { apiClient } from './client';

/** Ответ `GET /api/v1/users/{id}/manager-card`. Лишние поля контракта сюда не входят. */
export type ManagerPublicCard = {
  id: string;
  displayName: string;
  avatarUrl?: string;
  about?: string;
  tourismSince?: number;
  languages?: string[];
};

type ManagerPublicCardDto = ManagerPublicCard & Record<string, unknown>;

export async function getManagerCard(userId: string): Promise<ManagerPublicCard> {
  const { data } = await apiClient.get<ManagerPublicCardDto>(`/users/${userId}/manager-card`);
  const languages = Array.isArray(data.languages)
    ? data.languages.filter((code) => typeof code === 'string' && code !== '')
    : [];
  const about = typeof data.about === 'string' ? data.about.trim() : '';

  return {
    id: String(data.id ?? ''),
    displayName: typeof data.displayName === 'string' ? data.displayName : '',
    ...(typeof data.avatarUrl === 'string' && data.avatarUrl !== ''
      ? { avatarUrl: data.avatarUrl }
      : {}),
    ...(about !== '' ? { about } : {}),
    ...(typeof data.tourismSince === 'number' && Number.isInteger(data.tourismSince)
      ? { tourismSince: data.tourismSince }
      : {}),
    ...(languages.length > 0 ? { languages } : {}),
  };
}
