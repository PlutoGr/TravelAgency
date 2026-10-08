import { PageTransition } from '@/components/common';
import {
  HeroSection,
  WhyUsSection,
  DestinationsGrid,
  ReviewsSlider,
} from '@/components/home';

export default function HomePage() {
  return (
    <PageTransition>
      <HeroSection />
      <WhyUsSection />
      {/* Горящие предложения скрыты, пока нет выдачи: https://github.com/PlutoGr/TravelAgency/issues/74 */}
      <DestinationsGrid />
      <ReviewsSlider />
    </PageTransition>
  );
}
