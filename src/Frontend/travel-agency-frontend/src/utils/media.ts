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

/** Публичный адрес картинки. Клиент собирает его из id файла, сервер адрес не отдаёт. */
export function mediaImageUrl(mediaFileId: string, size: MediaSize): string {
  return `/api/v1/media/files/${mediaFileId}/${size}`;
}

/** Превью черновика. Публичный /media/files/{id} до публикации отвечает 404. */
export function manageMediaFileUrl(mediaFileId: string, size: MediaSize): string {
  return `/api/v1/media/manage/files/${mediaFileId}/${size}`;
}

export function mediaSrcSet(mediaFileId: string, max: MediaSize = 'w1600'): string {
  const limit = WIDTH[max];
  return MEDIA_SIZES.filter((size) => WIDTH[size] <= limit)
    .map((size) => `${mediaImageUrl(mediaFileId, size)} ${WIDTH[size]}w`)
    .join(', ');
}

export type ResolvedMedia = {
  src: string;
  srcSet?: string;
  sizes?: string;
  width: number;
  height: number;
};

export function resolveTourMedia(
  mediaFileId: string | null | undefined,
  variant: MediaVariant,
): ResolvedMedia | null {
  if (!mediaFileId) return null;
  const size = VARIANT_SIZE[variant];
  const width = WIDTH[size];
  return {
    src: mediaImageUrl(mediaFileId, size),
    srcSet: mediaSrcSet(mediaFileId, VARIANT_MAX[variant]),
    sizes: MEDIA_SIZES_ATTR[variant],
    width,
    height: Math.round(width * (variant === 'gallery' ? 10 / 16 : 3 / 4)),
  };
}

export function coverMediaFileId(tour: {
  coverMediaFileId?: string | null;
  images?: { mediaFileId: string; isCover: boolean }[] | null;
}): string | null {
  return tour.coverMediaFileId
    || tour.images?.find((image) => image.isCover)?.mediaFileId
    || tour.images?.[0]?.mediaFileId
    || null;
}
