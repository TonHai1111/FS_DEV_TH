import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '../context/AuthContext';
import { LogIn, Mail, Lock, Sparkles } from 'lucide-react';
import LoadingSpinner from '../components/LoadingSpinner';

const loginSchema = z.object({
  email: z.string().email('Please enter a valid email'),
  password: z.string().min(1, 'Password is required'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });
  
  const onSubmit = async (data: LoginFormData) => {
    setIsSubmitting(true);
    try {
      await login(data);
      navigate('/');
    } catch {
      // Error handled by context
    } finally {
      setIsSubmitting(false);
    }
  };
  
  return (
    <div className="min-h-screen flex">
      {/* Left side - Decorative */}
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-primary-600 via-primary-700 to-primary-900" />
        <div className="absolute inset-0 opacity-20">
          <div className="absolute top-20 left-20 w-64 h-64 bg-white/10 rounded-full blur-3xl" />
          <div className="absolute bottom-20 right-20 w-96 h-96 bg-accent-500/20 rounded-full blur-3xl" />
        </div>
        <div className="relative z-10 flex flex-col justify-center px-16">
          <div className="flex items-center gap-3 mb-8">
            <div className="p-3 bg-white/10 rounded-xl backdrop-blur-sm">
              <Sparkles className="w-8 h-8 text-white" />
            </div>
            <h1 className="text-4xl font-display font-bold text-white">TaskFlow</h1>
          </div>
          <p className="text-xl text-primary-100 max-w-md leading-relaxed">
            Streamline your productivity with our modern task management solution. 
            Organize, prioritize, and accomplish more every day.
          </p>
          <div className="mt-12 grid grid-cols-2 gap-6">
            <div className="p-4 bg-white/10 backdrop-blur-sm rounded-xl">
              <div className="text-3xl font-bold text-white">10K+</div>
              <div className="text-primary-200">Active Users</div>
            </div>
            <div className="p-4 bg-white/10 backdrop-blur-sm rounded-xl">
              <div className="text-3xl font-bold text-white">1M+</div>
              <div className="text-primary-200">Tasks Completed</div>
            </div>
          </div>
        </div>
      </div>
      
      {/* Right side - Login Form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-8">
        <div className="w-full max-w-md">
          <div className="lg:hidden flex items-center gap-3 mb-8 justify-center">
            <div className="p-2 bg-primary-600/20 rounded-xl">
              <Sparkles className="w-6 h-6 text-primary-500" />
            </div>
            <h1 className="text-2xl font-display font-bold text-surface-100">TaskFlow</h1>
          </div>
          
          <div className="glass rounded-2xl p-8 animate-fade-in">
            <h2 className="text-2xl font-display font-bold text-surface-100 mb-2">Welcome back</h2>
            <p className="text-surface-400 mb-8">Sign in to continue to your dashboard</p>
            
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
              <div>
                <label htmlFor="email" className="block text-sm font-medium text-surface-300 mb-2">
                  Email Address
                </label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-surface-500" />
                  <input
                    {...register('email')}
                    type="email"
                    id="email"
                    placeholder="you@example.com"
                    className="w-full pl-11"
                    disabled={isSubmitting}
                  />
                </div>
                {errors.email && (
                  <p className="mt-1 text-sm text-red-400">{errors.email.message}</p>
                )}
              </div>
              
              <div>
                <label htmlFor="password" className="block text-sm font-medium text-surface-300 mb-2">
                  Password
                </label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-surface-500" />
                  <input
                    {...register('password')}
                    type="password"
                    id="password"
                    placeholder="••••••••"
                    className="w-full pl-11"
                    disabled={isSubmitting}
                  />
                </div>
                {errors.password && (
                  <p className="mt-1 text-sm text-red-400">{errors.password.message}</p>
                )}
              </div>
              
              <button
                type="submit"
                disabled={isSubmitting}
                className="btn-primary w-full py-3"
              >
                {isSubmitting ? (
                  <LoadingSpinner size="sm" />
                ) : (
                  <>
                    <LogIn className="w-5 h-5" />
                    Sign In
                  </>
                )}
              </button>
            </form>
            
            <p className="mt-6 text-center text-surface-400">
              Don't have an account?{' '}
              <Link to="/register" className="text-primary-400 hover:text-primary-300 font-medium transition-colors">
                Create one
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
