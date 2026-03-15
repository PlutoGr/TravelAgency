import { create } from 'zustand';
import type { RegisterRequest, User } from '@/types';
import * as authApi from '@/api/auth';
import { AUTH_TOKEN_KEY } from '@/api/client';

type AuthState = {
  user: User | null;
  token: string | null;
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
  token: null,
  isAuthenticated: false,
  isLoading: false,

  login: async (email, password) => {
    set({ isLoading: true });
    try {
      const { user, tokens } = await authApi.login({ email, password });
      set({ user, token: tokens.accessToken, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  register: async (data) => {
    set({ isLoading: true });
    try {
      const { user, tokens } = await authApi.register(data);
      set({ user, token: tokens.accessToken, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  logout: async () => {
    try {
      await authApi.logout();
    } finally {
      set({ user: null, token: null, isAuthenticated: false });
    }
  },

  setUser: (user) => set({ user }),

  checkAuth: () => {
    // SECURITY: Token read from localStorage (XSS-vulnerable). Migration to httpOnly cookies planned.
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    if (token) {
      set({ token, isAuthenticated: true, isLoading: true });
      authApi
        .getMe()
        .then((user) => set({ user, isLoading: false }))
        .catch((error) => {
          const isAuthFailure = error?.response?.status === 401;
          if (isAuthFailure) {
            void authApi.logout();
            set({ user: null, token: null, isAuthenticated: false, isLoading: false });
          } else {
            set({ isLoading: false });
          }
        });
    }
  },
}));
