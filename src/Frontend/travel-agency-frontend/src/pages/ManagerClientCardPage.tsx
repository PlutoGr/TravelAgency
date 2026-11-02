import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Avatar } from '@/components/ui';
import { BookingStatusBadge } from '@/components/booking';
import { Breadcrumbs } from '@/components/layout';
import { PageTransition } from '@/components/common';
import {
  addClientNote,
  clientBookingTitles,
  clientNoteFieldError,
  getClientCard,
  type ClientCard,
  type ClientNote,
} from '@/api/clientCards';
import { bookingCountLabel } from '@/domain/bookingCountLabel';
import { formatDate } from '@/utils/format';

const CARD_ERROR = 'Не удалось открыть карточку.';
const SAVE_ERROR = 'Не удалось сохранить заметку.';
const EMPTY_NOTE = 'Напишите текст заметки.';
const LONG_NOTE = 'Заметка не длиннее 500 символов.';
const NOTE_LIMIT = 500;

export default function ManagerClientCardPage() {
  const { id = '' } = useParams();
  const [loadedFor, setLoadedFor] = useState<string | null>(null);
  const [phase, setPhase] = useState<'loading' | 'error' | 'ready'>('loading');
  const [card, setCard] = useState<ClientCard | null>(null);
  const [titles, setTitles] = useState<Record<string, string>>({});
  const [draft, setDraft] = useState('');
  const [fieldError, setFieldError] = useState('');
  const [saveError, setSaveError] = useState('');
  const [saving, setSaving] = useState(false);

  if (loadedFor !== id && phase !== 'loading') {
    setLoadedFor(id);
    setPhase('loading');
    setCard(null);
    setTitles({});
    setDraft('');
    setFieldError('');
    setSaveError('');
  }

  useEffect(() => {
    let cancelled = false;
    setPhase('loading');
    setCard(null);
    setTitles({});
    getClientCard(id).then(
      async (value) => {
        if (cancelled) return;
        const tourTitles = await clientBookingTitles(value.bookings);
        if (cancelled) return;
        setLoadedFor(id);
        setCard(value);
        setTitles(Object.fromEntries(tourTitles));
        setPhase('ready');
      },
      () => {
        if (cancelled) return;
        setLoadedFor(id);
        setCard(null);
        setTitles({});
        setPhase('error');
      },
    );
    return () => {
      cancelled = true;
    };
  }, [id]);

  const crumbs = [
    { label: 'Панель менеджера', path: '/manager' },
    { label: 'Клиенты', path: '/manager/clients' },
    ...(card ? [{ label: card.displayName }] : []),
  ];

  async function submitNote() {
    if (!card || saving) return;
    const trimmed = draft.trim();
    if (trimmed.length === 0) {
      setFieldError(EMPTY_NOTE);
      setSaveError('');
      return;
    }
    if (trimmed.length > NOTE_LIMIT) {
      setFieldError(LONG_NOTE);
      setSaveError('');
      return;
    }

    setSaving(true);
    setFieldError('');
    setSaveError('');
    try {
      const note = await addClientNote(card.id, trimmed);
      setCard((current) =>
        current
          ? { ...current, notes: [note, ...current.notes.filter((item) => item.id !== note.id)] }
          : current,
      );
      setDraft('');
    } catch (error) {
      const message = clientNoteFieldError(error);
      if (message) setFieldError(message);
      else setSaveError(SAVE_ERROR);
    } finally {
      setSaving(false);
    }
  }

  return (
    <PageTransition>
      <div className="space-y-6">
        <Breadcrumbs items={crumbs} />
        {phase === 'loading' && <p>Загрузка карточки...</p>}
        {phase === 'error' && <p role="alert">{CARD_ERROR}</p>}
        {phase === 'ready' && card && (
          <>
            <header className="flex items-center gap-4">
              <Avatar src={card.avatarUrl} name={card.displayName} size="xl" />
              <div>
                <h1 className="font-heading text-2xl font-bold text-dark">{card.displayName}</h1>
                <p className="mt-2 text-sm text-dark">
                  <span className="mr-2 text-warm-gray">Email</span>
                  <a href={`mailto:${card.email}`} className="text-primary">
                    {card.email}
                  </a>
                </p>
                {card.phone ? (
                  <p className="mt-1 text-sm text-dark">
                    <span className="mr-2 text-warm-gray">Телефон</span>
                    <a href={`tel:${card.phone}`} className="text-primary">
                      {card.phone}
                    </a>
                  </p>
                ) : null}
              </div>
            </header>

            <div className="grid grid-cols-1 gap-6 lg:grid-cols-5">
              <section className="lg:col-span-3">
                <h2 className="font-heading text-lg font-semibold text-dark">Заявки</h2>
                {card.bookings.length === 0 ? (
                  <p className="mt-3 text-sm text-warm-gray">Заявок нет</p>
                ) : (
                  <>
                    <p className="mt-1 text-sm text-warm-gray">{bookingCountLabel(card.bookings.length)}</p>
                    <ul className="mt-3 space-y-3">
                      {card.bookings.map((booking) => (
                        <li key={booking.id} className="rounded-[16px] border border-sand p-4">
                          <p className="font-medium text-dark">{titles[booking.id] ?? 'Тур недоступен'}</p>
                          <div className="mt-2 flex flex-wrap items-center gap-3 text-sm">
                            <BookingStatusBadge status={booking.status} size="sm" />
                            <span className="text-warm-gray">{formatDate(booking.createdAt)}</span>
                          </div>
                          <Link
                            to={`/manager/bookings/${booking.id}`}
                            className="mt-3 inline-flex text-sm font-medium text-primary"
                          >
                            Открыть заявку
                          </Link>
                        </li>
                      ))}
                    </ul>
                  </>
                )}
              </section>

              <section className="lg:col-span-2">
                <h2 className="font-heading text-lg font-semibold text-dark">Заметки</h2>
                {card.notes.length === 0 ? (
                  <p className="mt-3 text-sm text-warm-gray">Заметок пока нет</p>
                ) : (
                  <ul className="mt-3 space-y-3">
                    {card.notes.map((note) => (
                      <li key={note.id}>
                        <NoteView note={note} />
                      </li>
                    ))}
                  </ul>
                )}
                <form
                  className="mt-4 space-y-2"
                  onSubmit={(event) => {
                    event.preventDefault();
                    void submitNote();
                  }}
                >
                  <label htmlFor="new-note" className="text-sm font-medium text-dark">
                    Новая заметка
                  </label>
                  <textarea
                    id="new-note"
                    value={draft}
                    placeholder="Просит вылет после 15 мая"
                    rows={3}
                    onChange={(event) => setDraft(event.target.value)}
                    className="w-full rounded-[12px] border border-sand px-3 py-2 text-sm text-dark outline-none focus:border-primary"
                  />
                  {fieldError ? (
                    <p role="alert" className="text-sm text-terracotta">
                      {fieldError}
                    </p>
                  ) : null}
                  {saveError ? (
                    <p role="alert" className="text-sm text-terracotta">
                      {saveError}
                    </p>
                  ) : null}
                  <button
                    type="submit"
                    disabled={saving}
                    className="rounded-xl bg-primary px-4 py-2 text-sm font-medium text-white disabled:cursor-wait disabled:opacity-60"
                  >
                    Добавить
                  </button>
                </form>
              </section>
            </div>
          </>
        )}
      </div>
    </PageTransition>
  );
}

function NoteView({ note }: { note: ClientNote }) {
  return (
    <div>
      <p className="text-sm text-dark">{note.text}</p>
      <p className="mt-1 text-xs text-warm-gray">
        {note.authorName} {formatDate(note.createdAt, 'd MMMM yyyy, HH:mm')}
      </p>
    </div>
  );
}
