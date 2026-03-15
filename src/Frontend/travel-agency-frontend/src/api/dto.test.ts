import { describe, expect, it } from 'vitest';
import type { AuthTokensDto } from './dto';

describe('dto', () => {
  describe('AuthTokensDto', () => {
    it('has required shape: accessToken and refreshToken', () => {
      const valid: AuthTokensDto = {
        accessToken: 'at-123',
        refreshToken: 'rt-456',
      };
      expect(valid.accessToken).toBe('at-123');
      expect(valid.refreshToken).toBe('rt-456');
    });

    it('accepts optional expiresAt', () => {
      const withExpiry: AuthTokensDto = {
        accessToken: 'at',
        refreshToken: 'rt',
        expiresAt: '2025-12-31T00:00:00Z',
      };
      expect(withExpiry.expiresAt).toBe('2025-12-31T00:00:00Z');
    });

    it('shape matches backend auth response contract', () => {
      const backendResponse = {
        accessToken: 'eyJ...',
        refreshToken: 'eyJ...',
        expiresAt: '2025-12-31T23:59:59Z',
      } satisfies AuthTokensDto;
      expect(backendResponse).toMatchObject({
        accessToken: expect.any(String),
        refreshToken: expect.any(String),
      });
    });
  });
});
