import type { ReactNode } from 'react';
import { Avatar } from '@/components/ui';
import { tenureLabel } from '@/domain/managerCardForm';

export const CARD_HEADER_SRC = '/profile/card-header-fireflies.jpg';

const serif = { fontFamily: 'Georgia, "Times New Roman", serif' };

type ManagerCardFaceProps = {
  displayName: string;
  avatarUrl?: string;
  position?: string | null;
  tourismSince?: number | null;
  about?: string | null;
  tags?: string[];
  footer?: ReactNode;
};

function filled(value: string | null | undefined): string | null {
  if (typeof value !== 'string') return null;
  return value.trim() ? value : null;
}

export default function ManagerCardFace({
  displayName,
  avatarUrl,
  position,
  tourismSince,
  about,
  tags,
  footer,
}: ManagerCardFaceProps) {
  const role = filled(position);
  const story = filled(about);
  const tenure = tenureLabel(tourismSince);
  const chips = (tags ?? []).filter((tag) => tag.trim());

  return (
    <article className="overflow-hidden rounded-[28px] border border-[#30281c]/10 bg-[#fffcf8] text-[#1c1915] shadow-[0_24px_60px_#30241214]">
      <div className="relative h-36 bg-[#14110e]">
        <img src={CARD_HEADER_SRC} alt="" className="h-full w-full object-cover" />
      </div>
      <div className="px-6 pb-6">
        <div className="-mt-10 mb-3 w-fit rounded-full ring-4 ring-[#fffcf8]">
          <Avatar src={avatarUrl} name={displayName} size="xl" />
        </div>
        <h2 data-testid="manager-card-name" className="text-[2rem] leading-tight font-medium" style={serif}>
          {displayName}
        </h2>
        {role ? <p className="mt-1 text-sm text-[#5c564e]">{role}</p> : null}
        {tenure ? (
          <p className="mt-4 inline-flex rounded-full bg-[#f3eee6] px-3 py-1 text-sm text-[#3a342c]">{tenure}</p>
        ) : null}
        {chips.length > 0 ? (
          <section className="mt-6">
            <h3 className="text-xs tracking-[0.14em] text-[#7a7268] uppercase">В чём разбираюсь</h3>
            <ul className="mt-3 flex flex-wrap gap-2">
              {chips.map((tag) => (
                <li key={tag} className="rounded-full border border-[#30281c]/15 bg-white px-3 py-1.5 text-sm">
                  {tag}
                </li>
              ))}
            </ul>
          </section>
        ) : null}
        {story ? (
          <section className="mt-6">
            <h3 className="text-xs tracking-[0.14em] text-[#7a7268] uppercase">О себе</h3>
            <p className="mt-3 text-[15px] leading-relaxed whitespace-pre-wrap text-[#3a342c]">{story}</p>
          </section>
        ) : null}
        {footer}
      </div>
    </article>
  );
}
