import { useState, useEffect, useRef, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Send, Paperclip, MessageSquare } from 'lucide-react';
import { format, isToday, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';
import clsx from 'clsx';
import toast from 'react-hot-toast';
import type { ChatMessage } from '@/types';
import { getClientCard } from '@/api/clientCards';
import { getManagerCard, type ManagerPublicCard } from '@/api/managerCard';
import { Avatar } from '@/components/ui';
import {
  getMessages,
  sendMessage,
  subscribeToMessages,
  disconnectChat,
} from '@/api/chat';
import ClientShortCard from './ClientShortCard';
import ManagerShortCard from './ManagerShortCard';

interface ChatWindowProps {
  bookingId: string;
  currentUserId: string;
  currentUserName: string;
  currentUserRole: 'client' | 'manager' | 'admin';
  /** Аватар сессии (`user.avatar` с `GET /api/v1/auth/me`). Отдельный запрос ради кружка не делается. */
  currentUserAvatar?: string;
}

function photoSrc(url: string | undefined): string | undefined {
  return typeof url === 'string' && url.trim() !== '' ? url : undefined;
}

type PersonFace = { src?: string; name?: string };

function trimmedName(value: string | undefined): string | undefined {
  if (typeof value !== 'string') return undefined;
  const name = value.trim();
  return name === '' ? undefined : name;
}

function personFace(src: string | undefined, name: string | undefined): PersonFace {
  const photo = photoSrc(src);
  const label = trimmedName(name);
  return {
    ...(photo ? { src: photo } : {}),
    ...(label ? { name: label } : {}),
  };
}

function sameFace(prev: PersonFace | undefined, next: PersonFace): boolean {
  return prev?.src === next.src && prev?.name === next.name;
}

function cardButtonName(who: 'manager' | 'client', name: string | undefined): string {
  const title = who === 'manager' ? 'Карточка менеджера' : 'Карточка клиента';
  return name ? `${title}, ${name}` : title;
}

type BubbleRead = 'session' | 'manager' | 'client' | 'none';

function bubbleRead(
  msg: Pick<ChatMessage, 'senderId' | 'senderRole'>,
  currentUserId: string,
  viewerIsClient: boolean,
): BubbleRead {
  if (msg.senderRole === 'system') return 'none';
  if (msg.senderId === currentUserId) return 'session';
  if (msg.senderRole === 'manager' || msg.senderRole === 'admin') return 'manager';
  if (msg.senderRole === 'client' && !viewerIsClient) return 'client';
  return 'none';
}

function formatMessageTime(dateStr: string): string {
  const date = parseISO(dateStr);
  if (isToday(date)) return format(date, 'HH:mm');
  return format(date, 'd MMM HH:mm', { locale: ru });
}

function formatDateSeparator(dateStr: string): string {
  const date = parseISO(dateStr);
  if (isToday(date)) return 'Сегодня';
  return format(date, 'd MMMM yyyy', { locale: ru });
}

function groupMessagesByDate(messages: ChatMessage[]) {
  const groups: { date: string; messages: ChatMessage[] }[] = [];

  for (const msg of messages) {
    const dateKey = format(parseISO(msg.createdAt), 'yyyy-MM-dd');
    const lastGroup = groups[groups.length - 1];

    if (lastGroup && lastGroup.date === dateKey) {
      lastGroup.messages.push(msg);
    } else {
      groups.push({ date: dateKey, messages: [msg] });
    }
  }

  return groups;
}

export default function ChatWindow({
  bookingId,
  currentUserId,
  currentUserName,
  currentUserRole,
  currentUserAvatar,
}: ChatWindowProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputText, setInputText] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSending, setIsSending] = useState(false);
  const [cardUserId, setCardUserId] = useState<string | null>(null);
  const [cardClientId, setCardClientId] = useState<string | null>(null);
  const [people, setPeople] = useState<Record<string, PersonFace>>({});
  const scrollRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const cards = useRef(new Map<string, Promise<ManagerPublicCard>>());
  const alive = useRef(0);
  const avatarSeq = useRef(new Map<string, number>());
  const lastViewerKey = useRef<string | null>(null);
  const messagesRef = useRef(messages);
  const knownIds = useRef(new Set<string>());
  const viewerIsClient = currentUserRole === 'client';
  const viewerKey = `${bookingId}\0${currentUserId}\0${viewerIsClient ? '1' : '0'}`;
  const viewerRef = useRef({ currentUserId, viewerIsClient, viewerKey });
  messagesRef.current = messages;
  knownIds.current = new Set(messages.map((msg) => msg.id));
  viewerRef.current = { currentUserId, viewerIsClient, viewerKey };

  const applyPerson = useCallback((token: number, id: string, seq: number, src: string | undefined, name: string | undefined) => {
    if (alive.current !== token || avatarSeq.current.get(id) !== seq) return;
    const next = personFace(src, name);
    setPeople((prev) => (sameFace(prev[id], next) ? prev : { ...prev, [id]: next }));
  }, []);

  const trackManagerCard = useCallback((id: string, pending: Promise<ManagerPublicCard>) => {
    cards.current.set(id, pending);
    const token = alive.current;
    const seq = (avatarSeq.current.get(id) ?? 0) + 1;
    avatarSeq.current.set(id, seq);
    pending.then(
      (card) => applyPerson(token, id, seq, card.avatarUrl, card.displayName),
      () => {
        // Отклонённый промис из кэша убирается, чтобы следующее открытие повторило запрос.
        if (cards.current.get(id) === pending) cards.current.delete(id);
        applyPerson(token, id, seq, undefined, undefined);
      },
    );
    return pending;
  }, [applyPerson]);

  const loadManagerCard = useCallback((id: string) => {
    const cached = cards.current.get(id);
    if (cached) return cached;
    return trackManagerCard(id, getManagerCard(id));
  }, [trackManagerCard]);

  const readClientBubble = useCallback((id: string) => {
    const token = alive.current;
    const seq = (avatarSeq.current.get(id) ?? 0) + 1;
    avatarSeq.current.set(id, seq);
    getClientCard(id).then(
      (card) => applyPerson(token, id, seq, card.avatarUrl, card.displayName),
      () => applyPerson(token, id, seq, undefined, undefined),
    );
  }, [applyPerson]);

  const readPeople = useCallback((list: ChatMessage[], refresh: boolean) => {
    const { currentUserId: userId, viewerIsClient: clientViewer } = viewerRef.current;
    const seen = new Set<string>();
    for (const msg of list) {
      if (seen.has(msg.senderId)) continue;
      const kind = bubbleRead(msg, userId, clientViewer);
      if (kind === 'manager') {
        seen.add(msg.senderId);
        if (refresh) trackManagerCard(msg.senderId, getManagerCard(msg.senderId));
        else loadManagerCard(msg.senderId);
      } else if (kind === 'client') {
        seen.add(msg.senderId);
        readClientBubble(msg.senderId);
      }
    }
  }, [loadManagerCard, readClientBubble, trackManagerCard]);

  const scrollToBottom = useCallback((behavior: ScrollBehavior = 'smooth') => {
    requestAnimationFrame(() => {
      const node = scrollRef.current;
      if (!node) return;
      if (typeof node.scrollTo === 'function') {
        node.scrollTo({ top: node.scrollHeight, behavior });
        return;
      }
      node.scrollTop = node.scrollHeight;
    });
  }, []);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    getMessages(bookingId)
      .then((data) => {
        if (cancelled) return;
        // Ответы, стартовавшие до истории, держат прежний token и после обнуления seq снова совпали бы с новым чтением.
        alive.current += 1;
        cards.current = new Map();
        avatarSeq.current = new Map();
        setPeople({});
        lastViewerKey.current = viewerRef.current.viewerKey;
        readPeople(data, false);
        setMessages(data);
        setIsLoading(false);
        scrollToBottom('instant');
      })
      .catch(() => {
        if (!cancelled) {
          setIsLoading(false);
          toast.error('Не удалось загрузить сообщения');
        }
      });
    return () => {
      cancelled = true;
      alive.current += 1;
      lastViewerKey.current = null;
    };
  }, [bookingId, readPeople, scrollToBottom]);

  useEffect(() => {
    if (isLoading) return;
    if (lastViewerKey.current === null || lastViewerKey.current === viewerKey) return;
    lastViewerKey.current = viewerKey;
    readPeople(messagesRef.current, false);
  }, [isLoading, readPeople, viewerKey]);

  useEffect(() => {
    const unsubscribe = subscribeToMessages(bookingId, (msg) => {
      const seen = knownIds.current.has(msg.id);
      setMessages((prev) => {
        const replaceIdx = prev.findIndex(
          (m) =>
            m.id.startsWith('temp-') &&
            m.text === msg.text &&
            m.senderId === msg.senderId,
        );
        if (replaceIdx >= 0) {
          const next = [...prev];
          next[replaceIdx] = msg;
          return next.sort(
            (a, b) =>
              new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
          );
        }
        if (prev.some((m) => m.id === msg.id)) return prev;
        return [...prev, msg].sort(
          (a, b) =>
            new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
        );
      });
      if (!seen) readPeople([msg], true);
      scrollToBottom();
    });
    return () => {
      unsubscribe();
      disconnectChat(bookingId);
    };
  }, [bookingId, readPeople, scrollToBottom]);

  const handleSend = async () => {
    const text = inputText.trim();
    if (!text || isSending) return;

    const optimisticMsg: ChatMessage = {
      id: `temp-${Date.now()}`,
      bookingId,
      senderId: currentUserId,
      senderName: currentUserName,
      senderRole: currentUserRole,
      text,
      createdAt: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, optimisticMsg]);
    setInputText('');
    resetTextareaHeight();
    scrollToBottom();

    setIsSending(true);
    try {
      const saved = await sendMessage(bookingId, text, currentUserId);
      setMessages((prev) =>
        prev.map((m) => (m.id === optimisticMsg.id ? saved : m)),
      );
    } catch {
      setMessages((prev) => prev.filter((m) => m.id !== optimisticMsg.id));
      toast.error('Не удалось отправить сообщение');
    } finally {
      setIsSending(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setInputText(e.target.value);
    autoResizeTextarea(e.target);
  };

  const autoResizeTextarea = (el: HTMLTextAreaElement) => {
    el.style.height = 'auto';
    el.style.height = `${Math.min(el.scrollHeight, 120)}px`;
  };

  const resetTextareaHeight = () => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
    }
  };

  const dateGroups = groupMessagesByDate(messages);
  const ownName = currentUserName.trim();

  if (isLoading) {
    return (
      <div className="flex h-full min-h-0 flex-1 items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-3 border-sand border-t-primary" />
      </div>
    );
  }

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <div
        ref={scrollRef}
        data-testid="chat-message-list"
        className="min-h-0 flex-1 space-y-4 overflow-y-auto px-4 py-4"
      >
        {messages.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-16 text-warm-gray">
            <MessageSquare size={48} className="mb-3 opacity-40" />
            <p className="text-base font-medium">
              Начните общение с менеджером
            </p>
            <p className="mt-1 text-sm">
              Напишите сообщение, чтобы обсудить детали поездки
            </p>
          </div>
        ) : (
          <AnimatePresence initial={false}>
            {dateGroups.map((group) => (
              <div key={group.date} className="space-y-3">
                <div className="flex justify-center">
                  <span className="rounded-full bg-sand px-3 py-1 text-xs font-medium text-warm-gray">
                    {formatDateSeparator(group.messages[0].createdAt)}
                  </span>
                </div>

                {group.messages.map((msg) => {
                  const isOwn = msg.senderId === currentUserId;
                  const staffViewer = currentUserRole === 'manager' || currentUserRole === 'admin';
                  const openClientCard = staffViewer && msg.senderRole === 'client' && !isOwn;
                  const openManagerCard =
                    (msg.senderRole === 'manager' || msg.senderRole === 'admin') && !isOwn;
                  const kind = bubbleRead(msg, currentUserId, viewerIsClient);
                  const face = people[msg.senderId];
                  const signature = msg.senderRole === 'system' || kind === 'none'
                    ? undefined
                    : kind === 'session'
                      ? ownName || undefined
                      : face?.name;
                  const avatarName = msg.senderRole === 'system' ? msg.senderName : (signature ?? '');
                  const src = kind === 'session'
                    ? photoSrc(currentUserAvatar)
                    : kind === 'manager' || kind === 'client'
                      ? face?.src
                      : undefined;
                  const avatar = <Avatar name={avatarName} size="sm" src={src} />;
                  const openCard = openClientCard ? 'client' as const : openManagerCard ? 'manager' as const : null;
                  const openThisCard = () => {
                    if (openClientCard) setCardClientId(msg.senderId);
                    else setCardUserId(msg.senderId);
                  };
                  return (
                    <motion.div
                      key={msg.id}
                      initial={{ opacity: 0, y: 8 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ duration: 0.2 }}
                      className={clsx(
                        'flex gap-2.5',
                        isOwn ? 'flex-row-reverse' : 'flex-row',
                      )}
                    >
                      <div className="mt-1 shrink-0">
                        {openCard ? (
                          <button
                            type="button"
                            className="cursor-pointer rounded-full"
                            aria-label={cardButtonName(openCard, signature)}
                            onClick={openThisCard}
                          >
                            {avatar}
                          </button>
                        ) : (
                          avatar
                        )}
                      </div>

                      <div
                        className={clsx(
                          'max-w-[75%] space-y-1',
                          isOwn ? 'items-end' : 'items-start',
                        )}
                      >
                        {signature ? (
                          openCard ? (
                            <button
                              type="button"
                              className={clsx(
                                'block w-full cursor-pointer text-xs font-medium',
                                isOwn ? 'text-right text-primary' : 'text-left text-warm-gray',
                              )}
                              aria-label={cardButtonName(openCard, signature)}
                              onClick={openThisCard}
                            >
                              {signature}
                            </button>
                          ) : (
                            <p
                              className={clsx(
                                'text-xs font-medium',
                                isOwn ? 'text-right text-primary' : 'text-warm-gray',
                              )}
                            >
                              {signature}
                            </p>
                          )
                        ) : null}

                        <div
                          className={clsx(
                            'rounded-2xl px-4 py-2.5 text-sm leading-relaxed',
                            isOwn
                              ? 'rounded-tr-sm bg-primary text-white'
                              : 'rounded-tl-sm bg-sand text-dark',
                          )}
                        >
                          {msg.text}
                        </div>

                        <p
                          className={clsx(
                            'text-[11px] text-warm-gray',
                            isOwn ? 'text-right' : 'text-left',
                          )}
                        >
                          {formatMessageTime(msg.createdAt)}
                        </p>
                      </div>
                    </motion.div>
                  );
                })}
              </div>
            ))}
          </AnimatePresence>
        )}
      </div>

      <div className="shrink-0 border-t border-sand bg-white px-4 py-3">
        <div className="flex items-end gap-2">
          <button
            type="button"
            disabled
            className="mb-1 flex shrink-0 cursor-not-allowed items-center gap-1.5 rounded-lg px-2 py-2 text-warm-gray"
          >
            <Paperclip size={20} className="shrink-0 opacity-60" aria-hidden />
            <span className="whitespace-nowrap text-xs leading-none">Появится позже</span>
          </button>

          <textarea
            ref={textareaRef}
            value={inputText}
            onChange={handleInputChange}
            onKeyDown={handleKeyDown}
            placeholder="Введите сообщение..."
            rows={1}
            className="max-h-[120px] min-h-[40px] flex-1 resize-none rounded-xl border border-sand bg-cream px-4 py-2.5 text-sm text-dark outline-none transition-colors placeholder:text-warm-gray focus:border-primary focus:ring-1 focus:ring-primary/20"
          />

          <button
            type="button"
            onClick={handleSend}
            disabled={!inputText.trim() || isSending}
            className={clsx(
              'mb-1 shrink-0 rounded-xl p-2.5 transition-all',
              inputText.trim() && !isSending
                ? 'bg-primary text-white shadow-button hover:bg-primary-light'
                : 'bg-sand text-warm-gray cursor-not-allowed',
            )}
          >
            <Send size={18} />
          </button>
        </div>
      </div>
      {cardUserId && (
        <ManagerShortCard
          userId={cardUserId}
          onClose={() => setCardUserId(null)}
          loadCard={loadManagerCard}
        />
      )}
      {cardClientId && (
        <ClientShortCard
          clientId={cardClientId}
          onClose={() => setCardClientId(null)}
          loadCard={getClientCard}
        />
      )}
    </div>
  );
}
