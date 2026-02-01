import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { User, LoginRequest, RegisterRequest } from '../types';
import { authApi, setTokens, clearTokens, getAccessToken, getStoredUser } from '../services/api';
import toast from 'react-hot-toast';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  
  useEffect(() => {
    // Check for existing session on mount
    const initAuth = () => {
      const token = getAccessToken();
      const storedUser = getStoredUser();
      
      if (token && storedUser) {
        setUser(storedUser);
      }
      setIsLoading(false);
    };
    
    initAuth();
  }, []);
  
  const login = async (data: LoginRequest) => {
    try {
      const response = await authApi.login(data);
      setTokens(response.accessToken, response.refreshToken, response.user);
      setUser(response.user);
      toast.success(`Welcome back, ${response.user.username}!`);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Login failed';
      toast.error(message);
      throw error;
    }
  };
  
  const register = async (data: RegisterRequest) => {
    try {
      const response = await authApi.register(data);
      setTokens(response.accessToken, response.refreshToken, response.user);
      setUser(response.user);
      toast.success('Account created successfully!');
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Registration failed';
      toast.error(message);
      throw error;
    }
  };
  
  const logout = async () => {
    try {
      await authApi.logout();
    } finally {
      clearTokens();
      setUser(null);
      queryClient.clear(); // Clear all cached data to prevent data leakage between users
      toast.success('Logged out successfully');
    }
  };
  
  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
