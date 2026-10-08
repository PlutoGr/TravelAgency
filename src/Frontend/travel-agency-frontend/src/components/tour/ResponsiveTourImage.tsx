import clsx from 'clsx';
import { resolveTourMedia, type MediaVariant } from '@/utils/media';

interface ResponsiveTourImageProps {
  mediaFileId?: string | null;
  fallbackUrl?: string | null;
  alt: string;
  variant: MediaVariant;
  className?: string;
  priority?: boolean;
  onLoad?: () => void;
}

export default function ResponsiveTourImage({
  mediaFileId,
  fallbackUrl,
  alt,
  variant,
  className,
  priority = false,
  onLoad,
}: ResponsiveTourImageProps) {
  const media = resolveTourMedia({ mediaFileId, fallbackUrl }, variant);
  if (!media) {
    return <div className={clsx('bg-sand', className)} aria-hidden />;
  }

  return (
    <img
      src={media.src}
      srcSet={media.srcSet}
      sizes={media.sizes}
      alt={alt}
      width={media.width}
      height={media.height}
      loading={priority ? 'eager' : 'lazy'}
      fetchPriority={priority ? 'high' : 'auto'}
      decoding="async"
      onLoad={onLoad}
      className={className}
    />
  );
}
