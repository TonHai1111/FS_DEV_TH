import { useState, useEffect, useCallback } from 'react';
import { Task, Category, TaskFilterParams, TaskStats, TaskStatus } from '../types';
import { tasksApi, categoriesApi } from '../services/api';
import toast from 'react-hot-toast';

export function useTasks() {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [stats, setStats] = useState<TaskStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState<TaskFilterParams>({});
  
  const fetchTasks = useCallback(async () => {
    try {
      const data = await tasksApi.getAll(filters);
      setTasks(data);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to fetch tasks';
      toast.error(message);
    }
  }, [filters]);
  
  const fetchCategories = useCallback(async () => {
    try {
      const data = await categoriesApi.getAll();
      setCategories(data);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to fetch categories';
      toast.error(message);
    }
  }, []);
  
  const fetchStats = useCallback(async () => {
    try {
      const data = await tasksApi.getStats();
      setStats(data);
    } catch (error) {
      console.error('Failed to fetch stats:', error);
    }
  }, []);
  
  const refreshAll = useCallback(async () => {
    setIsLoading(true);
    await Promise.all([fetchTasks(), fetchCategories(), fetchStats()]);
    setIsLoading(false);
  }, [fetchTasks, fetchCategories, fetchStats]);
  
  useEffect(() => {
    refreshAll();
  }, [refreshAll]);
  
  const createTask = async (data: Parameters<typeof tasksApi.create>[0]) => {
    try {
      const newTask = await tasksApi.create(data);
      setTasks((prev) => [newTask, ...prev]);
      await fetchStats();
      toast.success('Task created successfully');
      return newTask;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to create task';
      toast.error(message);
      throw error;
    }
  };
  
  const updateTask = async (id: number, data: Parameters<typeof tasksApi.update>[1]) => {
    try {
      const updatedTask = await tasksApi.update(id, data);
      setTasks((prev) => prev.map((t) => (t.id === id ? updatedTask : t)));
      await fetchStats();
      toast.success('Task updated successfully');
      return updatedTask;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to update task';
      toast.error(message);
      throw error;
    }
  };
  
  const updateTaskStatus = async (id: number, status: TaskStatus) => {
    // Optimistic update
    setTasks((prev) =>
      prev.map((t) => (t.id === id ? { ...t, status } : t))
    );
    
    try {
      const updatedTask = await tasksApi.updateStatus(id, status);
      setTasks((prev) => prev.map((t) => (t.id === id ? updatedTask : t)));
      await fetchStats();
    } catch (error) {
      // Revert on error
      await fetchTasks();
      const message = error instanceof Error ? error.message : 'Failed to update status';
      toast.error(message);
      throw error;
    }
  };
  
  const deleteTask = async (id: number) => {
    try {
      await tasksApi.delete(id);
      setTasks((prev) => prev.filter((t) => t.id !== id));
      await fetchStats();
      toast.success('Task deleted successfully');
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to delete task';
      toast.error(message);
      throw error;
    }
  };
  
  const createCategory = async (data: Parameters<typeof categoriesApi.create>[0]) => {
    try {
      const newCategory = await categoriesApi.create(data);
      setCategories((prev) => [...prev, newCategory]);
      toast.success('Category created successfully');
      return newCategory;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to create category';
      toast.error(message);
      throw error;
    }
  };
  
  const updateCategory = async (id: number, data: Parameters<typeof categoriesApi.update>[1]) => {
    try {
      const updatedCategory = await categoriesApi.update(id, data);
      setCategories((prev) => prev.map((c) => (c.id === id ? updatedCategory : c)));
      toast.success('Category updated successfully');
      return updatedCategory;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to update category';
      toast.error(message);
      throw error;
    }
  };
  
  const deleteCategory = async (id: number) => {
    try {
      await categoriesApi.delete(id);
      setCategories((prev) => prev.filter((c) => c.id !== id));
      await fetchTasks(); // Refresh tasks as some may have lost their category
      toast.success('Category deleted successfully');
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Failed to delete category';
      toast.error(message);
      throw error;
    }
  };
  
  // Group tasks by status
  const tasksByStatus = {
    [TaskStatus.Todo]: tasks.filter((t) => t.status === TaskStatus.Todo),
    [TaskStatus.InProgress]: tasks.filter((t) => t.status === TaskStatus.InProgress),
    [TaskStatus.Done]: tasks.filter((t) => t.status === TaskStatus.Done),
  };
  
  return {
    tasks,
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
    updateCategory,
    deleteCategory,
    refreshAll,
  };
}
