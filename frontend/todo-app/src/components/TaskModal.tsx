import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Task, Category, TaskPriority, TaskStatus } from '../types';
import { X, Calendar, Flag, Tag } from 'lucide-react';
import { clsx } from 'clsx';
import { format } from 'date-fns';

interface TaskModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (data: TaskFormData) => Promise<void>;
  task: Task | null;
  categories: Category[];
}

const taskSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Title is too long'),
  description: z.string().max(2000, 'Description is too long').optional(),
  priority: z.nativeEnum(TaskPriority),
  status: z.nativeEnum(TaskStatus).optional(),
  dueDate: z.string().optional(),
  categoryId: z.number().optional(),
});

type TaskFormData = z.infer<typeof taskSchema>;

export default function TaskModal({
  isOpen,
  onClose,
  onSave,
  task,
  categories,
}: TaskModalProps) {
  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<TaskFormData>({
    resolver: zodResolver(taskSchema),
    defaultValues: {
      title: '',
      description: '',
      priority: TaskPriority.Medium,
      status: TaskStatus.Todo,
      dueDate: '',
      categoryId: undefined,
    },
  });
  
  const selectedPriority = watch('priority');
  const selectedCategoryId = watch('categoryId');
  
  useEffect(() => {
    if (task) {
      reset({
        title: task.title,
        description: task.description || '',
        priority: task.priority,
        status: task.status,
        dueDate: task.dueDate ? format(new Date(task.dueDate), 'yyyy-MM-dd') : '',
        categoryId: task.categoryId || undefined,
      });
    } else {
      reset({
        title: '',
        description: '',
        priority: TaskPriority.Medium,
        status: TaskStatus.Todo,
        dueDate: '',
        categoryId: undefined,
      });
    }
  }, [task, reset]);
  
  const onSubmit = async (data: TaskFormData) => {
    await onSave({
      ...data,
      dueDate: data.dueDate || undefined,
    });
  };
  
  if (!isOpen) return null;
  
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div 
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
      />
      
      <div className="relative w-full max-w-lg glass rounded-2xl shadow-2xl animate-scale-in">
        <div className="flex items-center justify-between p-4 border-b border-surface-700/50">
          <h2 className="text-lg font-display font-semibold text-surface-100">
            {task ? 'Edit Task' : 'Create New Task'}
          </h2>
          <button
            onClick={onClose}
            className="p-2 text-surface-400 hover:text-surface-200 hover:bg-surface-700/50 rounded-lg transition-default"
          >
            <X className="w-5 h-5" />
          </button>
        </div>
        
        <form onSubmit={handleSubmit(onSubmit)} className="p-4 space-y-4">
          {/* Title */}
          <div>
            <label htmlFor="title" className="block text-sm font-medium text-surface-300 mb-2">
              Task Title
            </label>
            <input
              {...register('title')}
              type="text"
              id="title"
              placeholder="What needs to be done?"
              className="w-full"
              autoFocus
            />
            {errors.title && (
              <p className="mt-1 text-sm text-red-400">{errors.title.message}</p>
            )}
          </div>
          
          {/* Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-surface-300 mb-2">
              Description
            </label>
            <textarea
              {...register('description')}
              id="description"
              rows={3}
              placeholder="Add more details..."
              className="w-full resize-none"
            />
            {errors.description && (
              <p className="mt-1 text-sm text-red-400">{errors.description.message}</p>
            )}
          </div>
          
          {/* Priority */}
          <div>
            <label className="flex items-center gap-2 text-sm font-medium text-surface-300 mb-2">
              <Flag className="w-4 h-4" />
              Priority
            </label>
            <div className="flex gap-2">
              {[
                { value: TaskPriority.Low, label: 'Low', color: 'emerald' },
                { value: TaskPriority.Medium, label: 'Medium', color: 'amber' },
                { value: TaskPriority.High, label: 'High', color: 'red' },
              ].map((priority) => (
                <button
                  key={priority.value}
                  type="button"
                  onClick={() => setValue('priority', priority.value)}
                  className={clsx(
                    'flex-1 py-2 px-3 rounded-lg text-sm font-medium transition-default border',
                    selectedPriority === priority.value
                      ? priority.color === 'emerald'
                        ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/50'
                        : priority.color === 'amber'
                        ? 'bg-amber-500/20 text-amber-400 border-amber-500/50'
                        : 'bg-red-500/20 text-red-400 border-red-500/50'
                      : 'bg-surface-700/50 text-surface-400 border-transparent hover:bg-surface-700'
                  )}
                >
                  {priority.label}
                </button>
              ))}
            </div>
          </div>
          
          {/* Status (only for editing) */}
          {task && (
            <div>
              <label className="block text-sm font-medium text-surface-300 mb-2">
                Status
              </label>
              <div className="flex gap-2">
                {[
                  { value: TaskStatus.Todo, label: 'To Do' },
                  { value: TaskStatus.InProgress, label: 'In Progress' },
                  { value: TaskStatus.Done, label: 'Done' },
                ].map((status) => (
                  <button
                    key={status.value}
                    type="button"
                    onClick={() => setValue('status', status.value)}
                    className={clsx(
                      'flex-1 py-2 px-3 rounded-lg text-sm font-medium transition-default border',
                      watch('status') === status.value
                        ? 'bg-primary-500/20 text-primary-400 border-primary-500/50'
                        : 'bg-surface-700/50 text-surface-400 border-transparent hover:bg-surface-700'
                    )}
                  >
                    {status.label}
                  </button>
                ))}
              </div>
            </div>
          )}
          
          {/* Due Date */}
          <div>
            <label htmlFor="dueDate" className="flex items-center gap-2 text-sm font-medium text-surface-300 mb-2">
              <Calendar className="w-4 h-4" />
              Due Date
            </label>
            <input
              {...register('dueDate')}
              type="date"
              id="dueDate"
              className="w-full"
            />
          </div>
          
          {/* Category */}
          <div>
            <label className="flex items-center gap-2 text-sm font-medium text-surface-300 mb-2">
              <Tag className="w-4 h-4" />
              Category
            </label>
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setValue('categoryId', undefined)}
                className={clsx(
                  'px-3 py-1.5 rounded-lg text-sm transition-default border',
                  selectedCategoryId === undefined
                    ? 'bg-primary-500/20 text-primary-400 border-primary-500/50'
                    : 'bg-surface-700/50 text-surface-400 border-transparent hover:bg-surface-700'
                )}
              >
                None
              </button>
              {categories.map((category) => (
                <button
                  key={category.id}
                  type="button"
                  onClick={() => setValue('categoryId', category.id)}
                  className={clsx(
                    'flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm transition-default border',
                    selectedCategoryId === category.id
                      ? 'bg-primary-500/20 text-primary-400 border-primary-500/50'
                      : 'bg-surface-700/50 text-surface-400 border-transparent hover:bg-surface-700'
                  )}
                >
                  <span
                    className="w-2.5 h-2.5 rounded-full"
                    style={{ backgroundColor: category.color }}
                  />
                  {category.name}
                </button>
              ))}
            </div>
          </div>
          
          {/* Actions */}
          <div className="flex gap-3 pt-4 border-t border-surface-700/50">
            <button
              type="button"
              onClick={onClose}
              className="btn-secondary flex-1"
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn-primary flex-1"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Saving...' : task ? 'Update Task' : 'Create Task'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
