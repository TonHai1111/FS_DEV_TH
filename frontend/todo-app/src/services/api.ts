import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { 
  ApiResponse, 
  AuthResponse, 
  LoginRequest, 
  RegisterRequest,
  Task,
  Category,
  CreateTaskRequest,
  UpdateTaskRequest,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  TaskFilterParams,
  TaskStats,
  TaskStatus,
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

// Create axios instance
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Token storage keys
const ACCESS_TOKEN_KEY = 'accessToken';
const REFRESH_TOKEN_KEY = 'refreshToken';
const USER_KEY = 'user';

// Token helpers with safe JSON parsing
export const getAccessToken = (): string | null => {
  try {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  } catch {
    return null;
  }
};

export const getRefreshToken = (): string | null => {
  try {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  } catch {
    return null;
  }
};

export const getStoredUser = (): AuthResponse['user'] | null => {
  try {
    const user = localStorage.getItem(USER_KEY);
    if (!user) return null;
    const parsed = JSON.parse(user);
    // Validate the parsed user has expected properties
    if (parsed && typeof parsed.id === 'number' && typeof parsed.username === 'string') {
      return parsed;
    }
    // Invalid user data, clear it
    clearTokens();
    return null;
  } catch {
    // Corrupted localStorage, clear tokens
    clearTokens();
    return null;
  }
};

export const setTokens = (accessToken: string, refreshToken: string, user: AuthResponse['user']) => {
  try {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  } catch {
    // localStorage might be full or disabled, fail silently
    console.error('Failed to save tokens to localStorage');
  }
};

export const clearTokens = () => {
  try {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  } catch {
    // Fail silently
  }
};

// Request interceptor - add auth token
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Token refresh state to prevent multiple refresh attempts
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Response interceptor - handle token refresh with queue to prevent race conditions
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse<null>>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // If 401 and we haven't retried yet, try to refresh the token
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // If already refreshing, queue this request
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = getRefreshToken();
      if (refreshToken) {
        try {
          const response = await axios.post<ApiResponse<AuthResponse>>(
            `${API_BASE_URL}/auth/refresh`,
            { refreshToken }
          );

          if (response.data.success && response.data.data) {
            const { accessToken, refreshToken: newRefreshToken, user } = response.data.data;
            setTokens(accessToken, newRefreshToken, user);

            processQueue(null, accessToken);

            // Retry original request with new token
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${accessToken}`;
            }
            return api(originalRequest);
          } else {
            // Response format unexpected
            const refreshError = new Error('Token refresh failed');
            processQueue(refreshError, null);
            clearTokens();
            window.location.href = '/login';
            return Promise.reject(refreshError);
          }
        } catch (refreshError) {
          // Refresh failed, clear tokens and redirect to login
          processQueue(refreshError as Error, null);
          clearTokens();
          window.location.href = '/login';
          return Promise.reject(refreshError);
        } finally {
          isRefreshing = false;
        }
      } else {
        // No refresh token, redirect to login
        clearTokens();
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  }
);

// Helper to unwrap API responses and throw errors if unsuccessful
const unwrapResponse = <T>(response: { data: ApiResponse<T> }, errorMessage: string): NonNullable<T> => {
  if (!response.data.success || response.data.data == null) {
    throw new Error(response.data.message || errorMessage);
  }
  return response.data.data as NonNullable<T>;
};

const unwrapResponseOrEmpty = <T>(response: { data: ApiResponse<T> }, errorMessage: string, defaultValue: T): T => {
  if (!response.data.success) {
    throw new Error(response.data.message || errorMessage);
  }
  return response.data.data ?? defaultValue;
};

const unwrapVoidResponse = (response: { data: ApiResponse<null> }, errorMessage: string): void => {
  if (!response.data.success) {
    throw new Error(response.data.message || errorMessage);
  }
};

// Auth API
export const authApi = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/auth/login', data);
    return unwrapResponse(response, 'Login failed');
  },

  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/auth/register', data);
    return unwrapResponse(response, 'Registration failed');
  },

  logout: async (): Promise<void> => {
    try {
      await api.post('/auth/logout');
    } finally {
      clearTokens();
    }
  },

  getCurrentUser: async () => {
    const response = await api.get<ApiResponse<AuthResponse['user']>>('/auth/me');
    return response.data.data;
  },
};

// Tasks API
export const tasksApi = {
  getAll: async (params?: TaskFilterParams): Promise<Task[]> => {
    const response = await api.get<ApiResponse<Task[]>>('/tasks', { params });
    return unwrapResponseOrEmpty(response, 'Failed to fetch tasks', []);
  },

  getById: async (id: number): Promise<Task> => {
    const response = await api.get<ApiResponse<Task>>(`/tasks/${id}`);
    return unwrapResponse(response, 'Task not found');
  },

  create: async (data: CreateTaskRequest): Promise<Task> => {
    const response = await api.post<ApiResponse<Task>>('/tasks', data);
    return unwrapResponse(response, 'Failed to create task');
  },

  update: async (id: number, data: UpdateTaskRequest): Promise<Task> => {
    const response = await api.put<ApiResponse<Task>>(`/tasks/${id}`, data);
    return unwrapResponse(response, 'Failed to update task');
  },

  updateStatus: async (id: number, status: TaskStatus): Promise<Task> => {
    const response = await api.patch<ApiResponse<Task>>(`/tasks/${id}/status`, { status });
    return unwrapResponse(response, 'Failed to update task status');
  },

  delete: async (id: number): Promise<void> => {
    const response = await api.delete<ApiResponse<null>>(`/tasks/${id}`);
    unwrapVoidResponse(response, 'Failed to delete task');
  },

  getStats: async (): Promise<TaskStats> => {
    const response = await api.get<ApiResponse<TaskStats>>('/tasks/stats');
    return unwrapResponse(response, 'Failed to fetch stats');
  },
};

// Categories API
export const categoriesApi = {
  getAll: async (): Promise<Category[]> => {
    const response = await api.get<ApiResponse<Category[]>>('/categories');
    return unwrapResponseOrEmpty(response, 'Failed to fetch categories', []);
  },

  getById: async (id: number): Promise<Category> => {
    const response = await api.get<ApiResponse<Category>>(`/categories/${id}`);
    return unwrapResponse(response, 'Category not found');
  },

  create: async (data: CreateCategoryRequest): Promise<Category> => {
    const response = await api.post<ApiResponse<Category>>('/categories', data);
    return unwrapResponse(response, 'Failed to create category');
  },

  update: async (id: number, data: UpdateCategoryRequest): Promise<Category> => {
    const response = await api.put<ApiResponse<Category>>(`/categories/${id}`, data);
    return unwrapResponse(response, 'Failed to update category');
  },

  delete: async (id: number): Promise<void> => {
    const response = await api.delete<ApiResponse<null>>(`/categories/${id}`);
    unwrapVoidResponse(response, 'Failed to delete category');
  },
};

export default api;
