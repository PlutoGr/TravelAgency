import { useFavoritesStore } from '@/store/favoritesStore';
import { useUIStore } from '@/store/uiStore';

/**
 * После входа добавляет тур, если гость нажимал сердечко.
 * Вызывать только после loadFavorites.
 * Если список не загрузился, намерение стираем и не трогаем избранное:
 * иначе POST даст 409, а откат toggle уберёт тур из списка.
 * Тур, который уже в загруженном списке, тоже не переключаем.
 * Заявку здесь не открываем: это делает страница тура.
 */
export function resumeFavoriteIntent(): void {
  const intent = useUIStore.getState().authIntent;
  if (intent?.type !== 'favorite') return;
  const favorites = useFavoritesStore.getState();
  useUIStore.getState().clearAuthIntent();
  if (favorites.error || favorites.isFavorite(intent.tourId)) return;
  favorites.toggleFavorite(intent.tourId, intent.tour);
}
