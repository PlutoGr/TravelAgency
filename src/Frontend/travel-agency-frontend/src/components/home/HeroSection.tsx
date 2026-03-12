import { motion } from 'framer-motion';
import { Shield, BadgeCheck, Headphones, XCircle } from 'lucide-react';
import { SearchBar } from '@/components/common';

const advantages = [
  { icon: Shield, label: 'Лучшие цены' },
  { icon: BadgeCheck, label: 'Проверенные отели' },
  { icon: Headphones, label: '24/7 поддержка' },
  { icon: XCircle, label: 'Бесплатная отмена' },
] as const;

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
          <SearchBar className="mx-auto max-w-4xl" />
        </motion.div>

        <motion.div
          variants={childVariants}
          className="mt-10 grid grid-cols-2 gap-3 sm:flex sm:justify-center sm:gap-6"
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
      </motion.div>
    </section>
  );
}
