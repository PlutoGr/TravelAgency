import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

const MAX_HASH_FRAMES = 60;
const ALIGN_MS = 1800;

export default function ScrollToTop() {
  const { pathname, hash } = useLocation();

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
    let scrollingFromAlign = false;
    const userEvents = ['wheel', 'touchstart', 'touchmove'] as const;

    const stopForUser = () => {
      userStopped = true;
    };

    const stopOnUserScroll = () => {
      if (!scrollingFromAlign) userStopped = true;
    };

    const cleanupAlign = () => {
      window.cancelAnimationFrame(alignFrame);
      observer?.disconnect();
      observer = null;
      for (const eventName of userEvents) {
        window.removeEventListener(eventName, stopForUser);
      }
      window.removeEventListener('scroll', stopOnUserScroll);
    };

    const followLayout = (target: HTMLElement) => {
      const deadline = performance.now() + ALIGN_MS;
      let lastTop = target.getBoundingClientRect().top + window.scrollY;

      const align = () => {
        if (cancelled || userStopped || performance.now() > deadline) return;
        const nextTop = target.getBoundingClientRect().top + window.scrollY;
        if (Math.abs(nextTop - lastTop) > 1) {
          scrollingFromAlign = true;
          target.scrollIntoView();
          scrollingFromAlign = false;
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
      window.addEventListener('scroll', stopOnUserScroll, { passive: true });
      alignFrame = window.requestAnimationFrame(watch);
    };

    const scrollToHash = () => {
      if (cancelled) return;
      const target = document.getElementById(id);
      if (target) {
        scrollingFromAlign = true;
        target.scrollIntoView();
        scrollingFromAlign = false;
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
  }, [pathname, hash]);

  return null;
}
