import type { AuthTokens, LoginRequest, RegisterRequest, User } from '@/types';
import { mockUser } from '@/mocks/users';

const delay = () => new Promise((r) => setTimeout(r, 300 + Math.random() * 200));

export async function login(
  _data: LoginRequest,
): Promise<{ user: User; tokens: AuthTokens }> {
  await delay();
  return {
    user: mockUser,
    tokens: {
      accessToken: 'mock-access-token-' + Date.now(),
      refreshToken: 'mock-refresh-token-' + Date.now(),
    },
  };
}

export async function register(
  data: RegisterRequest,
): Promise<{ user: User; tokens: AuthTokens }> {
  await delay();
  return {
    user: {
      ...mockUser,
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      phone: data.phone,
      createdAt: new Date().toISOString(),
    },
    tokens: {
      accessToken: 'mock-access-token-' + Date.now(),
      refreshToken: 'mock-refresh-token-' + Date.now(),
    },
  };
}

export async function logout(): Promise<void> {
  await delay();
}

export async function getMe(): Promise<User> {
  await delay();
  return mockUser;
}

export async function updateProfile(data: Partial<User>): Promise<User> {
  await delay();
  return { ...mockUser, ...data };
}
