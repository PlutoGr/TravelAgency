import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent, { PointerEventsCheckLevel } from '@testing-library/user-event';
import Modal from './Modal';

function following(node: Node, later: Node) {
  return (node.compareDocumentPosition(later) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0;
}

describe('Modal close control', () => {
  it('closes from the round button and from the icon, including the edges of the circle', async () => {
    const user = userEvent.setup({ pointerEventsCheck: PointerEventsCheckLevel.Never });
    const onClose = vi.fn();

    const { rerender } = render(
      <Modal isOpen onClose={onClose}>
        <div className="relative" data-testid="tabs">
          Вкладки
        </div>
      </Modal>,
    );

    const button = screen.getByRole('button', { name: 'Закрыть' });
    const icon = button.querySelector('svg');
    const tabs = screen.getByTestId('tabs');
    const body = tabs.parentElement;

    expect(icon).not.toBeNull();
    expect(button).toHaveClass('absolute', 'z-10', 'h-8', 'w-8', 'rounded-full');
    expect(icon).toHaveClass('pointer-events-none');
    expect(getComputedStyle(icon as Element).pointerEvents).toBe('none');
    expect(body).toHaveClass('pt-14');
    expect(following(button, tabs)).toBe(true);
    expect(tabs).toHaveClass('relative');
    expect(tabs.className).not.toMatch(/\bz-\d+/);

    await user.click(button);
    await user.pointer([
      { target: button, coords: { x: 1, y: 1 }, keys: '[MouseLeft]' },
      { target: button, coords: { x: 30, y: 4 }, keys: '[MouseLeft]' },
      { target: button, coords: { x: 16, y: 16 }, keys: '[MouseLeft]' },
    ]);
    await user.click(icon as Element);

    expect(onClose).toHaveBeenCalledTimes(5);

    onClose.mockClear();
    rerender(
      <Modal isOpen onClose={onClose} title="Заявка">
        <p>Форма</p>
      </Modal>,
    );

    const titled = screen.getByRole('button', { name: 'Закрыть' });
    expect(titled).toHaveClass('relative', 'z-10');
    expect(titled.parentElement).toHaveClass('z-10');
    await user.click(titled);
    await user.click(titled.querySelector('svg') as Element);
    expect(onClose).toHaveBeenCalledTimes(2);
  });
});
