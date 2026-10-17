import { useFavoritesStore } from '@/store/favoritesStore';
import { useUIStore } from '@/store/uiStore';

/**
 * После входа добавляет тур, если гость нажимал сердечко.
 * Заявку здесь не открываем: это делает страница тура, когда окно входа уже закрыто.
 * Вызывать после загрузки избранного, иначе ответ сервера затрёт только что добавленный id.
 */
export function resumeFavoriteIntent(): void {
  const intent = useUIStore.getState().authIntent;
  if (intent?.type !== 'favorite') return;
  useUIStore.getState().clearAuthIntent();
  if (useFavoritesStore.getState().isFavorite(intent.tourId)) return;
  useFavoritesStore.getState().toggleFavorite(intent.tourId, intent.tour);
}
