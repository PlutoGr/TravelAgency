import { afterEach, describe, expect, it } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { apiClient } from '@/api/client';
import { getManagerCard } from '@/api/managerCard';
import { clearSessionSignal, markSessionPresent } from '@/api/sessionSignal';
import ManagerShortCard from './ManagerShortCard';

const USER_ID = 'manager-1';

function httpResponse(
  config: InternalAxiosRequestConfig,
  status: number,
  data: unknown,
): AxiosResponse {
  return { data, status, statusText: String(status), headers: {}, config };
}

function httpError(
  config: InternalAxiosRequestConfig,
  status: number,
  detail: string,
): AxiosError {
  return new AxiosError(
    'request failed',
    String(status),
    config,
    null,
    httpResponse(config, status, { detail }),
  );
}

function renderCard() {
  return render(
    <ManagerShortCard userId={USER_ID} onClose={() => undefined} loadCard={getManagerCard} />,
  );
}

describe('manager card session refresh', () => {
  const originalAdapter = apiClient.defaults.adapter;

  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
    clearSessionSignal();
  });

  it('повторяет тот же GET один раз после обновления cookie', async () => {
    markSessionPresent();
    let cardCalls = 0;
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        return httpResponse(config, 200, { success: true });
      }
      if (url.includes(`/users/${USER_ID}/manager-card`)) {
        cardCalls += 1;
        if (cardCalls === 1) throw httpError(config, 401, 'The token is invalid.');
        return httpResponse(config, 200, {
          id: USER_ID,
          displayName: 'Марина Иванова',
        });
      }
      throw httpError(config, 404, 'missing');
    };

    renderCard();

    expect(await screen.findByText('Марина Иванова')).toBeInTheDocument();
    expect(refreshCalls).toBe(1);
    expect(cardCalls).toBe(2);
    expect(screen.queryByText('The token is invalid.')).not.toBeInTheDocument();
  });

  it('не открывает карточку, если cookie обновить не удалось', async () => {
    markSessionPresent();
    let cardCalls = 0;
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        throw httpError(config, 401, 'Refresh failed.');
      }
      if (url.includes(`/users/${USER_ID}/manager-card`)) {
        cardCalls += 1;
        throw httpError(config, 401, 'The token is invalid.');
      }
      throw httpError(config, 404, 'missing');
    };

    renderCard();

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось открыть карточку.');
    await waitFor(() => expect(refreshCalls).toBe(1));
    expect(cardCalls).toBe(1);
    expect(screen.queryByText('Марина Иванова')).not.toBeInTheDocument();
    expect(screen.queryByText('The token is invalid.')).not.toBeInTheDocument();
    expect(screen.queryByText('Refresh failed.')).not.toBeInTheDocument();
  });

  it('не запускает второй refresh, если повторный GET тоже 401', async () => {
    markSessionPresent();
    let cardCalls = 0;
    let refreshCalls = 0;
    apiClient.defaults.adapter = async (config) => {
      const url = config.url ?? '';
      if (url.includes('/auth/refresh')) {
        refreshCalls += 1;
        return httpResponse(config, 200, { success: true });
      }
      if (url.includes(`/users/${USER_ID}/manager-card`)) {
        cardCalls += 1;
        throw httpError(config, 401, 'The token is invalid.');
      }
      throw httpError(config, 404, 'missing');
    };

    renderCard();

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось открыть карточку.');
    await waitFor(() => expect(cardCalls).toBe(2));
    expect(refreshCalls).toBe(1);
    expect(screen.queryByText('The token is invalid.')).not.toBeInTheDocument();
  });
});
