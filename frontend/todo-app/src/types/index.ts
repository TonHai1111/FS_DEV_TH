// Task Status Enum
export enum TaskStatus {
  Todo = 0,
  InProgress = 1,
  Done = 2,
}

// Task Priority Enum
export enum TaskPriority {
  Low = 0,
  Medium = 1,
  High = 2,
}

// User type
export interface User {
  id: number;
  username: string;
  email: string;
  createdAt: string;
}

// Category type
export interface Category {
  id: number;
  name: string;
  color: string;
  taskCount?: number;
}

// Task type
export interface Task {
  id: number;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string;
  categoryId: number | null;
  category: Category | null;
  isOverdue: boolean;
}

// Task Statistics
export interface TaskStats {
  total: number;
  todo: number;
  inProgress: number;
  done: number;
  overdue: number;
}

// API Response wrapper
export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errors: string[] | null;
}

// Auth Response
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

// Request DTOs
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  priority?: TaskPriority;
  dueDate?: string;
  categoryId?: number;
}

export interface UpdateTaskRequest {
  title: string;
  description?: string;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate?: string;
  categoryId?: number;
}

export interface CreateCategoryRequest {
  name: string;
  color?: string;
}

export interface UpdateCategoryRequest {
  name: string;
  color: string;
}

// Task filter params
export interface TaskFilterParams {
  status?: TaskStatus;
  priority?: TaskPriority;
  categoryId?: number;
  search?: string;
  overdue?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
}

// Helper functions for enums
export const getStatusLabel = (status: TaskStatus): string => {
  switch (status) {
    case TaskStatus.Todo:
      return 'To Do';
    case TaskStatus.InProgress:
      return 'In Progress';
    case TaskStatus.Done:
      return 'Done';
    default:
      return 'Unknown';
  }
};

export const getPriorityLabel = (priority: TaskPriority): string => {
  switch (priority) {
    case TaskPriority.Low:
      return 'Low';
    case TaskPriority.Medium:
      return 'Medium';
    case TaskPriority.High:
      return 'High';
    default:
      return 'Unknown';
  }
};

export const getPriorityColor = (priority: TaskPriority): string => {
  switch (priority) {
    case TaskPriority.Low:
      return 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30';
    case TaskPriority.Medium:
      return 'bg-amber-500/20 text-amber-400 border-amber-500/30';
    case TaskPriority.High:
      return 'bg-red-500/20 text-red-400 border-red-500/30';
    default:
      return 'bg-surface-500/20 text-surface-400 border-surface-500/30';
  }
};

export const getStatusColor = (status: TaskStatus): string => {
  switch (status) {
    case TaskStatus.Todo:
      return 'bg-surface-600';
    case TaskStatus.InProgress:
      return 'bg-primary-600';
    case TaskStatus.Done:
      return 'bg-emerald-600';
    default:
      return 'bg-surface-600';
  }
};
