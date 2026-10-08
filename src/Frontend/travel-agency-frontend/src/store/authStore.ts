import { create } from 'zustand';
import type { RegisterRequest, User } from '@/types';
import * as authApi from '@/api/auth';

type AuthState = {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  setUser: (user: User) => void;
  checkAuth: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  // True until the first auth/me check finishes. A direct load or F5 of
  // /dashboard/* must not treat "user still null" as a guest: checkAuth()
  // runs in an effect, after the first render of ProtectedRoute.
  isLoading: true,

  login: async (email, password) => {
    set({ isLoading: true });
    try {
      const { user } = await authApi.login({ email, password });
      set({ user, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  register: async (data) => {
    set({ isLoading: true });
    try {
      const { user } = await authApi.register(data);
      set({ user, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  logout: async () => {
    try {
      await authApi.logout();
    } finally {
      set({ user: null, isAuthenticated: false });
    }
  },

  setUser: (user) => set({ user }),

  checkAuth: () => {
    set({ isLoading: true });
    authApi
      .getMe()
      .then((user) => set({ user, isAuthenticated: true, isLoading: false }))
      .catch((error) => {
        const isAuthFailure = error?.response?.status === 401;
        if (isAuthFailure) {
          set({ user: null, isAuthenticated: false, isLoading: false });
        } else {
          set({ isLoading: false });
        }
      });
  },
}));
