import { apiClient } from './client';
import type {
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
    role: dto.role.toLowerCase() as UserRole,
    createdAt: dto.createdAt,
  };
}

export async function login(
  data: LoginRequest,
): Promise<{ user: User }> {
  await apiClient.post('/auth/login', data);
  const user = await getMe();
  return { user };
}

export async function register(
  data: RegisterRequest,
): Promise<{ user: User }> {
  await apiClient.post('/auth/register', data);
  const user = await getMe();
  return { user };
}

export async function logout(): Promise<void> {
  try {
    await apiClient.post('/auth/logout', {});
  } finally {
    // Always clear local state even if API call fails
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
