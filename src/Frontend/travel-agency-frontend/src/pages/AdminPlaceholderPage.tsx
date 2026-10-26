import { Link } from 'react-router-dom';
import { PageTransition } from '@/components/common';

export default function AdminPlaceholderPage({ title }: { title: string }) {
  return (
    <PageTransition>
      <h1 className="font-heading text-2xl font-bold text-dark sm:text-3xl">{title}</h1>
      <p className="mt-3 text-warm-gray">Раздел в разработке.</p>
      <Link
        to="/admin"
        className="mt-6 inline-block text-sm font-medium text-primary hover:underline"
      >
        К разделу «Администрирование»
      </Link>
    </PageTransition>
  );
}
