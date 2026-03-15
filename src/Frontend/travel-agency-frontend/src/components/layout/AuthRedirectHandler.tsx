import { useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useUIStore } from '@/store/uiStore';

/**
 * Handles ?auth=login query param (e.g. from 401 redirect).
 * Opens AuthModal and clears the param from URL.
 */
export default function AuthRedirectHandler() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const openAuthModal = useUIStore((s) => s.openAuthModal);

  useEffect(() => {
    const auth = searchParams.get('auth');
    if (auth === 'login') {
      openAuthModal('login');
      const next = new URLSearchParams(searchParams);
      next.delete('auth');
      const qs = next.toString();
      navigate(qs ? `/?${qs}` : '/', { replace: true });
    }
  }, [searchParams, navigate, openAuthModal]);

  return null;
}
