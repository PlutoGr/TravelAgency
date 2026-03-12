import { motion } from 'framer-motion';
import { Compass } from 'lucide-react';
import type { Tour } from '@/types';
import { Skeleton } from '@/components/ui';
import TourCard from './TourCard.tsx';

interface TourGridProps {
  tours: Tour[];
  isLoading: boolean;
}

const containerVariants = {
  hidden: {},
  visible: {
    transition: { staggerChildren: 0.08 },
  },
};

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.4, ease: 'easeOut' as const } },
};

function SkeletonCard() {
  return (
    <div className="overflow-hidden rounded-[16px] bg-white shadow-card">
      <Skeleton variant="rectangular" className="aspect-[4/3] w-full !rounded-none" />
      <div className="space-y-3 p-4">
        <Skeleton className="h-3 w-1/2" />
        <Skeleton className="h-5 w-full" />
        <Skeleton className="h-5 w-3/4" />
        <Skeleton className="h-4 w-1/3" />
        <div className="flex items-end justify-between pt-2">
          <Skeleton className="h-6 w-24" />
          <Skeleton variant="rectangular" className="h-9 w-24 !rounded-[12px]" />
        </div>
      </div>
    </div>
  );
}

export default function TourGrid({ tours, isLoading }: TourGridProps) {
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }, (_, i) => (
          <SkeletonCard key={i} />
        ))}
      </div>
    );
  }

  if (tours.length === 0) {
    return (
      <div className="flex min-h-[40vh] flex-col items-center justify-center gap-4 text-center">
        <div className="flex h-20 w-20 items-center justify-center rounded-full bg-sand">
          <Compass size={36} className="text-warm-gray" />
        </div>
        <h3 className="font-heading text-xl font-semibold text-dark">
          Туры не найдены
        </h3>
        <p className="max-w-sm text-sm text-warm-gray">
          Попробуйте изменить параметры поиска или сбросить фильтры
        </p>
      </div>
    );
  }

  return (
    <motion.div
      variants={containerVariants}
      initial="hidden"
      animate="visible"
      className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3"
    >
      {tours.map((tour) => (
        <motion.div key={tour.id} variants={itemVariants}>
          <TourCard tour={tour} />
        </motion.div>
      ))}
    </motion.div>
  );
}
