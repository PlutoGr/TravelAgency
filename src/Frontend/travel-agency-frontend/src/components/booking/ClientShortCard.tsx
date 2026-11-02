import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Avatar, Modal } from '@/components/ui';
import { bookingCountLabel } from '@/domain/bookingCountLabel';
import type { ClientCard } from '@/api/clientCards';

const CARD_ERROR = 'Не удалось открыть карточку.';

type ClientShortCardProps = {
  clientId: string;
  onClose: () => void;
  loadCard: (clientId: string) => Promise<ClientCard>;
};

export default function ClientShortCard({ clientId, onClose, loadCard }: ClientShortCardProps) {
  const [loadedFor, setLoadedFor] = useState<string | null>(null);
  const [phase, setPhase] = useState<'loading' | 'error' | 'ready'>('loading');
  const [card, setCard] = useState<ClientCard | null>(null);

  if (loadedFor !== clientId && phase !== 'loading') {
    setLoadedFor(clientId);
    setPhase('loading');
    setCard(null);
  }

  useEffect(() => {
    let cancelled = false;
    loadCard(clientId).then(
      (value) => {
        if (cancelled) return;
        setLoadedFor(clientId);
        setCard(value);
        setPhase('ready');
      },
      () => {
        if (cancelled) return;
        setLoadedFor(clientId);
        setCard(null);
        setPhase('error');
      },
    );
    return () => {
      cancelled = true;
    };
  }, [clientId, loadCard]);

  return (
    <Modal isOpen onClose={onClose} dialogLabel="Карточка клиента">
      {phase === 'loading' && (
        <div
          role="status"
          aria-label="Загрузка"
          className="mx-auto h-8 w-8 animate-spin rounded-full border-3 border-sand border-t-primary"
        />
      )}
      {phase === 'error' && <p role="alert">{CARD_ERROR}</p>}
      {phase === 'ready' && card && (
        <div className="flex flex-col items-start gap-4">
          <div className="flex items-center gap-4">
            <Avatar src={card.avatarUrl} name={card.displayName} size="xl" />
            <div>
              <p className="font-heading text-xl font-semibold text-dark">{card.displayName}</p>
              {card.phone ? (
                <p className="mt-2 text-sm text-dark">
                  <span className="mr-2 text-warm-gray">Телефон</span>
                  <a href={`tel:${card.phone}`} className="text-primary">
                    {card.phone}
                  </a>
                </p>
              ) : null}
            </div>
          </div>
          <p className="text-sm text-dark">{bookingCountLabel(card.bookings.length)}</p>
          <Link
            to={`/manager/clients/${card.id}`}
            onClick={onClose}
            className="inline-flex rounded-xl bg-primary px-4 py-2.5 text-sm font-medium text-white"
          >
            Открыть карточку
          </Link>
        </div>
      )}
    </Modal>
  );
}
