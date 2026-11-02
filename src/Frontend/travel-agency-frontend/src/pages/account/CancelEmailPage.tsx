import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { cancelEmailChange } from '@/api/account';
import { ACCOUNT_TEXT, accountFailureText } from '@/domain/accountLinks';
import { AccountColumn, AccountStatus, useLinkToken } from './accountPage';

export default function CancelEmailPage() {
  const token = useLinkToken();
  const [text, setText] = useState<string | undefined>();

  useEffect(() => {
    let cancelled = false;
    cancelEmailChange(token)
      .then((data) => {
        if (cancelled) return;
        setText(data.outcome === 'cancelled' ? ACCOUNT_TEXT.emailCancelled : ACCOUNT_TEXT.linkInvalid);
      })
      .catch((err) => {
        if (!cancelled) setText(accountFailureText(err));
      });
    return () => {
      cancelled = true;
    };
  }, [token]);

  return (
    <AccountColumn>
      <h1 className="sr-only">Отмена смены адреса</h1>
      <AccountStatus>{text ?? 'Проверяем ссылку...'}</AccountStatus>
      <Link to="/" className="mt-6 inline-block text-sm text-primary">
        На главную
      </Link>
    </AccountColumn>
  );
}
