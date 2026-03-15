import { describe, expect, it } from 'vitest';
import { apiClient } from './client';

describe('client', () => {
  describe('apiClient', () => {
    it('has withCredentials set to true for cookie-based auth', () => {
      expect(apiClient.defaults.withCredentials).toBe(true);
    });

    it('has baseURL from env or default', () => {
      expect(apiClient.defaults.baseURL).toBeDefined();
    });

    it('has Content-Type application/json header', () => {
      expect(apiClient.defaults.headers['Content-Type']).toBe(
        'application/json',
      );
    });
  });
});
