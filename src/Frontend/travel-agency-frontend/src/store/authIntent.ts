import toast from 'react-hot-toast';
import { useFavoritesStore } from '@/store/favoritesStore';
import { useUIStore } from '@/store/uiStore';

export const favoriteIntentErrorToast =
  'Не удалось добавить тур в избранное. Попробуйте ещё раз';

/**
 * Сессия уже есть: вход прошёл или стартовый refresh вернул пользователя.
 * Окно входа, открытое пока проверка ещё считала гостя, закрываем.
 * Намерение не стираем: его заберёт страница тура или resumeFavoriteIntent.
 */
export function closeLoginKeepingIntent(): void {
  if (useUIStore.getState().isAuthModalOpen) {
    useUIStore.getState().closeAuthModal();
  }
}

/**
 * После появления сессии. loadFavorites обязателен до избранного.
 * isCancelled останавливает продолжение, если сессия успела смениться.
 */
export async function afterSessionEstablished(isCancelled: () => boolean): Promise<void> {
  closeLoginKeepingIntent();
  await useFavoritesStore.getState().loadFavorites();
  if (isCancelled()) return;

  const favorites = useFavoritesStore.getState();
  const intent = useUIStore.getState().authIntent;
  if (intent?.type === 'favorite' && favorites.error) {
    useUIStore.getState().clearAuthIntent();
    toast.error(favoriteIntentErrorToast);
    return;
  }
  if (intent?.type === 'favorite' && favorites.isFavorite(intent.tourId)) {
    useUIStore.getState().clearAuthIntent();
    return;
  }
  resumeFavoriteIntent();
}

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
