import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import PageErrorBoundary from './PageErrorBoundary';

function Boom(): never {
  throw new Error('boom');
}

describe('PageErrorBoundary', () => {
  it('renders the fallback when a child throws', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <PageErrorBoundary>
        <Boom />
      </PageErrorBoundary>,
    );

    expect(screen.getByRole('alert')).toHaveTextContent('Не удалось показать страницу');
    expect(screen.getByRole('button', { name: 'Обновить' })).toBeInTheDocument();
  });
});
