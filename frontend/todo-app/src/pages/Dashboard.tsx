import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { useTasks } from '../hooks/useTasks';
import Header from '../components/Header';
import Sidebar from '../components/Sidebar';
import TaskBoard from '../components/TaskBoard';
import TaskModal from '../components/TaskModal';
import { Task, TaskStatus, TaskPriority } from '../types';
import LoadingSpinner from '../components/LoadingSpinner';

export default function Dashboard() {
  const { user } = useAuth();
  const {
    tasksByStatus,
    categories,
    stats,
    isLoading,
    filters,
    setFilters,
    createTask,
    updateTask,
    updateTaskStatus,
    deleteTask,
    createCategory,
    deleteCategory,
  } = useTasks();
  
  const [isTaskModalOpen, setIsTaskModalOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [sidebarOpen, setSidebarOpen] = useState(true);
  
  const handleCreateTask = () => {
    setEditingTask(null);
    setIsTaskModalOpen(true);
  };
  
  const handleEditTask = (task: Task) => {
    setEditingTask(task);
    setIsTaskModalOpen(true);
  };
  
  const handleSaveTask = async (data: {
    title: string;
    description?: string;
    priority: TaskPriority;
    dueDate?: string;
    categoryId?: number;
    status?: TaskStatus;
  }) => {
    if (editingTask) {
      await updateTask(editingTask.id, {
        title: data.title,
        description: data.description,
        priority: data.priority,
        dueDate: data.dueDate,
        categoryId: data.categoryId,
        status: data.status ?? editingTask.status,
      });
    } else {
      await createTask({
        title: data.title,
        description: data.description,
        priority: data.priority,
        dueDate: data.dueDate,
        categoryId: data.categoryId,
      });
    }
    setIsTaskModalOpen(false);
  };
  
  const handleStatusChange = async (taskId: number, newStatus: TaskStatus) => {
    await updateTaskStatus(taskId, newStatus);
  };
  
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <LoadingSpinner size="lg" />
          <p className="mt-4 text-surface-400">Loading your tasks...</p>
        </div>
      </div>
    );
  }
  
  return (
    <div className="min-h-screen flex flex-col">
      <Header
        user={user}
        stats={stats}
        onCreateTask={handleCreateTask}
        onToggleSidebar={() => setSidebarOpen(!sidebarOpen)}
      />
      
      <div className="flex flex-1 overflow-hidden">
        <Sidebar
          isOpen={sidebarOpen}
          categories={categories}
          filters={filters}
          onFilterChange={setFilters}
          onCreateCategory={createCategory}
          onDeleteCategory={deleteCategory}
        />
        
        <main className="flex-1 overflow-hidden">
          <TaskBoard
            tasksByStatus={tasksByStatus}
            onTaskClick={handleEditTask}
            onStatusChange={handleStatusChange}
            onDeleteTask={deleteTask}
          />
        </main>
      </div>
      
      <TaskModal
        isOpen={isTaskModalOpen}
        onClose={() => setIsTaskModalOpen(false)}
        onSave={handleSaveTask}
        task={editingTask}
        categories={categories}
      />
    </div>
  );
}
