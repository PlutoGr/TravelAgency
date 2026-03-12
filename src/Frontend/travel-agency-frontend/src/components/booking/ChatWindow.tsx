import { useState, useEffect, useRef, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Send, Paperclip, MessageSquare } from 'lucide-react';
import { format, isToday, parseISO } from 'date-fns';
import { ru } from 'date-fns/locale';
import clsx from 'clsx';
import type { ChatMessage } from '@/types';
import { Avatar } from '@/components/ui';
import { getMessages, sendMessage } from '@/api/chat';

interface ChatWindowProps {
  bookingId: string;
  currentUserId: string;
  currentUserName: string;
  currentUserRole: 'client' | 'manager';
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
}: ChatWindowProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputText, setInputText] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSending, setIsSending] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const scrollToBottom = useCallback((behavior: ScrollBehavior = 'smooth') => {
    requestAnimationFrame(() => {
      scrollRef.current?.scrollTo({
        top: scrollRef.current.scrollHeight,
        behavior,
      });
    });
  }, []);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    getMessages(bookingId)
      .then((data) => {
        if (!cancelled) {
          setMessages(data);
          setIsLoading(false);
          scrollToBottom('instant');
        }
      })
      .catch(() => {
        if (!cancelled) setIsLoading(false);
      });
    return () => { cancelled = true; };
  }, [bookingId, scrollToBottom]);

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
      const saved = await sendMessage(bookingId, text);
      setMessages((prev) =>
        prev.map((m) => (m.id === optimisticMsg.id ? saved : m)),
      );
    } catch {
      setMessages((prev) => prev.filter((m) => m.id !== optimisticMsg.id));
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

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center py-16">
        <div className="h-8 w-8 animate-spin rounded-full border-3 border-sand border-t-primary" />
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col">
      <div
        ref={scrollRef}
        className="flex-1 overflow-y-auto px-4 py-4 space-y-4"
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
                        <Avatar name={msg.senderName} size="sm" />
                      </div>

                      <div
                        className={clsx(
                          'max-w-[75%] space-y-1',
                          isOwn ? 'items-end' : 'items-start',
                        )}
                      >
                        <p
                          className={clsx(
                            'text-xs font-medium',
                            isOwn ? 'text-right text-primary' : 'text-warm-gray',
                          )}
                        >
                          {msg.senderName}
                        </p>

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

      <div className="border-t border-sand bg-white px-4 py-3">
        <div className="flex items-end gap-2">
          <button
            type="button"
            className="mb-1 shrink-0 rounded-lg p-2 text-warm-gray transition-colors hover:bg-sand hover:text-primary"
            title="Прикрепить файл"
          >
            <Paperclip size={20} />
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
    </div>
  );
}
