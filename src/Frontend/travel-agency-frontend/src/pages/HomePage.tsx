import { PageTransition } from '@/components/common';
import {
  HeroSection,
  WhyUsSection,
  HotDealsCarousel,
  DestinationsGrid,
  ReviewsSlider,
} from '@/components/home';

export default function HomePage() {
  return (
    <PageTransition>
      <HeroSection />
      <WhyUsSection />
      <HotDealsCarousel />
      <DestinationsGrid />
      <ReviewsSlider />
    </PageTransition>
  );
}
