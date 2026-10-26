import axios from 'axios';
import type {
  AdminUserDetails,
  AdminUserList,
  AdminUserMutation,
  AdminUserQuery,
} from '@/domain/adminUsers';
import { apiClient } from './client';

export async function getAdminUsers(query: AdminUserQuery = {}): Promise<AdminUserList> {
  const { data } = await apiClient.get<AdminUserList>('/admin/users', { params: query });
  return data;
}

export async function getAdminUser(id: string): Promise<AdminUserDetails> {
  const { data } = await apiClient.get<AdminUserDetails>(`/admin/users/${id}`);
  return data;
}

export async function changeAdminUserRole(id: string, role: string): Promise<AdminUserMutation> {
  const { data } = await apiClient.put<AdminUserMutation>(`/admin/users/${id}/role`, { role });
  return data;
}

export async function blockAdminUser(id: string, reason: string): Promise<AdminUserMutation> {
  const { data } = await apiClient.post<AdminUserMutation>(`/admin/users/${id}/block`, { reason });
  return data;
}

export async function unblockAdminUser(id: string): Promise<AdminUserMutation> {
  const { data } = await apiClient.post<AdminUserMutation>(`/admin/users/${id}/unblock`);
  return data;
}

export function adminUserErrorMessage(error: unknown, fallback: string): string {
  if (!axios.isAxiosError(error)) return fallback;
  const data = error.response?.data;
  if (!data || typeof data !== 'object') return fallback;

  const detail = 'detail' in data && typeof data.detail === 'string' ? data.detail.trim() : '';
  if (detail && detail !== 'One or more validation errors occurred.') return detail;

  const errors = 'errors' in data ? data.errors : undefined;
  if (errors && typeof errors === 'object') {
    for (const value of Object.values(errors as Record<string, unknown>)) {
      const text = Array.isArray(value) ? String(value[0] ?? '') : String(value ?? '');
      if (text.trim()) return text.trim();
    }
  }

  return fallback;
}
