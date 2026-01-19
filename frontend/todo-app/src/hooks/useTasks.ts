import { useState, useMemo, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Task, TaskFilterParams, TaskStatus } from '../types';
import { tasksApi, categoriesApi } from '../services/api';
import { useDebounce } from './useDebounce';
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

  // Debounce the search term to prevent excessive API calls
  const debouncedSearch = useDebounce(filters.search, 300);

  // Create debounced filters for API calls
  const debouncedFilters = useMemo(
    () => ({
      ...filters,
      search: debouncedSearch,
    }),
    [filters, debouncedSearch]
  );

  // Common invalidation helpers
  const invalidateTaskQueries = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: ['tasks'] });
    queryClient.invalidateQueries({ queryKey: queryKeys.stats });
  }, [queryClient]);

  const invalidateCategoryQueries = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: queryKeys.categories });
  }, [queryClient]);

  // Common mutation handler factories
  const onSuccessWithToast = (invalidate: () => void, message: string) => () => {
    invalidate();
    toast.success(message);
  };

  const onErrorWithToast = (fallbackMessage: string) => (error: Error) => {
    toast.error(error.message || fallbackMessage);
  };

  // Fetch tasks with React Query using debounced filters
  const {
    data: tasks = [],
    isLoading: isLoadingTasks,
    error: tasksError,
  } = useQuery({
    queryKey: queryKeys.tasks(debouncedFilters),
    queryFn: () => tasksApi.getAll(debouncedFilters),
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
  const { data: stats = null } = useQuery({
    queryKey: queryKeys.stats,
    queryFn: tasksApi.getStats,
  });

  // Combined loading state
  const isLoading = isLoadingTasks || isLoadingCategories;

  // Task mutations
  const createTaskMutation = useMutation({
    mutationFn: tasksApi.create,
    onSuccess: onSuccessWithToast(invalidateTaskQueries, 'Task created successfully'),
    onError: onErrorWithToast('Failed to create task'),
  });

  const updateTaskMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof tasksApi.update>[1] }) =>
      tasksApi.update(id, data),
    onSuccess: onSuccessWithToast(invalidateTaskQueries, 'Task updated successfully'),
    onError: onErrorWithToast('Failed to update task'),
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }: { id: number; status: TaskStatus }) =>
      tasksApi.updateStatus(id, status),
    onMutate: async ({ id, status }) => {
      await queryClient.cancelQueries({ queryKey: ['tasks'] });
      const previousTasks = queryClient.getQueryData<Task[]>(queryKeys.tasks(debouncedFilters));
      queryClient.setQueryData<Task[]>(queryKeys.tasks(debouncedFilters), (old) =>
        old?.map((task) => (task.id === id ? { ...task, status } : task)) ?? []
      );
      return { previousTasks };
    },
    onError: (error: Error, _variables, context) => {
      if (context?.previousTasks) {
        queryClient.setQueryData(queryKeys.tasks(debouncedFilters), context.previousTasks);
      }
      toast.error(`Status update failed: ${error.message || 'Unknown error'}`);
    },
    onSettled: invalidateTaskQueries,
  });

  const deleteTaskMutation = useMutation({
    mutationFn: tasksApi.delete,
    onSuccess: onSuccessWithToast(invalidateTaskQueries, 'Task deleted successfully'),
    onError: onErrorWithToast('Failed to delete task'),
  });

  // Category mutations
  const createCategoryMutation = useMutation({
    mutationFn: categoriesApi.create,
    onSuccess: onSuccessWithToast(invalidateCategoryQueries, 'Category created successfully'),
    onError: onErrorWithToast('Failed to create category'),
  });

  const updateCategoryMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof categoriesApi.update>[1] }) =>
      categoriesApi.update(id, data),
    onSuccess: onSuccessWithToast(invalidateCategoryQueries, 'Category updated successfully'),
    onError: onErrorWithToast('Failed to update category'),
  });

  const deleteCategoryMutation = useMutation({
    mutationFn: categoriesApi.delete,
    onSuccess: () => {
      invalidateCategoryQueries();
      invalidateTaskQueries(); // Tasks may have lost category
      toast.success('Category deleted successfully');
    },
    onError: onErrorWithToast('Failed to delete category'),
  });

  // Public API
  const createTask = (data: Parameters<typeof tasksApi.create>[0]) =>
    createTaskMutation.mutateAsync(data);

  const updateTask = (id: number, data: Parameters<typeof tasksApi.update>[1]) =>
    updateTaskMutation.mutateAsync({ id, data });

  const updateTaskStatus = (id: number, status: TaskStatus) =>
    updateStatusMutation.mutateAsync({ id, status });

  const deleteTask = (id: number) => deleteTaskMutation.mutateAsync(id);

  const createCategory = (data: Parameters<typeof categoriesApi.create>[0]) =>
    createCategoryMutation.mutateAsync(data);

  const updateCategory = (id: number, data: Parameters<typeof categoriesApi.update>[1]) =>
    updateCategoryMutation.mutateAsync({ id, data });

  const deleteCategory = (id: number) => deleteCategoryMutation.mutateAsync(id);

  const refreshAll = useCallback(() => {
    invalidateTaskQueries();
    invalidateCategoryQueries();
  }, [invalidateTaskQueries, invalidateCategoryQueries]);

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
