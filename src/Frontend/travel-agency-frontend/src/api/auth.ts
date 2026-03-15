import { apiClient, applyTokens, AUTH_TOKEN_KEY, REFRESH_TOKEN_KEY } from './client';
import { mapAuthTokensDtoToAuthTokens, type AuthTokensDto } from './dto';
import type {
  AuthTokens,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  User,
} from '@/types';

/** Backend DTO: user profile */
type UserProfileDto = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string | null;
  role: string;
  createdAt: string;
};

export type UserRole = 'client' | 'manager' | 'admin';

/** Maps backend UserProfileDto to User. Note: avatar is not in backend DTO; reserved for future use. */
function mapUserProfileDtoToUser(dto: UserProfileDto): User {
  return {
    id: dto.id,
    email: dto.email,
    firstName: dto.firstName,
    lastName: dto.lastName,
    phone: dto.phone ?? '',
    role: dto.role as UserRole,
    createdAt: dto.createdAt,
  };
}

export async function login(
  data: LoginRequest,
): Promise<{ user: User; tokens: AuthTokens }> {
  const { data: tokensDto } = await apiClient.post<AuthTokensDto>(
    '/auth/login',
    data,
  );
  const tokens = mapAuthTokensDtoToAuthTokens(tokensDto);
  applyTokens(tokens);

  const user = await getMe();
  return { user, tokens };
}

export async function register(
  data: RegisterRequest,
): Promise<{ user: User; tokens: AuthTokens }> {
  const { data: tokensDto } = await apiClient.post<AuthTokensDto>(
    '/auth/register',
    data,
  );
  const tokens = mapAuthTokensDtoToAuthTokens(tokensDto);
  applyTokens(tokens);

  const user = await getMe();
  return { user, tokens };
}

export async function logout(): Promise<void> {
  // SECURITY: Tokens read from localStorage (XSS-vulnerable). Migration to httpOnly cookies planned.
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
  try {
    if (refreshToken) {
      await apiClient.post('/auth/logout', { refreshToken });
    }
  } finally {
    // Always clear local state even if API call fails
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    delete apiClient.defaults.headers.common.Authorization;
  }
}

export async function getMe(): Promise<User> {
  const { data } = await apiClient.get<UserProfileDto>('/auth/me');
  return mapUserProfileDtoToUser(data);
}

export async function updateProfile(data: UpdateProfileRequest): Promise<User> {
  const payload: UpdateProfileRequest = {};
  if (data.firstName !== undefined) payload.firstName = data.firstName;
  if (data.lastName !== undefined) payload.lastName = data.lastName;
  if (data.phone !== undefined) payload.phone = data.phone;

  const { data: dto } = await apiClient.patch<UserProfileDto>(
    '/auth/me',
    payload,
  );
  return mapUserProfileDtoToUser(dto);
}

export async function refresh(): Promise<AuthTokens> {
  // SECURITY: Tokens read from localStorage (XSS-vulnerable). Migration to httpOnly cookies planned.
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
  if (!refreshToken) {
    throw new Error('No refresh token available');
  }

  const { data: tokensDto } = await apiClient.post<AuthTokensDto>(
    '/auth/refresh',
    { refreshToken },
  );
  const tokens = mapAuthTokensDtoToAuthTokens(tokensDto);
  applyTokens(tokens);

  return tokens;
}
