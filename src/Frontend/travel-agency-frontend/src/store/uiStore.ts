import { create } from 'zustand';

type AuthModalTab = 'login' | 'register';

type UIState = {
  isAuthModalOpen: boolean;
  authModalTab: AuthModalTab;
  isMobileMenuOpen: boolean;
  openAuthModal: (tab?: AuthModalTab) => void;
  closeAuthModal: () => void;
  toggleMobileMenu: () => void;
  closeMobileMenu: () => void;
};

export const useUIStore = create<UIState>((set) => ({
  isAuthModalOpen: false,
  authModalTab: 'login',
  isMobileMenuOpen: false,

  openAuthModal: (tab = 'login') =>
    set({ isAuthModalOpen: true, authModalTab: tab }),

  closeAuthModal: () => set({ isAuthModalOpen: false }),

  toggleMobileMenu: () =>
    set((state) => ({ isMobileMenuOpen: !state.isMobileMenuOpen })),

  closeMobileMenu: () => set({ isMobileMenuOpen: false }),
}));
