import { useState, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { motion, AnimatePresence } from 'framer-motion';
import { ChevronLeft, ChevronRight, X, ZoomIn } from 'lucide-react';
import clsx from 'clsx';

interface TourGalleryProps {
  photos: string[];
}

function NavButton({
  direction,
  onClick,
  className,
}: {
  direction: 'prev' | 'next';
  onClick: () => void;
  className?: string;
}) {
  const Icon = direction === 'prev' ? ChevronLeft : ChevronRight;
  return (
    <motion.button
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.95 }}
      onClick={onClick}
      className={clsx(
        'flex h-10 w-10 items-center justify-center rounded-full bg-white/80 text-dark shadow-md backdrop-blur-sm transition-colors hover:bg-white',
        className,
      )}
    >
      <Icon size={20} />
    </motion.button>
  );
}

export default function TourGallery({ photos }: TourGalleryProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [direction, setDirection] = useState(0);

  const navigate = useCallback(
    (newIndex: number) => {
      setDirection(newIndex > activeIndex ? 1 : -1);
      setActiveIndex(newIndex);
    },
    [activeIndex],
  );

  const goPrev = useCallback(() => {
    const newIdx = activeIndex === 0 ? photos.length - 1 : activeIndex - 1;
    navigate(newIdx);
  }, [activeIndex, photos.length, navigate]);

  const goNext = useCallback(() => {
    const newIdx = activeIndex === photos.length - 1 ? 0 : activeIndex + 1;
    navigate(newIdx);
  }, [activeIndex, photos.length, navigate]);

  const slideVariants = {
    enter: (dir: number) => ({ x: dir > 0 ? 300 : -300, opacity: 0 }),
    center: { x: 0, opacity: 1 },
    exit: (dir: number) => ({ x: dir > 0 ? -300 : 300, opacity: 0 }),
  };

  return (
    <>
      <div className="space-y-3">
        {/* Main Photo */}
        <div className="group relative aspect-[16/10] overflow-hidden rounded-[16px] bg-sand">
          <AnimatePresence initial={false} custom={direction} mode="popLayout">
            <motion.img
              key={activeIndex}
              src={photos[activeIndex]}
              alt={`Фото ${activeIndex + 1}`}
              custom={direction}
              variants={slideVariants}
              initial="enter"
              animate="center"
              exit="exit"
              transition={{ duration: 0.35, ease: 'easeInOut' }}
              className="absolute inset-0 h-full w-full cursor-pointer object-cover"
              onClick={() => setLightboxOpen(true)}
            />
          </AnimatePresence>

          {photos.length > 1 && (
            <>
              <div className="absolute inset-y-0 left-3 flex items-center opacity-0 transition-opacity group-hover:opacity-100">
                <NavButton direction="prev" onClick={goPrev} />
              </div>
              <div className="absolute inset-y-0 right-3 flex items-center opacity-0 transition-opacity group-hover:opacity-100">
                <NavButton direction="next" onClick={goNext} />
              </div>
            </>
          )}

          <button
            onClick={() => setLightboxOpen(true)}
            className="absolute bottom-3 right-3 flex items-center gap-1.5 rounded-lg bg-white/80 px-3 py-1.5 text-xs font-medium text-dark backdrop-blur-sm transition-colors hover:bg-white"
          >
            <ZoomIn size={14} />
            {activeIndex + 1} / {photos.length}
          </button>
        </div>

        {/* Thumbnails */}
        {photos.length > 1 && (
          <div className="flex gap-2 overflow-x-auto pb-1">
            {photos.map((photo, i) => (
              <button
                key={i}
                onClick={() => navigate(i)}
                className={clsx(
                  'relative flex-shrink-0 overflow-hidden rounded-xl transition-all',
                  i === activeIndex
                    ? 'ring-2 ring-primary ring-offset-2'
                    : 'opacity-60 hover:opacity-100',
                )}
              >
                <img
                  src={photo}
                  alt={`Миниатюра ${i + 1}`}
                  className="h-16 w-20 object-cover sm:h-18 sm:w-24"
                />
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Lightbox */}
      {createPortal(
        <AnimatePresence>
          {lightboxOpen && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="fixed inset-0 z-[60] flex items-center justify-center bg-dark/90 backdrop-blur-md"
              onClick={() => setLightboxOpen(false)}
            >
              <button
                onClick={() => setLightboxOpen(false)}
                className="absolute right-4 top-4 z-10 flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20"
              >
                <X size={22} />
              </button>

              <div
                className="relative flex h-[85vh] w-[90vw] max-w-5xl items-center justify-center"
                onClick={(e) => e.stopPropagation()}
              >
                <AnimatePresence initial={false} custom={direction} mode="popLayout">
                  <motion.img
                    key={activeIndex}
                    src={photos[activeIndex]}
                    alt={`Фото ${activeIndex + 1}`}
                    custom={direction}
                    variants={slideVariants}
                    initial="enter"
                    animate="center"
                    exit="exit"
                    transition={{ duration: 0.35, ease: 'easeInOut' }}
                    className="absolute max-h-full max-w-full rounded-lg object-contain"
                  />
                </AnimatePresence>

                {photos.length > 1 && (
                  <>
                    <div className="absolute -left-2 top-1/2 -translate-y-1/2 sm:left-2">
                      <NavButton direction="prev" onClick={goPrev} />
                    </div>
                    <div className="absolute -right-2 top-1/2 -translate-y-1/2 sm:right-2">
                      <NavButton direction="next" onClick={goNext} />
                    </div>
                  </>
                )}

                <div className="absolute bottom-4 left-1/2 -translate-x-1/2 rounded-full bg-white/10 px-4 py-1.5 text-sm text-white backdrop-blur-sm">
                  {activeIndex + 1} / {photos.length}
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
