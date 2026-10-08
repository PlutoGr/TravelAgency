import { useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useUIStore } from '@/store/uiStore';
import { resolveReturnTo } from '@/utils/safeReturnTo';

function readReturnTo(raw: string | null): string | undefined {
  if (!raw) return undefined;
  try {
    return decodeURIComponent(raw);
  } catch {
    return undefined;
  }
}

/**
 * Handles ?auth=login query param (e.g. from 401 redirect).
 * Opens AuthModal and clears the param from URL.
 * returnTo is kept only when it is an internal path.
 */
export default function AuthRedirectHandler() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const openAuthModal = useUIStore((s) => s.openAuthModal);

  useEffect(() => {
    const auth = searchParams.get('auth');
    const returnTo = searchParams.get('returnTo');
    if (auth === 'login') {
      openAuthModal('login', resolveReturnTo(readReturnTo(returnTo)));
      const next = new URLSearchParams(searchParams);
      next.delete('auth');
      next.delete('returnTo');
      const qs = next.toString();
      navigate(qs ? `/?${qs}` : '/', { replace: true });
    }
  }, [searchParams, navigate, openAuthModal]);

  return null;
}
