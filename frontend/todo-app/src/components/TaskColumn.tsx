import { ReactNode } from 'react';
import { useDroppable } from '@dnd-kit/core';
import { clsx } from 'clsx';

interface TaskColumnProps {
  id: string;
  title: string;
  color: string;
  count: number;
  children: ReactNode;
}

export default function TaskColumn({ id, title, color, count, children }: TaskColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id });
  
  const colorClasses: Record<string, string> = {
    surface: 'bg-surface-600',
    primary: 'bg-primary-600',
    emerald: 'bg-emerald-600',
  };
  
  return (
    <div className="flex flex-col w-80 lg:w-[calc(33.333%-1rem)] min-w-[300px] lg:min-w-0">
      <div className="flex items-center gap-3 mb-4">
        <div className={clsx('w-3 h-3 rounded-full', colorClasses[color] || 'bg-surface-600')} />
        <h3 className="font-semibold text-surface-200">{title}</h3>
        <span className="px-2 py-0.5 text-xs bg-surface-700 text-surface-400 rounded-full">
          {count}
        </span>
      </div>
      
      <div
        ref={setNodeRef}
        className={clsx(
          'flex-1 p-3 rounded-xl transition-all duration-200 min-h-[200px]',
          'bg-surface-800/30 border-2 border-dashed',
          isOver
            ? 'border-primary-500/50 bg-primary-600/10'
            : 'border-surface-700/50'
        )}
      >
        <div className="space-y-3">
          {children}
          
          {count === 0 && (
            <div className="flex items-center justify-center h-32 text-surface-500 text-sm">
              Drop tasks here
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
