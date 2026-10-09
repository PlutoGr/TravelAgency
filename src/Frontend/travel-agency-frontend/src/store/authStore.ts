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

// Latest login, register, or logout. checkAuth remembers the value at
// start and writes its result only while the value is unchanged, so a
// page-load auth/me that answers after login cannot clear the session.
let authGeneration = 0;

function bumpAuthGeneration(): void {
  authGeneration += 1;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  // True until the first auth/me check finishes. A direct load or F5 of
  // /dashboard/* must not treat "user still null" as a guest: checkAuth()
  // runs in an effect, after the first render of ProtectedRoute.
  isLoading: true,

  login: async (email, password) => {
    bumpAuthGeneration();
    set({ isLoading: true });
    try {
      const { user } = await authApi.login({ email, password });
      set({ user, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  register: async (data) => {
    bumpAuthGeneration();
    set({ isLoading: true });
    try {
      const { user } = await authApi.register(data);
      set({ user, isAuthenticated: true });
    } finally {
      set({ isLoading: false });
    }
  },

  logout: async () => {
    bumpAuthGeneration();
    try {
      await authApi.logout();
    } finally {
      // The in-flight checkAuth will not clear isLoading: its generation
      // is already stale. Logout leaves the store as a settled guest.
      set({ user: null, isAuthenticated: false, isLoading: false });
    }
  },

  setUser: (user) => set({ user }),

  checkAuth: () => {
    const generation = authGeneration;
    set({ isLoading: true });
    authApi
      .getMe()
      .then((user) => {
        if (generation !== authGeneration) return;
        set({ user, isAuthenticated: true, isLoading: false });
      })
      .catch((error) => {
        if (generation !== authGeneration) return;
        const isAuthFailure = error?.response?.status === 401;
        if (isAuthFailure) {
          set({ user: null, isAuthenticated: false, isLoading: false });
        } else {
          set({ isLoading: false });
        }
      });
  },
}));
