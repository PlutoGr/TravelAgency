import { useState, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronLeft, ChevronRight, X, ZoomIn } from 'lucide-react';
import clsx from 'clsx';
import type { TourImage } from '@/types';
import ResponsiveTourImage from './ResponsiveTourImage';

interface TourGalleryProps {
  images?: TourImage[];
  photos?: string[];
  title: string;
}

type Slide = {
  key: string;
  mediaFileId?: string;
  fallbackUrl?: string;
  alt: string;
};

function slidesFrom(images: TourImage[] | undefined, photos: string[] | undefined, title: string): Slide[] {
  const fromImages = [...(images ?? [])].sort(
    (a, b) => Number(b.isCover) - Number(a.isCover) || a.sortOrder - b.sortOrder,
  );
  if (fromImages.length > 0) {
    return fromImages.map((image) => ({
      key: image.mediaFileId,
      mediaFileId: image.mediaFileId,
      alt: image.alt || title,
    }));
  }

  return (photos ?? [])
    .filter((photo) => photo && !photo.toLowerCase().includes('minio'))
    .map((photo, index) => ({
      key: `${photo}-${index}`,
      fallbackUrl: photo,
      alt: `${title}, фото ${index + 1}`,
    }));
}

function NavButton({
  direction,
  onClick,
}: {
  direction: 'prev' | 'next';
  onClick: () => void;
}) {
  const Icon = direction === 'prev' ? ChevronLeft : ChevronRight;
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={direction === 'prev' ? 'Предыдущее фото' : 'Следующее фото'}
      className="flex h-11 w-11 items-center justify-center rounded-full bg-white/80 text-dark shadow-md backdrop-blur-sm transition-colors hover:bg-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
    >
      <Icon size={20} />
    </button>
  );
}

export default function TourGallery({ images, photos, title }: TourGalleryProps) {
  const slides = slidesFrom(images, photos, title);
  const [activeIndex, setActiveIndex] = useState(0);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [direction, setDirection] = useState(0);
  const safeIndex = slides.length === 0 ? 0 : activeIndex % slides.length;
  const active = slides[safeIndex];

  const navigate = useCallback(
    (newIndex: number) => {
      setDirection(newIndex > safeIndex ? 1 : -1);
      setActiveIndex(newIndex);
    },
    [safeIndex],
  );

  const goPrev = useCallback(() => {
    if (slides.length < 2) return;
    navigate(safeIndex === 0 ? slides.length - 1 : safeIndex - 1);
  }, [safeIndex, slides.length, navigate]);

  const goNext = useCallback(() => {
    if (slides.length < 2) return;
    navigate(safeIndex === slides.length - 1 ? 0 : safeIndex + 1);
  }, [safeIndex, slides.length, navigate]);

  if (!active) {
    return (
      <div className="flex aspect-[16/10] items-center justify-center rounded-[16px] bg-sand text-sm text-warm-gray">
        Фото пока нет
      </div>
    );
  }

  const slideVariants = {
    enter: (dir: number) => ({ x: dir > 0 ? 80 : -80, opacity: 0 }),
    center: { x: 0, opacity: 1 },
    exit: (dir: number) => ({ x: dir > 0 ? -80 : 80, opacity: 0 }),
  };

  return (
    <>
      <div className="space-y-3">
        <div className="group relative aspect-[16/10] overflow-hidden rounded-[16px] bg-sand">
          <AnimatePresence initial={false} custom={direction} mode="popLayout">
            <motion.div
              key={active.key}
              custom={direction}
              variants={slideVariants}
              initial="enter"
              animate="center"
              exit="exit"
              transition={{ duration: 0.35, ease: 'easeInOut' }}
              className="absolute inset-0"
            >
              <ResponsiveTourImage
                mediaFileId={active.mediaFileId}
                fallbackUrl={active.fallbackUrl}
                alt={active.alt}
                variant="gallery"
                priority
                className="h-full w-full cursor-pointer object-cover"
              />
            </motion.div>
          </AnimatePresence>
          <button
            type="button"
            className="absolute inset-0"
            aria-label="Открыть фото"
            onClick={() => setLightboxOpen(true)}
          />

          {slides.length > 1 && (
            <>
              <div className="absolute inset-y-0 left-3 z-10 flex items-center opacity-100 sm:opacity-0 sm:transition-opacity sm:group-hover:opacity-100">
                <NavButton direction="prev" onClick={goPrev} />
              </div>
              <div className="absolute inset-y-0 right-3 z-10 flex items-center opacity-100 sm:opacity-0 sm:transition-opacity sm:group-hover:opacity-100">
                <NavButton direction="next" onClick={goNext} />
              </div>
            </>
          )}

          <button
            type="button"
            onClick={() => setLightboxOpen(true)}
            className="absolute bottom-3 right-3 flex items-center gap-1.5 rounded-lg bg-white/80 px-3 py-1.5 text-xs font-medium text-dark backdrop-blur-sm transition-colors hover:bg-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
          >
            <ZoomIn size={14} />
            {safeIndex + 1} / {slides.length}
          </button>
        </div>

        {slides.length > 1 && (
          <div className="flex gap-2 overflow-x-auto pb-1">
            {slides.map((slide, index) => (
              <button
                key={slide.key}
                type="button"
                onClick={() => navigate(index)}
                aria-label={slide.alt}
                aria-current={index === safeIndex}
                className={clsx(
                  'relative h-16 w-20 flex-shrink-0 overflow-hidden rounded-xl focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary sm:h-[4.5rem] sm:w-24',
                  index === safeIndex
                    ? 'ring-2 ring-primary ring-offset-2'
                    : 'opacity-60 hover:opacity-100',
                )}
              >
                <ResponsiveTourImage
                  mediaFileId={slide.mediaFileId}
                  fallbackUrl={slide.fallbackUrl}
                  alt=""
                  variant="thumb"
                  className="h-full w-full object-cover"
                />
              </button>
            ))}
          </div>
        )}
      </div>

      {createPortal(
        <AnimatePresence>
          {lightboxOpen && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="fixed inset-0 z-[60] flex items-center justify-center bg-dark/90 backdrop-blur-md"
              onClick={() => setLightboxOpen(false)}
              role="dialog"
              aria-modal="true"
              aria-label="Галерея тура"
            >
              <button
                type="button"
                onClick={() => setLightboxOpen(false)}
                aria-label="Закрыть галерею"
                className="absolute right-4 top-4 z-10 flex h-11 w-11 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20"
              >
                <X size={22} />
              </button>

              <div
                className="relative flex h-[85vh] w-[92vw] max-w-5xl items-center justify-center"
                onClick={(e) => e.stopPropagation()}
              >
                <ResponsiveTourImage
                  mediaFileId={active.mediaFileId}
                  fallbackUrl={active.fallbackUrl}
                  alt={active.alt}
                  variant="gallery"
                  priority
                  className="max-h-full max-w-full rounded-lg object-contain"
                />

                {slides.length > 1 && (
                  <>
                    <div className="absolute left-2 top-1/2 -translate-y-1/2">
                      <NavButton direction="prev" onClick={goPrev} />
                    </div>
                    <div className="absolute right-2 top-1/2 -translate-y-1/2">
                      <NavButton direction="next" onClick={goNext} />
                    </div>
                  </>
                )}

                <div className="absolute bottom-4 left-1/2 -translate-x-1/2 rounded-full bg-white/10 px-4 py-1.5 text-sm text-white backdrop-blur-sm">
                  {safeIndex + 1} / {slides.length}
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>,
        document.body,
      )}
    </>
  );
}
