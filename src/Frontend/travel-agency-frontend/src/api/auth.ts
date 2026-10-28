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
  city?: string | null;
  avatarUrl?: string | null;
  about?: string | null;
  tourismSince?: number | null;
  languages?: string[] | null;
  specialization?: string | null;
};

export type UserRole = 'client' | 'manager' | 'admin';

/** Maps backend UserProfileDto to User. */
export function mapUserProfileDtoToUser(dto: UserProfileDto): User {
  const user: User = {
    id: dto.id,
    email: dto.email,
    firstName: dto.firstName,
    lastName: dto.lastName,
    phone: dto.phone ?? '',
    city: dto.city ?? '',
    avatar: dto.avatarUrl ?? undefined,
    role: dto.role.toLowerCase() as UserRole,
    createdAt: dto.createdAt,
    about: dto.about ?? '',
    languages: dto.languages ?? [],
    specialization: dto.specialization ?? '',
  };
  if (dto.tourismSince != null) user.tourismSince = dto.tourismSince;
  return user;
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
  if (data.city !== undefined) payload.city = data.city;
  if (data.about !== undefined) payload.about = data.about;
  if (data.specialization !== undefined) payload.specialization = data.specialization;
  if (data.languages !== undefined) payload.languages = data.languages;
  if (typeof data.tourismSince === 'number') payload.tourismSince = data.tourismSince;

  const { data: dto } = await apiClient.patch<UserProfileDto>(
    '/auth/me',
    payload,
  );
  return mapUserProfileDtoToUser(dto);
}

export async function uploadAvatar(file: File): Promise<User> {
  const body = new FormData();
  body.append('file', file);
  const { data } = await apiClient.postForm<UserProfileDto>('/auth/me/avatar', body);
  return mapUserProfileDtoToUser(data);
}

export async function deleteAvatar(): Promise<User> {
  const { data } = await apiClient.delete<UserProfileDto>('/auth/me/avatar');
  return mapUserProfileDtoToUser(data);
}
