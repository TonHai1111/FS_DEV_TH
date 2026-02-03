import { renderHook, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider, useAuth } from '../../context/AuthContext';
import * as api from '../../services/api';

// Mock the api module
vi.mock('../../services/api', () => ({
  authApi: {
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
  },
  setTokens: vi.fn(),
  clearTokens: vi.fn(),
  getAccessToken: vi.fn(),
  getStoredUser: vi.fn(),
}));

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

const mockUser = {
  id: 1,
  username: 'testuser',
  email: 'test@example.com',
  createdAt: '2024-01-01T00:00:00Z',
};

const mockAuthResponse = {
  accessToken: 'mock-access-token',
  refreshToken: 'mock-refresh-token',
  expiresAt: '2024-01-01T01:00:00Z',
  user: mockUser,
};

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>{children}</AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
};

describe('AuthContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getAccessToken).mockReturnValue(null);
    vi.mocked(api.getStoredUser).mockReturnValue(null);
  });

  describe('initial state', () => {
    it('should start with no user and isAuthenticated false', async () => {
      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.user).toBeNull();
      expect(result.current.isAuthenticated).toBe(false);
    });

    it('should restore user from storage if token exists', async () => {
      vi.mocked(api.getAccessToken).mockReturnValue('stored-token');
      vi.mocked(api.getStoredUser).mockReturnValue(mockUser);

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.user).toEqual(mockUser);
      expect(result.current.isAuthenticated).toBe(true);
    });
  });

  describe('login', () => {
    it('should login successfully and set user', async () => {
      vi.mocked(api.authApi.login).mockResolvedValue(mockAuthResponse);

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      await act(async () => {
        await result.current.login({
          email: 'test@example.com',
          password: 'password123',
        });
      });

      expect(api.authApi.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'password123',
      });
      expect(api.setTokens).toHaveBeenCalledWith(
        mockAuthResponse.accessToken,
        mockAuthResponse.refreshToken,
        mockAuthResponse.user
      );
      expect(result.current.user).toEqual(mockUser);
      expect(result.current.isAuthenticated).toBe(true);
    });

    it('should throw error on login failure', async () => {
      const error = new Error('Invalid credentials');
      vi.mocked(api.authApi.login).mockRejectedValue(error);

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      await expect(
        act(async () => {
          await result.current.login({
            email: 'test@example.com',
            password: 'wrongpassword',
          });
        })
      ).rejects.toThrow('Invalid credentials');

      expect(result.current.user).toBeNull();
      expect(result.current.isAuthenticated).toBe(false);
    });
  });

  describe('register', () => {
    it('should register successfully and set user', async () => {
      vi.mocked(api.authApi.register).mockResolvedValue(mockAuthResponse);

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      await act(async () => {
        await result.current.register({
          username: 'newuser',
          email: 'new@example.com',
          password: 'password123',
        });
      });

      expect(api.authApi.register).toHaveBeenCalledWith({
        username: 'newuser',
        email: 'new@example.com',
        password: 'password123',
      });
      expect(api.setTokens).toHaveBeenCalled();
      expect(result.current.user).toEqual(mockUser);
      expect(result.current.isAuthenticated).toBe(true);
    });
  });

  describe('logout', () => {
    it('should logout and clear user', async () => {
      vi.mocked(api.getAccessToken).mockReturnValue('stored-token');
      vi.mocked(api.getStoredUser).mockReturnValue(mockUser);
      vi.mocked(api.authApi.logout).mockResolvedValue(undefined);

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      expect(result.current.isAuthenticated).toBe(true);

      await act(async () => {
        await result.current.logout();
      });

      expect(api.clearTokens).toHaveBeenCalled();
      expect(result.current.user).toBeNull();
      expect(result.current.isAuthenticated).toBe(false);
    });

    it('should clear tokens even if logout API fails', async () => {
      vi.mocked(api.getAccessToken).mockReturnValue('stored-token');
      vi.mocked(api.getStoredUser).mockReturnValue(mockUser);
      vi.mocked(api.authApi.logout).mockRejectedValue(new Error('Network error'));

      const { result } = renderHook(() => useAuth(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      // The logout function uses try/finally (not try/catch), so errors still propagate
      // We need to catch the error while verifying that cleanup happened
      await act(async () => {
        try {
          await result.current.logout();
        } catch {
          // Expected to throw due to network error
        }
      });

      // Should still clear tokens even on error (via finally block)
      expect(api.clearTokens).toHaveBeenCalled();
      expect(result.current.user).toBeNull();
    });
  });
});