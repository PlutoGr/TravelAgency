import { create } from 'zustand';

type AuthModalTab = 'login' | 'register';

type UIState = {
  isAuthModalOpen: boolean;
  authModalTab: AuthModalTab;
  authReturnTo: string | null;
  isMobileMenuOpen: boolean;
  openAuthModal: (tab?: AuthModalTab, returnTo?: string) => void;
  closeAuthModal: () => void;
  toggleMobileMenu: () => void;
  closeMobileMenu: () => void;
};

export const useUIStore = create<UIState>((set) => ({
  isAuthModalOpen: false,
  authModalTab: 'login',
  authReturnTo: null,
  isMobileMenuOpen: false,

  openAuthModal: (tab = 'login', returnTo) =>
    set({ isAuthModalOpen: true, authModalTab: tab, authReturnTo: returnTo ?? null }),

  closeAuthModal: () => set({ isAuthModalOpen: false, authReturnTo: null }),

  toggleMobileMenu: () =>
    set((state) => ({ isMobileMenuOpen: !state.isMobileMenuOpen })),

  closeMobileMenu: () => set({ isMobileMenuOpen: false }),
}));
