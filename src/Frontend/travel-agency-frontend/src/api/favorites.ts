import type { Tour } from '@/types';
import { mockTours } from '@/mocks/tours';

const delay = () => new Promise((r) => setTimeout(r, 300 + Math.random() * 200));

const STORAGE_KEY = 'favorite_tours';

function getStoredIds(): string[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

function storeIds(ids: string[]) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(ids));
}

export async function getFavorites(): Promise<Tour[]> {
  await delay();

  const ids = getStoredIds();
  return mockTours.filter((t) => ids.includes(t.id));
}

export async function addFavorite(tourId: string): Promise<void> {
  await delay();

  const ids = getStoredIds();
  if (!ids.includes(tourId)) {
    storeIds([...ids, tourId]);
  }
}

export async function removeFavorite(tourId: string): Promise<void> {
  await delay();

  const ids = getStoredIds();
  storeIds(ids.filter((id) => id !== tourId));
}
