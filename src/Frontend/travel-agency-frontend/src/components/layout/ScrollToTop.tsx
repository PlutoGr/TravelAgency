import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

const MAX_HASH_FRAMES = 60;
const ALIGN_MS = 1800;

export default function ScrollToTop() {
  const { pathname, hash, key } = useLocation();

  useEffect(() => {
    if (!hash) {
      window.scrollTo(0, 0);
      return;
    }

    let id: string;
    try {
      id = decodeURIComponent(hash.slice(1));
    } catch (error) {
      if (error instanceof URIError) {
        window.scrollTo(0, 0);
        return;
      }
      throw error;
    }

    const startedAt = performance.now();
    let frame = 0;
    let attempts = 0;
    let cancelled = false;
    let userStopped = false;
    let alignFrame = 0;
    let observer: ResizeObserver | null = null;
    const userEvents = ['wheel', 'touchstart', 'touchmove'] as const;
    const scrollKeys = new Set([
      'PageUp',
      'PageDown',
      'ArrowUp',
      'ArrowDown',
      'ArrowLeft',
      'ArrowRight',
      ' ',
      'Spacebar',
      'Home',
      'End',
    ]);

    const stopForUser = () => {
      userStopped = true;
    };

    const stopOnKey = (event: KeyboardEvent) => {
      if (scrollKeys.has(event.key)) userStopped = true;
    };

    const cleanupAlign = () => {
      window.cancelAnimationFrame(alignFrame);
      observer?.disconnect();
      observer = null;
      for (const eventName of userEvents) {
        window.removeEventListener(eventName, stopForUser);
      }
      window.removeEventListener('keydown', stopOnKey);
    };

    const followLayout = (target: HTMLElement) => {
      const deadline = performance.now() + ALIGN_MS;
      let lastTop = target.getBoundingClientRect().top + window.scrollY;

      const align = () => {
        if (cancelled || userStopped || performance.now() > deadline) return;
        const nextTop = target.getBoundingClientRect().top + window.scrollY;
        if (Math.abs(nextTop - lastTop) > 1) {
          target.scrollIntoView();
          lastTop = target.getBoundingClientRect().top + window.scrollY;
        }
      };

      const watch = () => {
        if (cancelled || userStopped || performance.now() > deadline) return;
        align();
        alignFrame = window.requestAnimationFrame(watch);
      };

      if (typeof ResizeObserver !== 'undefined') {
        observer = new ResizeObserver(() => align());
        observer.observe(document.body);
        observer.observe(target);
      }
      for (const eventName of userEvents) {
        window.addEventListener(eventName, stopForUser, { passive: true });
      }
      window.addEventListener('keydown', stopOnKey);
      alignFrame = window.requestAnimationFrame(watch);
    };

    const scrollToHash = () => {
      if (cancelled) return;
      const target = document.getElementById(id);
      if (target) {
        target.scrollIntoView();
        followLayout(target);
        return;
      }
      attempts += 1;
      const timedOut = performance.now() - startedAt >= 2000;
      if (attempts >= MAX_HASH_FRAMES || timedOut) {
        window.scrollTo(0, 0);
        return;
      }
      frame = window.requestAnimationFrame(scrollToHash);
    };

    scrollToHash();

    return () => {
      cancelled = true;
      window.cancelAnimationFrame(frame);
      cleanupAlign();
    };
  }, [pathname, hash, key]);

  return null;
}
