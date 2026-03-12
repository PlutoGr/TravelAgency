import { useState } from "react";
import { MapPin, Calendar, Users, Search } from "lucide-react";
import clsx from "clsx";
import type { FormEvent } from "react";

interface SearchBarValues {
  destination: string;
  date: string;
  travelers: number;
}

interface SearchBarProps {
  onSearch?: (values: SearchBarValues) => void;
  className?: string;
}

export default function SearchBar({ onSearch, className }: SearchBarProps) {
  const [destination, setDestination] = useState("");
  const [date, setDate] = useState("");
  const [travelers, setTravelers] = useState(1);

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    onSearch?.({ destination, date, travelers });
  }

  return (
    <form
      onSubmit={handleSubmit}
      className={clsx(
        "flex flex-col gap-0 overflow-hidden rounded-[20px] bg-white shadow-card md:flex-row md:items-stretch",
        className,
      )}
    >
      <div className="group relative flex flex-1 items-center gap-3 border-b border-sand px-5 py-4 transition-colors focus-within:bg-cream md:border-b-0 md:border-r">
        <MapPin size={20} className="shrink-0 text-terracotta" />
        <div className="flex-1">
          <label htmlFor="search-dest" className="block text-xs font-medium text-warm-gray">
            Куда?
          </label>
          <input
            id="search-dest"
            type="text"
            placeholder="Город или страна"
            value={destination}
            onChange={(e) => setDestination(e.target.value)}
            className="w-full bg-transparent text-sm text-dark outline-none placeholder:text-warm-gray/60"
          />
        </div>
      </div>

      <div className="group relative flex flex-1 items-center gap-3 border-b border-sand px-5 py-4 transition-colors focus-within:bg-cream md:border-b-0 md:border-r">
        <Calendar size={20} className="shrink-0 text-olive" />
        <div className="flex-1">
          <label htmlFor="search-date" className="block text-xs font-medium text-warm-gray">
            Когда?
          </label>
          <input
            id="search-date"
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
            className="w-full bg-transparent text-sm text-dark outline-none"
          />
        </div>
      </div>

      <div className="group relative flex flex-1 items-center gap-3 border-b border-sand px-5 py-4 transition-colors focus-within:bg-cream md:border-b-0 md:border-r">
        <Users size={20} className="shrink-0 text-primary" />
        <div className="flex-1">
          <label htmlFor="search-travelers" className="block text-xs font-medium text-warm-gray">
            Сколько?
          </label>
          <input
            id="search-travelers"
            type="number"
            min={1}
            max={20}
            value={travelers}
            onChange={(e) => setTravelers(Number(e.target.value))}
            className="w-full bg-transparent text-sm text-dark outline-none"
          />
        </div>
      </div>

      <button
        type="submit"
        className="flex items-center justify-center gap-2 bg-primary px-8 py-4 font-heading font-semibold text-white transition-colors hover:bg-primary-light active:bg-primary-dark md:rounded-none"
      >
        <Search size={20} />
        <span>Найти</span>
      </button>
    </form>
  );
}
