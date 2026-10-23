import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import toast from 'react-hot-toast';
import Footer from './Footer';

vi.mock('react-hot-toast', () => ({
  default: { error: vi.fn(), success: vi.fn() },
}));

function renderFooter() {
  return render(
    <MemoryRouter>
      <Footer />
    </MemoryRouter>,
  );
}

function sectionByHeading(name: string): HTMLElement {
  const heading = screen.getByRole('heading', { name });
  const section = heading.parentElement;
  if (!(section instanceof HTMLElement)) {
    throw new Error(`блок «${name}» не найден`);
  }
  return section;
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

    for (const social of ['VK', 'TG', 'YT']) {
      expect(screen.queryByRole('link', { name: social })).not.toBeInTheDocument();
    }
  });

  it('shows a placeholder phone and replaces email and address', () => {
    renderFooter();

    const contacts = within(sectionByHeading('Контакты'));
    const items = contacts.getAllByRole('listitem');
    expect(items).toHaveLength(3);

    const phone = screen.getByText('+7 999 999-99-99');
    expect(items[0]).toContainElement(phone);
    expect(items[0]).toHaveTextContent('Появится позже');
    expect(phone.closest('a')).toBeNull();
    expect(document.querySelector('a[href^="tel:"]')).toBeNull();

    expect(items[1]).toHaveTextContent(/^Появится позже$/);
    expect(items[2]).toHaveTextContent(/^Появится позже$/);
    expect(contacts.getByRole('list').textContent).not.toMatch(/сайт|website|https?:|www\.|\.ru/i);

    expect(screen.queryByText('+7 (800) 555-35-35')).not.toBeInTheDocument();
    expect(screen.queryByText('info@travelagency.ru')).not.toBeInTheDocument();
    expect(screen.queryByText('Москва, ул. Тверская 1')).not.toBeInTheDocument();
    expect(screen.queryByText(/Тверская/)).not.toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /сайт/i })).not.toBeInTheDocument();
  });

  it('keeps the newsletter inactive without a success toast', () => {
    renderFooter();

    const newsletter = sectionByHeading('Подпишитесь на рассылку');
    expect(newsletter).toHaveTextContent('Появится позже');

    const field = screen.getByPlaceholderText('Ваш email');
    const button = screen.getByRole('button', { name: 'Подписаться' });
    expect(field).toBeDisabled();
    expect(button).toBeDisabled();
    expect(field).toHaveValue('');

    const form = field.closest('form');
    expect(form).not.toBeNull();
    fireEvent.submit(form!);

    expect(toast.success).not.toHaveBeenCalled();
    expect(screen.queryByText('Спасибо за подписку!')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Подпишитесь на рассылку' })).toBeInTheDocument();
  });
});
