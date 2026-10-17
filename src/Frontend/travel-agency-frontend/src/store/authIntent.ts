import toast from 'react-hot-toast';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useUIStore } from '@/store/uiStore';

export const favoriteIntentErrorToast =
  'Не удалось добавить тур в избранное. Попробуйте ещё раз';

/**
 * После входа добавляет тур, если гость нажимал сердечко.
 * Вызывать только после loadFavorites.
 * Если список не загрузился, намерение стираем, показываем тост и не трогаем избранное:
 * иначе POST даст 409, а откат toggle уберёт тур из списка. Повтора в фоне нет.
 * Тур, который уже в загруженном списке, тоже не переключаем.
 * Заявку здесь не открываем: это делает страница тура.
 */
export function resumeFavoriteIntent(): void {
  const intent = useUIStore.getState().authIntent;
  if (intent?.type !== 'favorite') return;
  const favorites = useFavoritesStore.getState();
  useUIStore.getState().clearAuthIntent();
  if (favorites.error) {
    toast.error(favoriteIntentErrorToast);
    return;
  }
  if (favorites.isFavorite(intent.tourId)) return;
  favorites.toggleFavorite(intent.tourId, intent.tour);
}
