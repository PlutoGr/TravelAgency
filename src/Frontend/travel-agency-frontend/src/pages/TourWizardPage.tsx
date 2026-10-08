import { useParams, useSearchParams } from 'react-router-dom';
import TourWizard from '@/components/manager/wizard/TourWizard';
import { resolveWizardStep } from '@/domain/tourWizard';

export default function TourWizardPage() {
  const { tourId } = useParams();
  const [params] = useSearchParams();
  const initialStep = resolveWizardStep(params.get('step'));
  const id = !tourId || tourId === 'new' ? null : tourId;
  return <TourWizard tourId={id} initialStep={initialStep} />;
}
