import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import ScrollToTop from './ScrollToTop';

function renderAt(path: string, anchors: string[] = []) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <ScrollToTop />
      {anchors.map((id) => (
        <div key={id} id={id} />
      ))}
    </MemoryRouter>,
  );
}

describe('ScrollToTop', () => {
  beforeEach(() => {
    vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
    HTMLElement.prototype.scrollIntoView = vi.fn();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('scrolls to the top when the location has no hash', () => {
    renderAt('/tours');
    expect(window.scrollTo).toHaveBeenCalledWith(0, 0);
    expect(HTMLElement.prototype.scrollIntoView).not.toHaveBeenCalled();
  });

  it('scrolls to the hashed element instead of the top', async () => {
    renderAt('/#reviews', ['reviews']);
    await waitFor(() => {
      expect(document.getElementById('reviews')!.scrollIntoView).toHaveBeenCalled();
    });
    expect(window.scrollTo).not.toHaveBeenCalled();
  });

  it('retries until the hashed element is rendered', async () => {
    renderAt('/#contacts');
    expect(window.scrollTo).not.toHaveBeenCalled();

    const target = document.createElement('div');
    target.id = 'contacts';
    document.body.appendChild(target);

    await waitFor(() => {
      expect(target.scrollIntoView).toHaveBeenCalled();
    });
    expect(window.scrollTo).not.toHaveBeenCalled();
    target.remove();
  });
});
