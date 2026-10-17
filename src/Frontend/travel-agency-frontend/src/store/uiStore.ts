import type { Tour } from '@/types';
import { create } from 'zustand';

type AuthModalTab = 'login' | 'register';

/** Что гость хотел сделать, когда его отправили во вход. */
export type AuthIntent =
  | { type: 'request'; tourId: string; offerId: string | null }
  | { type: 'favorite'; tourId: string; tour?: Tour };

type UIState = {
  isAuthModalOpen: boolean;
  authModalTab: AuthModalTab;
  authIntent: AuthIntent | null;
  isMobileMenuOpen: boolean;
  openAuthModal: (tab?: AuthModalTab, intent?: AuthIntent | null) => void;
  /** Закрыть окно после успешного входа, намерение остаётся для страницы. */
  closeAuthModal: () => void;
  /** Гость закрыл окно сам: намерение больше не выполняем. */
  dismissAuthModal: () => void;
  clearAuthIntent: () => void;
  toggleMobileMenu: () => void;
  closeMobileMenu: () => void;
};

export const useUIStore = create<UIState>((set) => ({
  isAuthModalOpen: false,
  authModalTab: 'login',
  authIntent: null,
  isMobileMenuOpen: false,

  openAuthModal: (tab = 'login', intent) =>
    set((state) => ({
      isAuthModalOpen: true,
      authModalTab: tab,
      // Смена вкладки внутри уже открытого окна не сбрасывает намерение.
      // Новый вызов с закрытого окна без намерения начинает чистый вход.
      authIntent:
        intent !== undefined ? intent : state.isAuthModalOpen ? state.authIntent : null,
    })),

  closeAuthModal: () => set({ isAuthModalOpen: false }),

  dismissAuthModal: () => set({ isAuthModalOpen: false, authIntent: null }),

  clearAuthIntent: () => set({ authIntent: null }),

  toggleMobileMenu: () =>
    set((state) => ({ isMobileMenuOpen: !state.isMobileMenuOpen })),

  closeMobileMenu: () => set({ isMobileMenuOpen: false }),
}));
