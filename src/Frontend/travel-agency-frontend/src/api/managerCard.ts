import { apiClient } from './client';

/** Ответ карточки менеджера. Почта, телефон и роль сюда не входят. */
export type ManagerPublicCard = {
  id: string;
  displayName: string;
  avatarUrl?: string;
  position?: string | null;
  tourismSince?: number | null;
  about?: string | null;
  tags?: string[];
};

export type ManagerCardWrite = {
  position: string | null;
  tourismSince: number | null;
  about: string | null;
  tags: string[];
};

type ManagerPublicCardDto = {
  id?: unknown;
  displayName?: unknown;
  avatarUrl?: unknown;
  position?: unknown;
  tourismSince?: unknown;
  about?: unknown;
  tags?: unknown;
};

function readText(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value : null;
}

export function mapManagerCard(data: ManagerPublicCardDto): ManagerPublicCard {
  const avatarUrl = typeof data.avatarUrl === 'string' ? data.avatarUrl : '';
  const tourismSince =
    typeof data.tourismSince === 'number' && Number.isInteger(data.tourismSince) ? data.tourismSince : null;
  const tags = Array.isArray(data.tags)
    ? data.tags.filter((item): item is string => typeof item === 'string')
    : [];

  return {
    id: String(data.id ?? ''),
    displayName: typeof data.displayName === 'string' ? data.displayName : '',
    ...(avatarUrl !== '' ? { avatarUrl } : {}),
    position: readText(data.position),
    tourismSince,
    about: typeof data.about === 'string' && data.about.trim() ? data.about : null,
    tags,
  };
}

export async function getManagerCard(userId: string): Promise<ManagerPublicCard> {
  const { data } = await apiClient.get<ManagerPublicCardDto>(`/users/${userId}/manager-card`);
  return mapManagerCard(data);
}

export async function updateManagerCard(body: ManagerCardWrite): Promise<ManagerPublicCard> {
  const { data } = await apiClient.patch<ManagerPublicCardDto>('/auth/me/manager-card', {
    position: body.position,
    tourismSince: body.tourismSince,
    about: body.about,
    tags: body.tags,
  });
  return mapManagerCard(data);
}
