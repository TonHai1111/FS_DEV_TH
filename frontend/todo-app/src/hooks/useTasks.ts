import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Task, TaskFilterParams, TaskStatus } from '../types';
import { tasksApi, categoriesApi } from '../services/api';
import toast from 'react-hot-toast';

// Query keys for caching
export const queryKeys = {
  tasks: (filters?: TaskFilterParams) => ['tasks', filters] as const,
  categories: ['categories'] as const,
  stats: ['taskStats'] as const,
};

export function useTasks() {
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState<TaskFilterParams>({});

  // Fetch tasks with React Query
  const {
    data: tasks = [],
    isLoading: isLoadingTasks,
    error: tasksError,
  } = useQuery({
    queryKey: queryKeys.tasks(filters),
    queryFn: () => tasksApi.getAll(filters),
  });

  // Fetch categories with React Query
  const {
    data: categories = [],
    isLoading: isLoadingCategories,
  } = useQuery({
    queryKey: queryKeys.categories,
    queryFn: categoriesApi.getAll,
  });

  // Fetch stats with React Query
  const {
    data: stats = null,
  } = useQuery({
    queryKey: queryKeys.stats,
    queryFn: tasksApi.getStats,
  });

  // Combined loading state
  const isLoading = isLoadingTasks || isLoadingCategories;

  // Create task mutation
  const createTaskMutation = useMutation({
    mutationFn: tasksApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: queryKeys.stats });
      toast.success('Task created successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to create task');
    },
  });

  // Update task mutation
  const updateTaskMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof tasksApi.update>[1] }) =>
      tasksApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: queryKeys.stats });
      toast.success('Task updated successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to update task');
    },
  });

  // Update task status mutation (optimistic update)
  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: number; status: TaskStatus }) =>
      tasksApi.updateStatus(id, status),
    onMutate: async ({ id, status }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['tasks'] });

      // Snapshot current tasks
      const previousTasks = queryClient.getQueryData<Task[]>(queryKeys.tasks(filters));

      // Optimistically update
      queryClient.setQueryData<Task[]>(queryKeys.tasks(filters), (old) =>
        old?.map((task) => (task.id === id ? { ...task, status } : task)) ?? []
      );

      return { previousTasks };
    },
    onError: (error: Error, _variables, context) => {
      // Rollback on error
      if (context?.previousTasks) {
        queryClient.setQueryData(queryKeys.tasks(filters), context.previousTasks);
      }
      toast.error(error.message || 'Failed to update status');
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: queryKeys.stats });
    },
  });

  // Delete task mutation
  const deleteTaskMutation = useMutation({
    mutationFn: tasksApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: queryKeys.stats });
      toast.success('Task deleted successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to delete task');
    },
  });

  // Create category mutation
  const createCategoryMutation = useMutation({
    mutationFn: categoriesApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.categories });
      toast.success('Category created successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to create category');
    },
  });

  // Update category mutation
  const updateCategoryMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof categoriesApi.update>[1] }) =>
      categoriesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.categories });
      toast.success('Category updated successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to update category');
    },
  });

  // Delete category mutation
  const deleteCategoryMutation = useMutation({
    mutationFn: categoriesApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.categories });
      queryClient.invalidateQueries({ queryKey: ['tasks'] }); // Tasks may have lost category
      toast.success('Category deleted successfully');
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to delete category');
    },
  });

  // Helper functions that match the old API
  const createTask = async (data: Parameters<typeof tasksApi.create>[0]) => {
    return createTaskMutation.mutateAsync(data);
  };

  const updateTask = async (id: number, data: Parameters<typeof tasksApi.update>[1]) => {
    return updateTaskMutation.mutateAsync({ id, data });
  };

  const updateTaskStatus = async (id: number, status: TaskStatus) => {
    return updateStatusMutation.mutateAsync({ id, status });
  };

  const deleteTask = async (id: number) => {
    return deleteTaskMutation.mutateAsync(id);
  };

  const createCategory = async (data: Parameters<typeof categoriesApi.create>[0]) => {
    return createCategoryMutation.mutateAsync(data);
  };

  const updateCategory = async (id: number, data: Parameters<typeof categoriesApi.update>[1]) => {
    return updateCategoryMutation.mutateAsync({ id, data });
  };

  const deleteCategory = async (id: number) => {
    return deleteCategoryMutation.mutateAsync(id);
  };

  const refreshAll = () => {
    queryClient.invalidateQueries({ queryKey: ['tasks'] });
    queryClient.invalidateQueries({ queryKey: queryKeys.categories });
    queryClient.invalidateQueries({ queryKey: queryKeys.stats });
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
    error: tasksError,
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
