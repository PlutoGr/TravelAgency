import { useState, type FormEvent, type ReactNode } from 'react';
import { Pencil } from 'lucide-react';
import { Button } from '@/components/ui';
import {
  ABOUT_MAX,
  POSITION_MAX,
  managerCardPayload,
  normalizeTag,
  sameManagerCard,
  tagProblem,
  tenureLabel,
  type ManagerCardDraft,
  type ManagerCardFieldErrors,
  type ManagerCardWrite,
} from '@/domain/managerCardForm';
import type { ManagerPublicCard } from '@/api/managerCard';
import ManagerCardFace from './ManagerCardFace';
import YearField from './YearField';

type ManagerCardWorkspaceProps = {
  personName: string;
  avatarUrl?: string;
  card: ManagerPublicCard | null;
  phase: 'idle' | 'loading' | 'error' | 'ready';
  refreshError?: string;
  locked: boolean;
  onRetry: () => void;
  onSave: (payload: ManagerCardWrite) => Promise<void>;
};

const fieldClass =
  'min-h-11 w-full rounded-[12px] border border-sand bg-white px-4 text-sm text-[#1c1915] outline-none focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-primary/10';

function draftFrom(card: ManagerPublicCard | null): ManagerCardDraft {
  return {
    position: card?.position ?? '',
    tourismSince: card?.tourismSince ?? null,
    about: card?.about ?? '',
    tags: [...(card?.tags ?? [])],
  };
}

function Check({ done }: { done: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={
        done
          ? 'inline-flex h-5 w-5 items-center justify-center rounded-full bg-[#6e7f62] text-xs text-white'
          : 'inline-flex h-5 w-5 items-center justify-center rounded-full border border-[#30281c]/20 text-xs text-[#7a7268]'
      }
    >
      {done ? '✓' : ''}
    </span>
  );
}

