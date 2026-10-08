import { useParams, useSearchParams } from 'react-router-dom';
import TourWizard from '@/components/manager/wizard/TourWizard';

export default function TourWizardPage() {
  const { tourId } = useParams();
  const [params] = useSearchParams();
  const parsed = Number(params.get('step'));
  const initialStep = parsed >= 1 && parsed <= 7 ? parsed : 1;
  const id = !tourId || tourId === 'new' ? null : tourId;
  return <TourWizard tourId={id} initialStep={initialStep} />;
}
