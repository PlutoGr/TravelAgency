import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { BadgeCheck } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { SearchBar, type SearchBarValues } from '@/components/common';
import { getDestinations } from '@/api/catalog';
import type { Destination } from '@/types';
import { catalogPath, resolveHomeSearch } from '@/utils/catalogQuery';

// Здесь только то, что сайт уже выполняет. Цены, поддержка и отмена вернутся отдельными задачами.
const advantages: { icon: LucideIcon; label: string }[] = [
  { icon: BadgeCheck, label: 'Проверенные отели' },
];

const containerVariants = {
  hidden: {},
  visible: {
    transition: { staggerChildren: 0.15 },
  },
};

const childVariants = {
  hidden: { opacity: 0, y: 30 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.6, ease: 'easeOut' as const } },
};

export default function HeroSection() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { data: destinations = [] } = useQuery({
    queryKey: ['destinations'],
    queryFn: getDestinations,
  });

  async function handleSearch(values: SearchBarValues) {
    let known: Destination[] = destinations;
    if (known.length === 0) {
      try {
        known = await queryClient.fetchQuery({
          queryKey: ['destinations'],
          queryFn: getDestinations,
        });
      } catch {
        known = [];
      }
    }
    navigate(catalogPath(resolveHomeSearch(values, known)));
  }

  return (
    <section className="relative flex min-h-screen items-center justify-center overflow-hidden">
      <div
        className="absolute inset-0 bg-cover bg-center"
        style={{
          backgroundImage:
            'url(https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=1920&q=80)',
        }}
      />
      <div className="absolute inset-0 bg-gradient-to-b from-black/50 to-black/30" />

      <motion.div
        variants={containerVariants}
        initial="hidden"
        animate="visible"
        className="relative z-10 mx-auto w-full max-w-5xl px-4 py-32 text-center"
      >
        <motion.h1
          variants={childVariants}
          className="font-heading text-5xl font-bold text-white md:text-7xl"
        >
          Путешествуй без забот
        </motion.h1>

        <motion.p
          variants={childVariants}
          className="mx-auto mt-5 max-w-2xl text-lg text-white/80 md:text-xl"
        >
          Откройте мир с лучшими турами от наших экспертов
        </motion.p>

        <motion.div variants={childVariants} className="mt-10">
          <SearchBar
            className="mx-auto w-full max-w-4xl"
            destinations={destinations}
            onSearch={handleSearch}
          />
        </motion.div>

        {advantages.length > 0 && (
          <motion.div
            variants={childVariants}
            className="mt-10 flex flex-wrap justify-center gap-3 sm:gap-6"
          >
            {advantages.map(({ icon: Icon, label }) => (
              <div
                key={label}
                className="flex items-center justify-center gap-2 rounded-full bg-white/15 px-5 py-2.5 text-sm font-medium text-white backdrop-blur-sm"
              >
                <Icon size={18} className="shrink-0" />
                <span>{label}</span>
              </div>
            ))}
          </motion.div>
        )}
      </motion.div>
    </section>
  );
}
