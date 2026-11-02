import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, Input } from '@/components/ui';
import { completePasswordReset, inspectPasswordReset } from '@/api/account';
import { ACCOUNT_TEXT, accountFailureText } from '@/domain/accountLinks';
import { useAuthStore } from '@/store/authStore';
import { useUIStore } from '@/store/uiStore';
import { AccountAlert, AccountColumn, AccountStatus, useLinkToken } from './accountPage';

type Phase = 'loading' | 'ready' | 'invalid' | 'unavailable' | 'changed' | 'changed-other';

export default function NewPasswordPage() {
  const token = useLinkToken();
  const navigate = useNavigate();
  const logout = useAuthStore((state) => state.logout);
  const openAuthModal = useUIStore((state) => state.openAuthModal);
  const [phase, setPhase] = useState<Phase>('loading');
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | undefined>();
  const [busy, setBusy] = useState(false);
  const [inspectAttempt, setInspectAttempt] = useState(0);

  useEffect(() => {
    let cancelled = false;
    inspectPasswordReset(token)
      .then((data) => {
        if (!cancelled) {
          setError(undefined);
          setPhase(data.outcome === 'ready' ? 'ready' : 'invalid');
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(accountFailureText(err));
          setPhase('unavailable');
        }
      });
    return () => {
      cancelled = true;
    };
  }, [token, inspectAttempt]);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError(undefined);
    if (!password) {
      setError(ACCOUNT_TEXT.passwordEmpty);
      return;
    }
    if (password !== confirm) {
      setError(ACCOUNT_TEXT.passwordMismatch);
      return;
    }

    setBusy(true);
    try {
      const data = await completePasswordReset(token, password, confirm);
      if (data.outcome === 'invalid') {
        setPhase('invalid');
        return;
      }
      if (data.outcome === 'changed' && data.session === 'other') {
        setPhase('changed-other');
        return;
      }
      if (data.outcome === 'changed' && data.session === 'current') {
        try {
          await logout();
        } catch {
          useAuthStore.getState().markSessionExpired();
        }
      }
      setPhase('changed');
    } catch (err) {
      setError(accountFailureText(err));
    } finally {
      setBusy(false);
    }
  }

  function loginAgain() {
    openAuthModal('login');
    navigate('/?auth=login');
  }

  return (
    <AccountColumn>
      <h1 className="font-heading text-2xl font-bold text-dark">Новый пароль</h1>
      {phase === 'loading' && <AccountStatus>Проверяем ссылку...</AccountStatus>}
      {phase === 'ready' && (
        <form onSubmit={submit} className="mt-6 flex flex-col gap-4">
          <div>
            <Input
              label="Новый пароль"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              autoComplete="new-password"
              aria-describedby="new-password-hint"
            />
            <p id="new-password-hint" className="mt-1.5 text-xs text-warm-gray">
              {ACCOUNT_TEXT.passwordHint}
            </p>
          </div>
          <Input
            label="Повторите пароль"
            type="password"
            value={confirm}
            onChange={(event) => setConfirm(event.target.value)}
            autoComplete="new-password"
          />
          <Button type="submit" fullWidth isLoading={busy}>
            Задать пароль
          </Button>
          {error && <AccountAlert>{error}</AccountAlert>}
        </form>
      )}
      {phase === 'changed' && (
        <div className="mt-6 space-y-4">
          <AccountStatus>{ACCOUNT_TEXT.resetChanged}</AccountStatus>
          <Button type="button" fullWidth onClick={loginAgain}>
            Войти
          </Button>
        </div>
      )}
      {phase === 'changed-other' && (
        <AccountStatus>{ACCOUNT_TEXT.resetChangedOther}</AccountStatus>
      )}
      {phase === 'unavailable' && (
        <div className="mt-6 space-y-4">
          <AccountAlert>{error ?? ACCOUNT_TEXT.requestFailed}</AccountAlert>
          <Button type="button" fullWidth onClick={() => setInspectAttempt((value) => value + 1)}>
            Попробовать ещё раз
          </Button>
        </div>
      )}
      {phase === 'invalid' && (
        <div className="mt-6 space-y-4">
          <AccountStatus>{ACCOUNT_TEXT.resetInvalid}</AccountStatus>
          <Button type="button" fullWidth onClick={() => navigate('/?auth=forgot')}>
            Запросить ещё раз
          </Button>
        </div>
      )}
    </AccountColumn>
  );
}
