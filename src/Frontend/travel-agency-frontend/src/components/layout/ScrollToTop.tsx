import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

const MAX_HASH_FRAMES = 60;

export default function ScrollToTop() {
  const { pathname, hash } = useLocation();

  useEffect(() => {
    if (!hash) {
      window.scrollTo(0, 0);
      return;
    }

    const id = decodeURIComponent(hash.slice(1));
    const startedAt = performance.now();
    let frame = 0;
    let attempts = 0;
    let cancelled = false;

    const scrollToHash = () => {
      if (cancelled) return;
      const target = document.getElementById(id);
      if (target) {
        target.scrollIntoView();
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
    };
  }, [pathname, hash]);

  return null;
}
