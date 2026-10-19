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

  it('stops retrying an unknown hash and scrolls to the top', () => {
    let queued: FrameRequestCallback | null = null;
    let scheduled = 0;
    vi.spyOn(window, 'requestAnimationFrame').mockImplementation((callback) => {
      queued = callback;
      scheduled += 1;
      return scheduled;
    });
    vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => {});

    const view = renderAt('/#foo');
    for (let i = 0; i < 70 && queued; i += 1) {
      const callback: FrameRequestCallback = queued;
      queued = null;
      callback(0);
    }

    expect(window.scrollTo).toHaveBeenCalledWith(0, 0);
    expect(HTMLElement.prototype.scrollIntoView).not.toHaveBeenCalled();
    expect(scheduled).toBeGreaterThan(0);
    expect(scheduled).toBeLessThanOrEqual(60);
    expect(queued).toBeNull();

    view.unmount();
    expect(window.cancelAnimationFrame).toHaveBeenCalled();
  });

  it('does not crash on a malformed hash and scrolls to the top', () => {
    expect(() => renderAt('/#%E0%A4%A')).not.toThrow();
    expect(window.scrollTo).toHaveBeenCalledWith(0, 0);
    expect(HTMLElement.prototype.scrollIntoView).not.toHaveBeenCalled();
  });

  it('re-aligns the hash target after a late layout shift and stops on wheel', () => {
    const frames: FrameRequestCallback[] = [];
    vi.spyOn(window, 'requestAnimationFrame').mockImplementation((callback) => {
      frames.push(callback);
      return frames.length;
    });
    vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => {});

    let docTop = 500;
    vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockImplementation(
      () => ({
        x: 0,
        y: docTop,
        top: docTop,
        left: 0,
        right: 10,
        bottom: docTop + 10,
        width: 10,
        height: 10,
        toJSON() {
          return {};
        },
      }),
    );

    const observed: Element[] = [];
    let disconnected = false;
    class FakeResizeObserver {
      observe(element: Element) {
        observed.push(element);
      }
      unobserve() {}
      disconnect() {
        disconnected = true;
      }
    }
    vi.stubGlobal('ResizeObserver', FakeResizeObserver);

    const view = renderAt('/#reviews', ['reviews']);
    const target = document.getElementById('reviews')!;
    expect(target.scrollIntoView).toHaveBeenCalledTimes(1);
    expect(observed).toContain(document.body);
    expect(observed).toContain(target);

    docTop = 900;
    frames[0](0);
    expect(target.scrollIntoView).toHaveBeenCalledTimes(2);

    window.dispatchEvent(new Event('wheel'));
    docTop = 1400;
    frames[frames.length - 1](0);
    expect(target.scrollIntoView).toHaveBeenCalledTimes(2);

    view.unmount();
    expect(disconnected).toBe(true);
    expect(window.cancelAnimationFrame).toHaveBeenCalled();
    vi.unstubAllGlobals();
  });
});
