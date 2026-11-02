import { useState } from 'react';
import { Button, Input } from '@/components/ui';
import { changePassword, requestEmailChange } from '@/api/account';
import { ACCOUNT_TEXT, accountFailureText, isEmailShape } from '@/domain/accountLinks';
import { PASSWORD_HINT } from '@/domain/profileForm';

function passwordProblem(password: string): string | undefined {
  if (password.length < 8 || password.length > 128) return ACCOUNT_TEXT.passwordRule;
  if (!/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/[0-9]/.test(password)) {
    return ACCOUNT_TEXT.passwordRule;
  }
  return undefined;
}

export function ProfilePasswordBlock() {
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | undefined>();
  const [notice, setNotice] = useState<string | undefined>();
  const [busy, setBusy] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError(undefined);
    setNotice(undefined);
    if (!current || !next || !confirm) {
      setError(ACCOUNT_TEXT.fillAll);
      return;
    }
    if (next !== confirm) {
      setError(ACCOUNT_TEXT.passwordMismatch);
      return;
    }
    const rule = passwordProblem(next);
    if (rule) {
      setError(rule);
      return;
    }

    setBusy(true);
    try {
      await changePassword(current, next, confirm);
      setCurrent('');
      setNext('');
      setConfirm('');
      setNotice(ACCOUNT_TEXT.passwordChanged);
    } catch (err) {
      setNotice(undefined);
      setError(accountFailureText(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form role="region" className="space-y-4" aria-label="Пароль" onSubmit={submit}>
      <h2 className="font-heading text-lg font-semibold text-dark">Пароль</h2>
      <Input
        id="settings-current-password"
        label="Текущий пароль"
        type="password"
        value={current}
        autoComplete="current-password"
        onChange={(event) => setCurrent(event.target.value)}
      />
      <div>
        <Input
          id="settings-new-password"
          label="Новый пароль"
          type="password"
          value={next}
          autoComplete="new-password"
          aria-describedby="profile-password-hint"
          onChange={(event) => setNext(event.target.value)}
        />
        <p id="profile-password-hint" className="mt-1.5 text-xs text-warm-gray">
          {PASSWORD_HINT}
        </p>
      </div>
      <Input
        id="settings-repeat-password"
        label="Повторите пароль"
        type="password"
        value={confirm}
        autoComplete="new-password"
        onChange={(event) => setConfirm(event.target.value)}
      />
      <Button type="submit" isLoading={busy}>
        Сменить пароль
      </Button>
      {notice && <p className="text-sm text-dark">{notice}</p>}
      {error && (
        <p role="alert" className="text-sm text-red-500">
          {error}
        </p>
      )}
    </form>
  );
}

export function ProfileEmailBlock({ currentEmail }: { currentEmail: string }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | undefined>();
  const [notice, setNotice] = useState<string | undefined>();
  const [busy, setBusy] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setError(undefined);
    setNotice(undefined);
    if (!isEmailShape(email)) {
      setError(ACCOUNT_TEXT.emailInvalid);
      return;
    }

    setBusy(true);
    try {
      const data = await requestEmailChange(email, password);
      setEmail('');
      setPassword('');
      setNotice(data.message || ACCOUNT_TEXT.emailSent);
    } catch (err) {
      setError(accountFailureText(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form role="region" className="space-y-4" aria-label="Email" onSubmit={submit}>
      <h2 className="font-heading text-lg font-semibold text-dark">Email</h2>
      <Input id="settings-current-email" label="Текущий email" value={currentEmail} disabled />
      <Input
        id="settings-new-email"
        label="Новый email"
        type="email"
        value={email}
        autoComplete="email"
        onChange={(event) => setEmail(event.target.value)}
      />
      <Input
        id="settings-email-password"
        label="Текущий пароль"
        type="password"
        value={password}
        autoComplete="current-password"
        onChange={(event) => setPassword(event.target.value)}
      />
      <Button type="submit" isLoading={busy}>
        Сменить email
      </Button>
      {notice && <p className="text-sm text-dark">{notice}</p>}
      {error && (
        <p role="alert" className="text-sm text-red-500">
          {error}
        </p>
      )}
    </form>
  );
}
