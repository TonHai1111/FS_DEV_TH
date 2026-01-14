import { User, TaskStats } from '../types';
import { useAuth } from '../context/AuthContext';
import { 
  Plus, 
  Menu, 
  LogOut, 
  Sparkles,
  CheckCircle2,
  Clock,
  AlertCircle
} from 'lucide-react';

interface HeaderProps {
  user: User | null;
  stats: TaskStats | null;
  onCreateTask: () => void;
  onToggleSidebar: () => void;
}

export default function Header({ user, stats, onCreateTask, onToggleSidebar }: HeaderProps) {
  const { logout } = useAuth();
  
  return (
    <header className="glass border-b border-surface-700/50 px-4 py-3 sticky top-0 z-30">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <button
            onClick={onToggleSidebar}
            className="btn-ghost p-2 lg:hidden"
            aria-label="Toggle sidebar"
          >
            <Menu className="w-5 h-5" />
          </button>
          
          <div className="flex items-center gap-2">
            <div className="p-1.5 bg-primary-600/20 rounded-lg">
              <Sparkles className="w-5 h-5 text-primary-500" />
            </div>
            <h1 className="text-lg font-display font-semibold text-surface-100 hidden sm:block">
              TaskFlow
            </h1>
          </div>
          
          {stats && (
            <div className="hidden md:flex items-center gap-4 ml-6 pl-6 border-l border-surface-700">
              <div className="flex items-center gap-1.5 text-sm">
                <Clock className="w-4 h-4 text-surface-500" />
                <span className="text-surface-400">{stats.inProgress}</span>
                <span className="text-surface-500">in progress</span>
              </div>
              <div className="flex items-center gap-1.5 text-sm">
                <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                <span className="text-surface-400">{stats.done}</span>
                <span className="text-surface-500">completed</span>
              </div>
              {stats.overdue > 0 && (
                <div className="flex items-center gap-1.5 text-sm">
                  <AlertCircle className="w-4 h-4 text-red-500" />
                  <span className="text-red-400">{stats.overdue}</span>
                  <span className="text-surface-500">overdue</span>
                </div>
              )}
            </div>
          )}
        </div>
        
        <div className="flex items-center gap-3">
          <button
            onClick={onCreateTask}
            className="btn-primary"
          >
            <Plus className="w-5 h-5" />
            <span className="hidden sm:inline">New Task</span>
          </button>
          
          <div className="flex items-center gap-3 pl-3 border-l border-surface-700">
            <div className="hidden sm:block text-right">
              <p className="text-sm font-medium text-surface-200">{user?.username}</p>
              <p className="text-xs text-surface-500">{user?.email}</p>
            </div>
            
            <button
              onClick={logout}
              className="btn-ghost p-2 text-surface-400 hover:text-red-400"
              aria-label="Logout"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
}
