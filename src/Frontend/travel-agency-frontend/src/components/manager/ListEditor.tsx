import { useState } from 'react';
import { X } from 'lucide-react';
import { Button } from '@/components/ui';

interface ListEditorProps {
  label: string;
  items: string[];
  onChange: (items: string[]) => void;
  placeholder?: string;
}

export default function ListEditor({
  label,
  items,
  onChange,
  placeholder = 'Новый пункт...',
}: ListEditorProps) {
  const [newItem, setNewItem] = useState('');

  const addItem = () => {
    if (!newItem.trim()) return;
    onChange([...items, newItem.trim()]);
    setNewItem('');
  };

  const removeItem = (index: number) => {
    onChange(items.filter((_, i) => i !== index));
  };

  return (
    <div>
      <label className="mb-1.5 block text-xs font-medium text-warm-gray">
        {label}
      </label>
      <div className="space-y-1.5">
        {items.map((item, i) => (
          <div
            key={i}
            className="flex items-center gap-2 rounded-lg bg-cream px-3 py-1.5 text-sm text-dark"
          >
            <span className="flex-1">{item}</span>
            <button
              onClick={() => removeItem(i)}
              className="text-warm-gray hover:text-red-500"
            >
              <X size={14} />
            </button>
          </div>
        ))}
      </div>
      <div className="mt-2 flex gap-2">
        <input
          type="text"
          value={newItem}
          onChange={(e) => setNewItem(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && addItem()}
          placeholder={placeholder}
          className="flex-1 rounded-[12px] border border-sand bg-white px-4 py-2 text-sm text-dark outline-none placeholder:text-warm-gray focus:border-primary focus:ring-2 focus:ring-primary/10"
        />
        <Button variant="secondary" size="sm" onClick={addItem}>
          Добавить
        </Button>
      </div>
    </div>
  );
}
