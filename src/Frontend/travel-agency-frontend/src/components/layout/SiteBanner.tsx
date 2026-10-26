interface SiteBannerProps {
  text: string | null | undefined;
  enabled: boolean;
}

export default function SiteBanner({ text, enabled }: SiteBannerProps) {
  if (!enabled || !text || text.trim().length === 0) return null;

  return (
    <div role="status" aria-label="Объявление" className="bg-terracotta text-white">
      <p className="mx-auto max-w-7xl px-4 py-2 text-center text-sm">{text}</p>
    </div>
  );
}
