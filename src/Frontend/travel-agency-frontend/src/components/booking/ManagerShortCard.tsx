import { useEffect, useState } from 'react';
import { Avatar, Modal } from '@/components/ui';
import type { ManagerPublicCard } from '@/api/managerCard';

const CARD_ERROR = 'Не удалось открыть карточку.';

type ManagerShortCardProps = {
  userId: string;
  onClose: () => void;
  loadCard: (userId: string) => Promise<ManagerPublicCard>;
};

export default function ManagerShortCard({ userId, onClose, loadCard }: ManagerShortCardProps) {
  const [loadedFor, setLoadedFor] = useState<string | null>(null);
  const [phase, setPhase] = useState<'loading' | 'error' | 'ready'>('loading');
  const [card, setCard] = useState<ManagerPublicCard | null>(null);

  if (loadedFor !== userId && phase !== 'loading') {
    setLoadedFor(userId);
    setPhase('loading');
    setCard(null);
  }

  useEffect(() => {
    let cancelled = false;
    loadCard(userId).then(
      (value) => {
        if (cancelled) return;
        setLoadedFor(userId);
        setCard(value);
        setPhase('ready');
      },
      () => {
        if (cancelled) return;
        setLoadedFor(userId);
        setCard(null);
        setPhase('error');
      },
    );
    return () => {
      cancelled = true;
    };
  }, [userId, loadCard]);

  return (
    <Modal isOpen onClose={onClose} dialogLabel="Карточка менеджера">
      {phase === 'loading' && (
        <div
          role="status"
          aria-label="Загрузка"
          className="mx-auto h-8 w-8 animate-spin rounded-full border-3 border-sand border-t-primary"
        />
      )}
      {phase === 'error' && <p role="alert">{CARD_ERROR}</p>}
      {phase === 'ready' && card && (
        <div className="flex items-center gap-3">
          <Avatar src={card.avatarUrl} name={card.displayName} size="lg" />
          <p data-testid="manager-card-name" className="font-heading text-lg font-semibold text-dark">
            {card.displayName}
          </p>
        </div>
      )}
    </Modal>
  );
}
