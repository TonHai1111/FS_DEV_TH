import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '../context/AuthContext';
import { UserPlus, Mail, Lock, User, Sparkles } from 'lucide-react';
import LoadingSpinner from '../components/LoadingSpinner';

const registerSchema = z.object({
  username: z
    .string()
    .min(3, 'Username must be at least 3 characters')
    .max(100, 'Username is too long'),
  email: z.string().email('Please enter a valid email'),
  password: z
    .string()
    .min(6, 'Password must be at least 6 characters')
    .max(100, 'Password is too long'),
  confirmPassword: z.string(),
}).refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

type RegisterFormData = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const navigate = useNavigate();
  const { register: registerUser } = useAuth();
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  });
  
  const onSubmit = async (data: RegisterFormData) => {
    setIsSubmitting(true);
    try {
      await registerUser({
        username: data.username,
        email: data.email,
        password: data.password,
      });
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
        <div className="absolute inset-0 bg-gradient-to-br from-accent-600 via-accent-700 to-accent-900" />
        <div className="absolute inset-0 opacity-20">
          <div className="absolute top-20 right-20 w-64 h-64 bg-white/10 rounded-full blur-3xl" />
          <div className="absolute bottom-20 left-20 w-96 h-96 bg-primary-500/20 rounded-full blur-3xl" />
        </div>
        <div className="relative z-10 flex flex-col justify-center px-16">
          <div className="flex items-center gap-3 mb-8">
            <div className="p-3 bg-white/10 rounded-xl backdrop-blur-sm">
              <Sparkles className="w-8 h-8 text-white" />
            </div>
            <h1 className="text-4xl font-display font-bold text-white">TaskFlow</h1>
          </div>
          <p className="text-xl text-accent-100 max-w-md leading-relaxed">
            Join thousands of productive individuals who trust TaskFlow to organize their lives and achieve their goals.
          </p>
          <div className="mt-12 space-y-4">
            <div className="flex items-center gap-3 text-accent-100">
              <div className="w-8 h-8 rounded-full bg-white/20 flex items-center justify-center">✓</div>
              <span>Kanban-style task management</span>
            </div>
            <div className="flex items-center gap-3 text-accent-100">
              <div className="w-8 h-8 rounded-full bg-white/20 flex items-center justify-center">✓</div>
              <span>Priority levels and due dates</span>
            </div>
            <div className="flex items-center gap-3 text-accent-100">
              <div className="w-8 h-8 rounded-full bg-white/20 flex items-center justify-center">✓</div>
              <span>Categories and filters</span>
            </div>
            <div className="flex items-center gap-3 text-accent-100">
              <div className="w-8 h-8 rounded-full bg-white/20 flex items-center justify-center">✓</div>
              <span>Beautiful, responsive design</span>
            </div>
          </div>
        </div>
      </div>
      
      {/* Right side - Register Form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-8">
        <div className="w-full max-w-md">
          <div className="lg:hidden flex items-center gap-3 mb-8 justify-center">
            <div className="p-2 bg-accent-600/20 rounded-xl">
              <Sparkles className="w-6 h-6 text-accent-500" />
            </div>
            <h1 className="text-2xl font-display font-bold text-surface-100">TaskFlow</h1>
          </div>
          
          <div className="glass rounded-2xl p-8 animate-fade-in">
            <h2 className="text-2xl font-display font-bold text-surface-100 mb-2">Create Account</h2>
            <p className="text-surface-400 mb-8">Start your productivity journey today</p>
            
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div>
                <label htmlFor="username" className="block text-sm font-medium text-surface-300 mb-2">
                  Username
                </label>
                <div className="relative">
                  <User className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-surface-500" />
                  <input
                    {...register('username')}
                    type="text"
                    id="username"
                    placeholder="johndoe"
                    className="w-full pl-11"
                    disabled={isSubmitting}
                  />
                </div>
                {errors.username && (
                  <p className="mt-1 text-sm text-red-400">{errors.username.message}</p>
                )}
              </div>
              
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
              
              <div>
                <label htmlFor="confirmPassword" className="block text-sm font-medium text-surface-300 mb-2">
                  Confirm Password
                </label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-surface-500" />
                  <input
                    {...register('confirmPassword')}
                    type="password"
                    id="confirmPassword"
                    placeholder="••••••••"
                    className="w-full pl-11"
                    disabled={isSubmitting}
                  />
                </div>
                {errors.confirmPassword && (
                  <p className="mt-1 text-sm text-red-400">{errors.confirmPassword.message}</p>
                )}
              </div>
              
              <button
                type="submit"
                disabled={isSubmitting}
                className="btn-primary w-full py-3 mt-2"
              >
                {isSubmitting ? (
                  <LoadingSpinner size="sm" />
                ) : (
                  <>
                    <UserPlus className="w-5 h-5" />
                    Create Account
                  </>
                )}
              </button>
            </form>
            
            <p className="mt-6 text-center text-surface-400">
              Already have an account?{' '}
              <Link to="/login" className="text-primary-400 hover:text-primary-300 font-medium transition-colors">
                Sign in
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
