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

// Token helpers
export const getAccessToken = (): string | null => localStorage.getItem(ACCESS_TOKEN_KEY);
export const getRefreshToken = (): string | null => localStorage.getItem(REFRESH_TOKEN_KEY);
export const getStoredUser = (): AuthResponse['user'] | null => {
  const user = localStorage.getItem(USER_KEY);
  return user ? JSON.parse(user) : null;
};

export const setTokens = (accessToken: string, refreshToken: string, user: AuthResponse['user']) => {
  localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
};

export const clearTokens = () => {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
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

// Response interceptor - handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse<null>>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    
    // If 401 and we haven't retried yet, try to refresh the token
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      
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
            
            // Retry original request with new token
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${accessToken}`;
            }
            return api(originalRequest);
          }
        } catch {
          // Refresh failed, clear tokens and redirect to login
          clearTokens();
          window.location.href = '/login';
        }
      }
    }
    
    return Promise.reject(error);
  }
);

// Auth API
export const authApi = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/auth/login', data);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Login failed');
    }
    return response.data.data;
  },
  
  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/auth/register', data);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Registration failed');
    }
    return response.data.data;
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
    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to fetch tasks');
    }
    return response.data.data || [];
  },
  
  getById: async (id: number): Promise<Task> => {
    const response = await api.get<ApiResponse<Task>>(`/tasks/${id}`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Task not found');
    }
    return response.data.data;
  },
  
  create: async (data: CreateTaskRequest): Promise<Task> => {
    const response = await api.post<ApiResponse<Task>>('/tasks', data);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to create task');
    }
    return response.data.data;
  },
  
  update: async (id: number, data: UpdateTaskRequest): Promise<Task> => {
    const response = await api.put<ApiResponse<Task>>(`/tasks/${id}`, data);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to update task');
    }
    return response.data.data;
  },
  
  updateStatus: async (id: number, status: TaskStatus): Promise<Task> => {
    const response = await api.patch<ApiResponse<Task>>(`/tasks/${id}/status`, { status });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to update task status');
    }
    return response.data.data;
  },
  
  delete: async (id: number): Promise<void> => {
    const response = await api.delete<ApiResponse<null>>(`/tasks/${id}`);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to delete task');
    }
  },
  
  getStats: async (): Promise<TaskStats> => {
    const response = await api.get<ApiResponse<TaskStats>>('/tasks/stats');
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to fetch stats');
    }
    return response.data.data;
  },
};

// Categories API
export const categoriesApi = {
  getAll: async (): Promise<Category[]> => {
    const response = await api.get<ApiResponse<Category[]>>('/categories');
    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to fetch categories');
    }
    return response.data.data || [];
  },
  
  getById: async (id: number): Promise<Category> => {
    const response = await api.get<ApiResponse<Category>>(`/categories/${id}`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Category not found');
    }
    return response.data.data;
  },
  
  create: async (data: CreateCategoryRequest): Promise<Category> => {
    const response = await api.post<ApiResponse<Category>>('/categories', data);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to create category');
    }
    return response.data.data;
  },
  
  update: async (id: number, data: UpdateCategoryRequest): Promise<Category> => {
    const response = await api.put<ApiResponse<Category>>(`/categories/${id}`, data);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to update category');
    }
    return response.data.data;
  },
  
  delete: async (id: number): Promise<void> => {
    const response = await api.delete<ApiResponse<null>>(`/categories/${id}`);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to delete category');
    }
  },
};

export default api;
