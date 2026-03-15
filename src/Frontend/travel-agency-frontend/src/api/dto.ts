import type { AuthTokens } from '@/types';

/** Backend DTO: auth tokens response (login, register, refresh) */
export type AuthTokensDto = {
  accessToken: string;
  refreshToken: string;
  expiresAt?: string;
};

export function mapAuthTokensDtoToAuthTokens(dto: AuthTokensDto): AuthTokens {
  return {
    accessToken: dto.accessToken,
    refreshToken: dto.refreshToken,
  };
}
