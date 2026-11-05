import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { CloseButton } from '@/components/ui';
import { useWideScreen } from '@/components/common/useWideScreen';
import ManagerCardFace from '@/components/profile/ManagerCardFace';
import type { ManagerPublicCard } from '@/api/managerCard';

const CARD_ERROR = 'Не удалось открыть карточку.';

type ManagerShortCardProps = {
  userId: string;
  onClose: () => void;
  loadCard: (userId: string) => Promise<ManagerPublicCard>;
};

export default function ManagerShortCard({ userId, onClose, loadCard }: ManagerShortCardProps) {
  const wide = useWideScreen();
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

  useEffect(() => {
    const previous = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', onKey);
    return () => {
      document.body.style.overflow = previous;
      document.removeEventListener('keydown', onKey);
    };
  }, [onClose]);

  return createPortal(
    <div className="fixed inset-0 z-50">
      <div data-card-overlay="" className="absolute inset-0 bg-[#1c1915]/45" onClick={onClose} />
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Карточка менеджера"
        className={
          wide
            ? 'absolute inset-y-0 right-0 flex w-full max-w-[420px] flex-col bg-[#fffcf8] shadow-[0_24px_60px_#30241214]'
            : 'absolute inset-x-0 bottom-0 flex max-h-[min(92vh,100%)] flex-col rounded-t-[28px] bg-[#fffcf8] shadow-[0_24px_60px_#30241214]'
        }
      >
        <div className="flex items-center justify-between px-5 py-4">
          <h2 className="font-heading text-lg font-semibold text-[#1c1915]">Ваш менеджер</h2>
          <CloseButton onClick={onClose} />
        </div>
        <div className="overflow-y-auto px-4 pb-6">
          {phase === 'loading' && (
            <div
              role="status"
              aria-label="Загрузка"
              className="mx-auto h-8 w-8 animate-spin rounded-full border-3 border-sand border-t-[#6e7f62]"
            />
          )}
          {phase === 'error' && <p role="alert">{CARD_ERROR}</p>}
          {phase === 'ready' && card && (
            <ManagerCardFace
              displayName={card.displayName}
              avatarUrl={card.avatarUrl}
              position={card.position}
              tourismSince={card.tourismSince}
              about={card.about}
              tags={card.tags}
            />
          )}
        </div>
      </div>
    </div>,
    document.body,
  );
}
