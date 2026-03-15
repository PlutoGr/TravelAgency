import { useState } from 'react';
import { X } from 'lucide-react';
import { Button } from '@/components/ui';

interface DatesEditorProps {
  dates: { start: string; end: string }[];
  onChange: (dates: { start: string; end: string }[]) => void;
}

export default function DatesEditor({ dates, onChange }: DatesEditorProps) {
  const [newStart, setNewStart] = useState('');
  const [newEnd, setNewEnd] = useState('');

  const addDate = () => {
    if (!newStart || !newEnd) return;
    onChange([...dates, { start: newStart, end: newEnd }]);
    setNewStart('');
    setNewEnd('');
  };

  const removeDate = (index: number) => {
    onChange(dates.filter((_, i) => i !== index));
  };

  return (
    <div>
      <label className="mb-1.5 block text-xs font-medium text-warm-gray">
        Даты заездов
      </label>
      <div className="space-y-1.5">
        {dates.map((d, i) => (
          <div
            key={i}
            className="flex items-center gap-2 rounded-lg bg-cream px-3 py-1.5 text-sm text-dark"
          >
            <span className="flex-1">
              {d.start} — {d.end}
            </span>
            <button
              onClick={() => removeDate(i)}
              className="text-warm-gray hover:text-red-500"
            >
              <X size={14} />
            </button>
          </div>
        ))}
      </div>
      <div className="mt-2 grid grid-cols-[1fr_1fr_auto] gap-2">
        <input
          type="date"
          value={newStart}
          onChange={(e) => setNewStart(e.target.value)}
          className="rounded-[12px] border border-sand bg-white px-3 py-2 text-sm text-dark outline-none focus:border-primary focus:ring-2 focus:ring-primary/10"
        />
        <input
          type="date"
          value={newEnd}
          onChange={(e) => setNewEnd(e.target.value)}
          className="rounded-[12px] border border-sand bg-white px-3 py-2 text-sm text-dark outline-none focus:border-primary focus:ring-2 focus:ring-primary/10"
        />
        <Button variant="secondary" size="sm" onClick={addDate}>
          +
        </Button>
      </div>
    </div>
  );
}
