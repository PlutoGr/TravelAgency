import { Shield, Wallet, Headphones, Star } from 'lucide-react';
import { FadeInOnScroll, AnimatedCounter } from '@/components/common';
import type { LucideIcon } from 'lucide-react';

interface Advantage {
  icon: LucideIcon;
  color: string;
  title: string;
  description: string;
  counter?: { end: number; suffix: string };
}

const advantages: Advantage[] = [
  {
    icon: Shield,
    color: 'bg-primary/10 text-primary',
    title: 'Надёжность',
    description: 'Более 10 лет на рынке туризма. Все наши туры застрахованы.',
    counter: { end: 10, suffix: '+ лет' },
  },
  {
    icon: Wallet,
    color: 'bg-terracotta/10 text-terracotta',
    title: 'Лучшие цены',
    description: 'Прямые контракты с отелями. Гарантия лучшей цены.',
  },
  {
    icon: Headphones,
    color: 'bg-olive/10 text-olive',
    title: 'Поддержка 24/7',
    description: 'Наши менеджеры на связи круглосуточно.',
  },
  {
    icon: Star,
    color: 'bg-amber-100 text-amber-600',
    title: '2000+ довольных клиентов',
    description: 'Высший рейтинг среди туроператоров.',
    counter: { end: 2000, suffix: '+' },
  },
];

export default function WhyUsSection() {
  return (
    <section id="why-us" className="bg-sand py-16 md:py-24">
      <div className="mx-auto max-w-7xl px-4">
        <FadeInOnScroll>
          <div className="mb-14 text-center">
            <h2 className="font-heading text-3xl font-bold text-dark md:text-4xl">
              Почему выбирают нас
            </h2>
            <div className="mx-auto mt-4 h-1 w-16 rounded-full bg-terracotta" />
          </div>
        </FadeInOnScroll>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {advantages.map((item, idx) => (
            <FadeInOnScroll key={item.title} delay={idx * 0.1}>
              <div className="flex h-full flex-col items-center rounded-2xl bg-white p-8 text-center shadow-card">
                <div
                  className={`mb-5 flex h-16 w-16 items-center justify-center rounded-full ${item.color}`}
                >
                  <item.icon size={28} />
                </div>

                <h3 className="font-heading text-lg font-bold text-dark">
                  {item.counter ? (
                    <AnimatedCounter end={item.counter.end} suffix={item.counter.suffix} />
                  ) : (
                    item.title
                  )}
                </h3>

                {item.counter && (
                  <p className="mt-1 text-sm font-semibold text-warm-gray">{item.title}</p>
                )}

                <p className="mt-3 text-sm leading-relaxed text-warm-gray">{item.description}</p>
              </div>
            </FadeInOnScroll>
          ))}
        </div>
      </div>
    </section>
  );
}
