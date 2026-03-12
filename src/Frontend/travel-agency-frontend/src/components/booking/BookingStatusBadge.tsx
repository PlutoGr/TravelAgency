import type { BookingStatus } from '@/types';
import { Badge } from '@/components/ui';

type BadgeVariant = 'blue' | 'green' | 'amber' | 'purple' | 'gray';

const statusConfig: Record<BookingStatus, { label: string; variant: BadgeVariant }> = {
  new: { label: 'Новая', variant: 'blue' },
  in_progress: { label: 'В работе', variant: 'amber' },
  proposal_sent: { label: 'Предложение отправлено', variant: 'purple' },
  confirmed: { label: 'Подтверждена', variant: 'green' },
  closed: { label: 'Закрыта', variant: 'gray' },
};

interface BookingStatusBadgeProps {
  status: BookingStatus;
  size?: 'sm' | 'md';
}

export default function BookingStatusBadge({ status, size = 'md' }: BookingStatusBadgeProps) {
  const { label, variant } = statusConfig[status];

  return (
    <Badge variant={variant} size={size}>
      {label}
    </Badge>
  );
}
