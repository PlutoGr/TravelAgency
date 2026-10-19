import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import Footer from './Footer';

function renderFooter() {
  return render(
    <MemoryRouter>
      <Footer />
    </MemoryRouter>,
  );
}

describe('Footer', () => {
  it('keeps tours and reviews and drops missing sections', () => {
    renderFooter();

    for (const label of ['Направления', 'О нас', 'Блог', 'Карьера', 'FAQ']) {
      expect(screen.queryByRole('link', { name: label })).not.toBeInTheDocument();
    }
    expect(screen.queryByRole('heading', { name: 'Компания' })).not.toBeInTheDocument();

    expect(screen.getByRole('link', { name: 'Популярные туры' })).toHaveAttribute('href', '/tours');
    expect(screen.getByRole('link', { name: 'Отзывы' })).toHaveAttribute('href', '/#reviews');
  });
});