export default function ManagerCardWorkspace({
  personName,
  avatarUrl,
  card,
  phase,
  refreshError,
  locked,
  onRetry,
  onSave,
}: ManagerCardWorkspaceProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState<ManagerCardDraft>(() => draftFrom(card));
  const [tagDraft, setTagDraft] = useState('');
  const [errors, setErrors] = useState<ManagerCardFieldErrors>({});
  const [formError, setFormError] = useState<string | undefined>();
  const [saving, setSaving] = useState(false);

  const saved = managerCardPayload(draftFrom(card)).payload;
  const current = managerCardPayload(draft).payload;
  const unchanged = Boolean(saved && current && sameManagerCard(saved, current));
  const filled = [
    Boolean(avatarUrl),
    Boolean(card?.position?.trim()),
    card?.tourismSince != null,
    Boolean(card?.about?.trim()),
    (card?.tags?.length ?? 0) > 0,
  ];
  const doneCount = filled.filter(Boolean).length;
  const previewName = personName;
  const previewAvatar = avatarUrl;

  function openEditor() {
    if (locked || saving || !card) return;
    setDraft(draftFrom(card));
    setTagDraft('');
    setErrors({});
    setFormError(undefined);
    setEditing(true);
  }

  function closeEditor() {
    if (saving) return;
    setEditing(false);
    setErrors({});
    setFormError(undefined);
  }

  function addTag() {
    const problem = tagProblem(tagDraft, draft.tags);
    if (problem) {
      setErrors((prev) => ({ ...prev, tags: problem }));
      return;
    }
    const tag = normalizeTag(tagDraft);
    setDraft((prev) => ({ ...prev, tags: [...prev.tags, tag] }));
    setTagDraft('');
    setErrors((prev) => ({ ...prev, tags: undefined }));
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    if (saving || unchanged) return;
    const { payload, errors: next } = managerCardPayload(draft);
    setErrors(next);
    setFormError(undefined);
    if (!payload) return;
    setSaving(true);
    try {
      await onSave(payload);
      setEditing(false);
    } catch (err) {
      const fields = err as ManagerCardFieldErrors & { form?: string };
      const { form, ...rest } = fields;
      if (rest.position || rest.tourismSince || rest.about || rest.tags) setErrors(rest);
      else setFormError(form);
    } finally {
      setSaving(false);
    }
  }

  const face = (
    <ManagerCardFace
      displayName={previewName}
      avatarUrl={previewAvatar}
      position={editing ? draft.position : card?.position}
      tourismSince={editing ? draft.tourismSince : card?.tourismSince}
      about={editing ? draft.about : card?.about}
      tags={editing ? draft.tags : card?.tags}
      footer={
        <section className="mt-6">
          <h3 className="text-xs tracking-[0.14em] text-[#7a7268] uppercase">Фото из поездок</h3>
          <p className="mt-3 rounded-[16px] border border-dashed border-[#30281c]/20 bg-[#f3eee6] px-4 py-8 text-center text-sm text-[#5c564e]">
            Пока нет снимков.
          </p>
        </section>
      }
    />
  );

  let editor: ReactNode = null;
  if (editing) {
    editor = (
      <form className="rounded-[28px] border border-[#30281c]/10 bg-[#fffcf8] p-6 shadow-[0_24px_60px_#30241214]" onSubmit={save}>
        <p className="text-xs tracking-[0.14em] text-[#7a7268] uppercase">Кто вы</p>
        <p className="mt-3 font-heading text-lg font-semibold text-[#1c1915]">{previewName}</p>
        <p className="mt-1 text-sm text-[#5c564e]">Фото и имя меняются во вкладке «Аккаунт».</p>

        <div className="mt-6">
          <div className="mb-1.5 flex items-baseline justify-between gap-3">
            <label htmlFor="card-position" className="text-sm font-medium text-[#1c1915]">
              Должность
            </label>
            <span className="text-xs text-[#7a7268]">
              {draft.position.trim().length} / {POSITION_MAX}
            </span>
          </div>
          <input
            id="card-position"
            value={draft.position}
            aria-invalid={Boolean(errors.position) || undefined}
            onChange={(event) => {
              setDraft((prev) => ({ ...prev, position: event.target.value }));
              setErrors((prev) => ({ ...prev, position: undefined }));
            }}
            className={fieldClass}
          />
          {errors.position ? <p className="mt-1 text-sm text-red-500">{errors.position}</p> : null}
        </div>

        <div className="mt-5">
          <label htmlFor="card-year" className="mb-1.5 block text-sm font-medium text-[#1c1915]">
            Работаю в туризме с
          </label>
          <YearField
            id="card-year"
            value={draft.tourismSince}
            invalid={Boolean(errors.tourismSince)}
            describedBy="card-year-hint"
            onChange={(year) => {
              setDraft((prev) => ({ ...prev, tourismSince: year }));
              setErrors((prev) => ({ ...prev, tourismSince: undefined }));
            }}
          />
          <p id="card-year-hint" className="mt-2 text-sm text-[#5c564e]">
            {draft.tourismSince != null
              ? `В визитке будет: «${tenureLabel(draft.tourismSince)}». Стаж пересчитывается сам каждый год.`
              : 'Укажите год, стаж посчитается сам.'}
          </p>
          {errors.tourismSince ? <p className="mt-1 text-sm text-red-500">{errors.tourismSince}</p> : null}
        </div>

        <div className="mt-5">
          <div className="mb-1.5 flex items-baseline justify-between gap-3">
            <label htmlFor="card-about" className="text-sm font-medium text-[#1c1915]">
              Расскажите клиенту о себе
            </label>
            <span className="text-xs text-[#7a7268]">
              {draft.about.trim().length} / {ABOUT_MAX}
            </span>
          </div>
          <textarea
            id="card-about"
            value={draft.about}
            rows={8}
            aria-invalid={Boolean(errors.about) || undefined}
            onChange={(event) => {
              setDraft((prev) => ({ ...prev, about: event.target.value }));
              setErrors((prev) => ({ ...prev, about: undefined }));
            }}
            className="w-full rounded-[12px] border border-sand bg-white px-4 py-3 text-sm text-[#1c1915] outline-none focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-primary/10"
          />
          {errors.about ? <p className="mt-1 text-sm text-red-500">{errors.about}</p> : null}
        </div>

        <div className="mt-5">
          <p className="text-xs tracking-[0.14em] text-[#7a7268] uppercase">В чём разбираюсь</p>
          <label htmlFor="card-tag" className="mt-3 mb-1.5 block text-sm font-medium text-[#1c1915]">
            Страна, курорт или вид отдыха
          </label>
          <div className="flex gap-2">
            <input
              id="card-tag"
              value={tagDraft}
              onChange={(event) => {
                setTagDraft(event.target.value);
                setErrors((prev) => ({ ...prev, tags: undefined }));
              }}
              onKeyDown={(event) => {
                if (event.key !== 'Enter') return;
                event.preventDefault();
                addTag();
              }}
              className={fieldClass}
            />
            <Button type="button" variant="ink" pill onClick={addTag}>
              Добавить
            </Button>
          </div>
          <p className="mt-2 text-sm text-[#5c564e]">Больше 10 слов не поместится</p>
          {errors.tags ? <p className="mt-1 text-sm text-red-500">{errors.tags}</p> : null}
          {draft.tags.length > 0 ? (
            <ul className="mt-3 flex flex-wrap gap-2">
              {draft.tags.map((tag) => (
                <li key={tag}>
                  <button
                    type="button"
                    className="rounded-full border border-[#30281c]/15 bg-white px-3 py-1.5 text-sm"
                    onClick={() => setDraft((prev) => ({ ...prev, tags: prev.tags.filter((item) => item !== tag) }))}
                  >
                    {tag}
                    <span className="sr-only">, убрать</span>
                  </button>
                </li>
              ))}
            </ul>
          ) : null}
        </div>

        <div className="mt-6 flex flex-wrap items-center gap-3">
          <Button type="button" variant="ghost" onClick={closeEditor} disabled={saving}>
            Отмена
          </Button>
          <Button type="submit" variant="ink" pill isLoading={saving} disabled={unchanged}>
            Сохранить
          </Button>
          {unchanged ? <p className="text-sm text-[#5c564e]">Изменений нет</p> : null}
        </div>
        {formError ? (
          <p role="alert" className="mt-3 text-sm text-red-500">
            {formError}
          </p>
        ) : null}
      </form>
    );
  }

  const checklist = (
    <section aria-label="Ваша визитка" className="rounded-[28px] border border-[#30281c]/10 bg-[#fffcf8] p-6 shadow-[0_24px_60px_#30241214]">
      <h2 className="text-3xl font-medium text-[#1c1915]" style={{ fontFamily: 'Georgia, "Times New Roman", serif' }}>
        Ваша визитка
      </h2>
      <p className="mt-2 text-sm text-[#5c564e]">
        Клиент открывает её из своей заявки, когда хочет узнать, кто им занимается.
      </p>
      <Button
        type="button"
        variant="ink"
        pill
        fullWidth
        className="mt-5"
        leftIcon={<Pencil size={16} aria-hidden="true" />}
        onClick={openEditor}
        disabled={locked || phase !== 'ready' || !card}
      >
        Редактировать визитку
      </Button>
      <div className="mt-6 flex items-baseline justify-between text-xs tracking-[0.14em] text-[#7a7268] uppercase">
        <span>Заполнено</span>
        <span>
          {doneCount} из {filled.length}
        </span>
      </div>
      <ul className="mt-3 space-y-2 text-sm text-[#1c1915]">
        {(
          [
            ['Фото', filled[0]],
            ['Должность', filled[1]],
            ['Стаж', filled[2]],
            ['О себе', filled[3]],
            ['В чём разбираюсь', filled[4]],
          ] as const
        ).map(([label, ok]) => (
          <li key={label} className="flex items-center gap-2">
            <Check done={ok} />
            {label}
          </li>
        ))}
        <li className="flex items-center gap-2 text-[#5c564e]">
          <Check done={false} />
          Фото из поездок
          <span className="text-xs">пока пусто</span>
        </li>
      </ul>
    </section>
  );

  return (
    <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1.15fr)_minmax(260px,0.85fr)]">
      <div className={editing ? 'order-2' : 'order-2 lg:order-1'}>
        {(phase === 'loading' || phase === 'idle') && !card ? (
          <div
            role="status"
            aria-label="Загрузка"
            className="mx-auto h-8 w-8 animate-spin rounded-full border-3 border-sand border-t-[#6e7f62]"
          />
        ) : null}
        {phase === 'error' && !card ? (
          <div className="rounded-[28px] border border-[#30281c]/10 bg-[#fffcf8] p-6">
            <p role="alert">Не удалось открыть карточку.</p>
            <Button type="button" variant="ink" pill className="mt-4" onClick={onRetry}>
              Повторить
            </Button>
          </div>
        ) : null}
        {card || editing ? (
          <>
            <p className="mb-3 flex items-center gap-2 text-sm text-[#5c564e]">
              <span aria-hidden="true" className="h-2 w-2 rounded-full bg-[#6e7f62]" />
              Так вашу визитку видит клиент
            </p>
            {refreshError ? (
              <p role="alert" className="mb-3 text-sm text-red-500">
                {refreshError}
              </p>
            ) : null}
            {face}
          </>
        ) : null}
      </div>
      <div className={editing ? 'order-1' : 'order-1 lg:order-2'}>{editing ? editor : checklist}</div>
    </div>
  );
}
