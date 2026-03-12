import type { User } from '@/types';

export const mockUser: User = {
  id: 'user-1',
  email: 'ivan.ivanov@mail.ru',
  firstName: 'Иван',
  lastName: 'Иванов',
  phone: '+7 (999) 123-45-67',
  avatar: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=100&q=80',
  passport: '4510 123456',
  role: 'client',
  createdAt: '2025-06-15T10:30:00Z',
};

export const mockManager: User = {
  id: 'manager-1',
  email: 'olga.sidorova@travelagency.ru',
  firstName: 'Ольга',
  lastName: 'Сидорова',
  phone: '+7 (495) 987-65-43',
  avatar: 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=100&q=80',
  role: 'manager',
  createdAt: '2024-03-01T09:00:00Z',
};
