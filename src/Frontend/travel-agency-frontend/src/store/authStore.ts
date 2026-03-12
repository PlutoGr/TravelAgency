import { create } from 'zustand';
import type { RegisterRequest, User } from '@/types';
import * as authApi from '@/api/auth';

const TOKEN_KEY = 'auth_token';

type AuthState = {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
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
      localStorage.setItem(TOKEN_KEY, tokens.accessToken);
      set({ user, token: tokens.accessToken, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  register: async (data) => {
    set({ isLoading: true });
    try {
      const { user, tokens } = await authApi.register(data);
      localStorage.setItem(TOKEN_KEY, tokens.accessToken);
      set({ user, token: tokens.accessToken, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  logout: () => {
    localStorage.removeItem(TOKEN_KEY);
    set({ user: null, token: null, isAuthenticated: false });
  },

  setUser: (user) => set({ user }),

  checkAuth: () => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token) {
      set({ token, isAuthenticated: true, isLoading: true });
      authApi
        .getMe()
        .then((user) => set({ user, isLoading: false }))
        .catch(() => {
          localStorage.removeItem(TOKEN_KEY);
          set({ token: null, isAuthenticated: false, isLoading: false });
        });
    }
  },
}));
