import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { MapPin, Home, ArrowLeft } from 'lucide-react'
import { PageTransition } from '@/components/common'
import { Button } from '@/components/ui'

export default function NotFoundPage() {
  return (
    <PageTransition>
      <div className="flex min-h-[70vh] flex-col items-center justify-center gap-8 px-4 text-center">
        <motion.div
          initial={{ scale: 0.8, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          transition={{ type: 'spring', stiffness: 100, damping: 15 }}
          className="relative"
        >
          <span className="font-heading text-[10rem] leading-none font-black text-primary/10 md:text-[14rem]">
            404
          </span>
          <motion.div
            animate={{ y: [0, -12, 0] }}
            transition={{ repeat: Infinity, duration: 3, ease: 'easeInOut' }}
            className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2"
          >
            <MapPin className="h-16 w-16 text-terracotta md:h-20 md:w-20" strokeWidth={1.5} />
          </motion.div>
        </motion.div>

        <motion.div
          initial={{ y: 20, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          transition={{ delay: 0.2 }}
          className="space-y-3"
        >
          <h1 className="font-heading text-2xl font-bold text-dark md:text-3xl">
            Кажется, вы заблудились
          </h1>
          <p className="mx-auto max-w-md text-warm-gray">
            Эта страница отправилась в путешествие и пока не вернулась. Попробуйте начать с главной страницы.
          </p>
        </motion.div>

        <motion.div
          initial={{ y: 20, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          transition={{ delay: 0.4 }}
          className="flex flex-col gap-3 sm:flex-row"
        >
          <Link to="/">
            <Button leftIcon={<Home className="h-4 w-4" />}>
              На главную
            </Button>
          </Link>
          <Button
            variant="secondary"
            leftIcon={<ArrowLeft className="h-4 w-4" />}
            onClick={() => window.history.back()}
          >
            Вернуться назад
          </Button>
        </motion.div>
      </div>
    </PageTransition>
  )
}
