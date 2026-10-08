import { useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useUIStore } from '@/store/uiStore';

/**
 * Handles ?auth=login query param (e.g. from 401 redirect).
 * Opens AuthModal and clears the param from URL.
 */
export default function AuthRedirectHandler() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const openAuthModal = useUIStore((s) => s.openAuthModal);

  useEffect(() => {
    const auth = searchParams.get('auth');
    const returnTo = searchParams.get('returnTo');
    if (auth === 'login') {
      openAuthModal('login', returnTo ? decodeURIComponent(returnTo) : undefined);
      const next = new URLSearchParams(searchParams);
      next.delete('auth');
      next.delete('returnTo');
      const qs = next.toString();
      navigate(qs ? `/?${qs}` : '/', { replace: true });
    }
  }, [searchParams, navigate, openAuthModal]);

  return null;
}
