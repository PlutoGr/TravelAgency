import { useEffect, useState } from 'react';

const QUERY = '(min-width: 768px)';

function readWide(): boolean {
  if (typeof window === 'undefined') return false;
  if (typeof window.matchMedia === 'function') return window.matchMedia(QUERY).matches;
  return window.innerWidth >= 768;
}

export function useWideScreen(): boolean {
  const [wide, setWide] = useState(readWide);

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return;
    const media = window.matchMedia(QUERY);
    const onChange = () => setWide(media.matches);
    onChange();
    media.addEventListener('change', onChange);
    return () => media.removeEventListener('change', onChange);
  }, []);

  return wide;
}
