import { useDraggable } from '@dnd-kit/core';
import { Task, getPriorityLabel, getPriorityColor } from '../types';
import { Calendar, Trash2, GripVertical } from 'lucide-react';
import { format, isPast, isToday, isTomorrow } from 'date-fns';
import { clsx } from 'clsx';

interface TaskCardProps {
  task: Task;
  onClick: () => void;
  onDelete: () => void;
  isDragging?: boolean;
}

export default function TaskCard({ task, onClick, onDelete, isDragging = false }: TaskCardProps) {
  const { attributes, listeners, setNodeRef, transform } = useDraggable({
    id: task.id,
  });
  
  const style = transform
    ? {
        transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
      }
    : undefined;
  
  const formatDueDate = (dateStr: string) => {
    const date = new Date(dateStr);
    if (isToday(date)) return 'Today';
    if (isTomorrow(date)) return 'Tomorrow';
    return format(date, 'MMM d');
  };
  
  const isOverdue = task.dueDate && isPast(new Date(task.dueDate)) && task.status !== 2;
  
  return (
    <div
      ref={setNodeRef}
      style={style}
      className={clsx(
        'group glass rounded-xl p-4 cursor-pointer transition-all duration-200',
        'hover:border-surface-500/50 hover:shadow-lg',
        isDragging && 'shadow-2xl ring-2 ring-primary-500/50',
        isOverdue && 'border-red-500/30'
      )}
      onClick={onClick}
    >
      <div className="flex items-start gap-2">
        <button
          {...attributes}
          {...listeners}
          className="mt-0.5 p-1 -ml-1 text-surface-500 hover:text-surface-300 cursor-grab active:cursor-grabbing opacity-0 group-hover:opacity-100 transition-opacity"
          onClick={(e) => e.stopPropagation()}
        >
          <GripVertical className="w-4 h-4" />
        </button>
        
        <div className="flex-1 min-w-0">
          <h4 className="font-medium text-surface-100 truncate">{task.title}</h4>
          
          {task.description && (
            <p className="text-sm text-surface-400 mt-1 line-clamp-2">{task.description}</p>
          )}
          
          <div className="flex items-center gap-2 mt-3 flex-wrap">
            <span
              className={clsx(
                'px-2 py-0.5 text-xs rounded-full border',
                getPriorityColor(task.priority)
              )}
            >
              {getPriorityLabel(task.priority)}
            </span>
            
            {task.category && (
              <span
                className="px-2 py-0.5 text-xs rounded-full bg-surface-700/50 text-surface-300 flex items-center gap-1"
              >
                <span
                  className="w-2 h-2 rounded-full"
                  style={{ backgroundColor: task.category.color }}
                />
                {task.category.name}
              </span>
            )}
            
            {task.dueDate && (
              <span
                className={clsx(
                  'flex items-center gap-1 px-2 py-0.5 text-xs rounded-full',
                  isOverdue
                    ? 'bg-red-500/20 text-red-400'
                    : 'bg-surface-700/50 text-surface-400'
                )}
              >
                <Calendar className="w-3 h-3" />
                {formatDueDate(task.dueDate)}
              </span>
            )}
          </div>
        </div>
        
        <button
          onClick={(e) => {
            e.stopPropagation();
            onDelete();
          }}
          className="p-1.5 text-surface-500 hover:text-red-400 hover:bg-red-500/10 rounded-lg opacity-0 group-hover:opacity-100 transition-all"
        >
          <Trash2 className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
}
