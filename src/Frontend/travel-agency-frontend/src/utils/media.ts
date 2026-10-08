export const MEDIA_SIZES = ['w200', 'w800', 'w1600'] as const;
export type MediaSize = (typeof MEDIA_SIZES)[number];
export type MediaVariant = 'card' | 'gallery' | 'thumb';

const WIDTH: Record<MediaSize, number> = {
  w200: 200,
  w800: 800,
  w1600: 1600,
};

const VARIANT_SIZE: Record<MediaVariant, MediaSize> = {
  card: 'w800',
  gallery: 'w1600',
  thumb: 'w200',
};

const VARIANT_MAX: Record<MediaVariant, MediaSize> = {
  card: 'w1600',
  gallery: 'w1600',
  thumb: 'w800',
};

export const MEDIA_SIZES_ATTR: Record<MediaVariant, string> = {
  card: '(min-width: 1024px) 300px, (min-width: 640px) 45vw, 92vw',
  gallery: '(min-width: 1024px) 720px, 92vw',
  thumb: '96px',
};

const MEDIA_FILE =
  /\/media\/files\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\//;

export function mediaFileUrl(mediaFileId: string, size: MediaSize): string {
  return `/api/v1/media/files/${mediaFileId}/${size}`;
}

export function mediaSrcSet(mediaFileId: string, max: MediaSize = 'w1600'): string {
  const limit = WIDTH[max];
  return MEDIA_SIZES.filter((size) => WIDTH[size] <= limit)
    .map((size) => `${mediaFileUrl(mediaFileId, size)} ${WIDTH[size]}w`)
    .join(', ');
}

export function mediaIdFromUrl(url: string | null | undefined): string | null {
  if (!url || url.toLowerCase().includes('minio')) return null;
  return url.match(MEDIA_FILE)?.[1] ?? null;
}

export function isSafePhotoUrl(url: string | null | undefined): url is string {
  if (!url) return false;
  return !url.toLowerCase().includes('minio');
}

export type ResolvedMedia = {
  src: string;
  srcSet?: string;
  sizes?: string;
  width: number;
  height: number;
};

export function resolveTourMedia(
  source: { mediaFileId?: string | null; fallbackUrl?: string | null },
  variant: MediaVariant,
): ResolvedMedia | null {
  const size = VARIANT_SIZE[variant];
  const mediaFileId = source.mediaFileId || mediaIdFromUrl(source.fallbackUrl);
  if (mediaFileId) {
    const width = WIDTH[size];
    return {
      src: mediaFileUrl(mediaFileId, size),
      srcSet: mediaSrcSet(mediaFileId, VARIANT_MAX[variant]),
      sizes: MEDIA_SIZES_ATTR[variant],
      width,
      height: Math.round(width * (variant === 'gallery' ? 10 / 16 : 3 / 4)),
    };
  }

  if (!isSafePhotoUrl(source.fallbackUrl)) return null;
  return {
    src: source.fallbackUrl,
    width: WIDTH[size],
    height: Math.round(WIDTH[size] * (variant === 'gallery' ? 10 / 16 : 3 / 4)),
  };
}
