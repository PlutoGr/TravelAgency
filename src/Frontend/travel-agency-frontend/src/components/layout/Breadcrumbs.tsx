import { Link } from 'react-router-dom';
import { ChevronRight } from 'lucide-react';
import clsx from 'clsx';

interface BreadcrumbItem {
  label: string;
  path?: string;
}

interface BreadcrumbsProps {
  items: BreadcrumbItem[];
}

export default function Breadcrumbs({ items }: BreadcrumbsProps) {
  return (
    <nav aria-label="Breadcrumb" className="flex items-center gap-1.5 text-sm">
      {items.map((item, idx) => {
        const isLast = idx === items.length - 1;
        return (
          <span key={item.label} className="flex items-center gap-1.5">
            {idx > 0 && (
              <ChevronRight size={14} className="text-warm-gray" />
            )}
            {isLast || !item.path ? (
              <span
                className={clsx(
                  'font-medium',
                  isLast ? 'text-dark' : 'text-warm-gray',
                )}
              >
                {item.label}
              </span>
            ) : (
              <Link
                to={item.path}
                className="text-warm-gray transition-colors hover:text-primary"
              >
                {item.label}
              </Link>
            )}
          </span>
        );
      })}
    </nav>
  );
}
