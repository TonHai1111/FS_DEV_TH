import { useState } from 'react';
import { Category, TaskFilterParams, TaskPriority, TaskStatus } from '../types';
import { 
  ChevronDown, 
  Plus, 
  Tag, 
  X,
  Filter,
  Trash2
} from 'lucide-react';
import { clsx } from 'clsx';

interface SidebarProps {
  isOpen: boolean;
  categories: Category[];
  filters: TaskFilterParams;
  onFilterChange: (filters: TaskFilterParams) => void;
  onCreateCategory: (data: { name: string; color: string }) => Promise<Category>;
  onDeleteCategory: (id: number) => Promise<void>;
}

const PRESET_COLORS = [
  '#3B82F6', // Blue
  '#10B981', // Emerald
  '#F59E0B', // Amber
  '#EF4444', // Red
  '#8B5CF6', // Violet
  '#EC4899', // Pink
  '#06B6D4', // Cyan
  '#84CC16', // Lime
];

export default function Sidebar({
  isOpen,
  categories,
  filters,
  onFilterChange,
  onCreateCategory,
  onDeleteCategory,
}: SidebarProps) {
  const [showCategoryForm, setShowCategoryForm] = useState(false);
  const [newCategoryName, setNewCategoryName] = useState('');
  const [newCategoryColor, setNewCategoryColor] = useState(PRESET_COLORS[0]);
  const [expandedSections, setExpandedSections] = useState({
    categories: true,
    filters: true,
  });
  
  const toggleSection = (section: 'categories' | 'filters') => {
    setExpandedSections((prev) => ({
      ...prev,
      [section]: !prev[section],
    }));
  };
  
  const handleCreateCategory = async () => {
    if (!newCategoryName.trim()) return;
    
    await onCreateCategory({
      name: newCategoryName.trim(),
      color: newCategoryColor,
    });
    
    setNewCategoryName('');
    setNewCategoryColor(PRESET_COLORS[0]);
    setShowCategoryForm(false);
  };
  
  const handleCategoryFilter = (categoryId: number | undefined) => {
    onFilterChange({
      ...filters,
      categoryId: filters.categoryId === categoryId ? undefined : categoryId,
    });
  };
  
  const handleStatusFilter = (status: TaskStatus | undefined) => {
    onFilterChange({
      ...filters,
      status: filters.status === status ? undefined : status,
    });
  };
  
  const handlePriorityFilter = (priority: TaskPriority | undefined) => {
    onFilterChange({
      ...filters,
      priority: filters.priority === priority ? undefined : priority,
    });
  };
  
  const clearFilters = () => {
    onFilterChange({});
  };
  
  const hasActiveFilters = filters.categoryId !== undefined || 
                          filters.status !== undefined || 
                          filters.priority !== undefined ||
                          filters.overdue;
  
  return (
    <aside
      className={clsx(
        'w-72 glass-light border-r border-surface-700/30 flex-shrink-0 overflow-y-auto transition-all duration-300',
        'absolute lg:relative z-20 h-[calc(100vh-65px)]',
        isOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0 lg:w-0 lg:opacity-0'
      )}
    >
      <div className="p-4 space-y-6">
        {/* Categories Section */}
        <div>
          <button
            onClick={() => toggleSection('categories')}
            className="flex items-center justify-between w-full text-sm font-medium text-surface-300 mb-3"
          >
            <span className="flex items-center gap-2">
              <Tag className="w-4 h-4" />
              Categories
            </span>
            <ChevronDown
              className={clsx(
                'w-4 h-4 transition-transform',
                expandedSections.categories ? 'rotate-0' : '-rotate-90'
              )}
            />
          </button>
          
          {expandedSections.categories && (
            <div className="space-y-1 animate-slide-down">
              {categories.map((category) => (
                <div
                  key={category.id}
                  className={clsx(
                    'group flex items-center justify-between px-3 py-2 rounded-lg cursor-pointer transition-default',
                    filters.categoryId === category.id
                      ? 'bg-primary-600/20 text-primary-300'
                      : 'hover:bg-surface-700/50 text-surface-300'
                  )}
                  onClick={() => handleCategoryFilter(category.id)}
                >
                  <div className="flex items-center gap-2">
                    <div
                      className="w-3 h-3 rounded-full"
                      style={{ backgroundColor: category.color }}
                    />
                    <span className="text-sm">{category.name}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-surface-500">{category.taskCount}</span>
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        onDeleteCategory(category.id);
                      }}
                      className="opacity-0 group-hover:opacity-100 p-1 hover:bg-red-500/20 rounded transition-default"
                    >
                      <Trash2 className="w-3 h-3 text-red-400" />
                    </button>
                  </div>
                </div>
              ))}
              
              {showCategoryForm ? (
                <div className="p-3 bg-surface-800/50 rounded-lg space-y-3 animate-fade-in">
                  <input
                    type="text"
                    value={newCategoryName}
                    onChange={(e) => setNewCategoryName(e.target.value)}
                    placeholder="Category name"
                    className="w-full text-sm"
                    autoFocus
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') handleCreateCategory();
                      if (e.key === 'Escape') setShowCategoryForm(false);
                    }}
                  />
                  <div className="flex flex-wrap gap-2">
                    {PRESET_COLORS.map((color) => (
                      <button
                        key={color}
                        onClick={() => setNewCategoryColor(color)}
                        className={clsx(
                          'w-6 h-6 rounded-full transition-transform',
                          newCategoryColor === color && 'ring-2 ring-white ring-offset-2 ring-offset-surface-800 scale-110'
                        )}
                        style={{ backgroundColor: color }}
                      />
                    ))}
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={handleCreateCategory}
                      className="btn-primary flex-1 py-1.5 text-sm"
                    >
                      Add
                    </button>
                    <button
                      onClick={() => setShowCategoryForm(false)}
                      className="btn-ghost py-1.5 text-sm"
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <button
                  onClick={() => setShowCategoryForm(true)}
                  className="flex items-center gap-2 w-full px-3 py-2 text-sm text-surface-500 hover:text-surface-300 hover:bg-surface-700/50 rounded-lg transition-default"
                >
                  <Plus className="w-4 h-4" />
                  Add Category
                </button>
              )}
            </div>
          )}
        </div>
        
        {/* Filters Section */}
        <div>
          <button
            onClick={() => toggleSection('filters')}
            className="flex items-center justify-between w-full text-sm font-medium text-surface-300 mb-3"
          >
            <span className="flex items-center gap-2">
              <Filter className="w-4 h-4" />
              Filters
              {hasActiveFilters && (
                <span className="w-2 h-2 bg-primary-500 rounded-full" />
              )}
            </span>
            <ChevronDown
              className={clsx(
                'w-4 h-4 transition-transform',
                expandedSections.filters ? 'rotate-0' : '-rotate-90'
              )}
            />
          </button>
          
          {expandedSections.filters && (
            <div className="space-y-4 animate-slide-down">
              {/* Status Filter */}
              <div>
                <p className="text-xs text-surface-500 uppercase tracking-wider mb-2">Status</p>
                <div className="flex flex-wrap gap-2">
                  {[
                    { value: TaskStatus.Todo, label: 'To Do', color: 'bg-surface-600' },
                    { value: TaskStatus.InProgress, label: 'In Progress', color: 'bg-primary-600' },
                    { value: TaskStatus.Done, label: 'Done', color: 'bg-emerald-600' },
                  ].map((status) => (
                    <button
                      key={status.value}
                      onClick={() => handleStatusFilter(status.value)}
                      className={clsx(
                        'px-2.5 py-1 text-xs rounded-full transition-default flex items-center gap-1.5',
                        filters.status === status.value
                          ? 'bg-primary-600/30 text-primary-300 border border-primary-500/50'
                          : 'bg-surface-700/50 text-surface-400 border border-transparent hover:bg-surface-700'
                      )}
                    >
                      <div className={clsx('w-2 h-2 rounded-full', status.color)} />
                      {status.label}
                    </button>
                  ))}
                </div>
              </div>
              
              {/* Priority Filter */}
              <div>
                <p className="text-xs text-surface-500 uppercase tracking-wider mb-2">Priority</p>
                <div className="flex flex-wrap gap-2">
                  {[
                    { value: TaskPriority.Low, label: 'Low', color: 'bg-emerald-500' },
                    { value: TaskPriority.Medium, label: 'Medium', color: 'bg-amber-500' },
                    { value: TaskPriority.High, label: 'High', color: 'bg-red-500' },
                  ].map((priority) => (
                    <button
                      key={priority.value}
                      onClick={() => handlePriorityFilter(priority.value)}
                      className={clsx(
                        'px-2.5 py-1 text-xs rounded-full transition-default flex items-center gap-1.5',
                        filters.priority === priority.value
                          ? 'bg-primary-600/30 text-primary-300 border border-primary-500/50'
                          : 'bg-surface-700/50 text-surface-400 border border-transparent hover:bg-surface-700'
                      )}
                    >
                      <div className={clsx('w-2 h-2 rounded-full', priority.color)} />
                      {priority.label}
                    </button>
                  ))}
                </div>
              </div>
              
              {/* Overdue Filter */}
              <div>
                <button
                  onClick={() => onFilterChange({ ...filters, overdue: !filters.overdue })}
                  className={clsx(
                    'px-3 py-1.5 text-xs rounded-lg transition-default w-full text-left',
                    filters.overdue
                      ? 'bg-red-600/20 text-red-300 border border-red-500/50'
                      : 'bg-surface-700/50 text-surface-400 border border-transparent hover:bg-surface-700'
                  )}
                >
                  Show only overdue tasks
                </button>
              </div>
              
              {/* Clear Filters */}
              {hasActiveFilters && (
                <button
                  onClick={clearFilters}
                  className="flex items-center justify-center gap-2 w-full py-2 text-sm text-surface-400 hover:text-surface-200 transition-default"
                >
                  <X className="w-4 h-4" />
                  Clear all filters
                </button>
              )}
            </div>
          )}
        </div>
      </div>
    </aside>
  );
}
