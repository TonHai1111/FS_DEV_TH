import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import ErrorBoundary from '../../components/ErrorBoundary';

// Component that throws an error based on a ref (allows changing behavior without remount)
let shouldThrowError = false;

const ThrowError = () => {
  if (shouldThrowError) {
    throw new Error('Test error message');
  }
  return <div>Child content</div>;
};

// Component that always throws
const AlwaysThrowError = () => {
  throw new Error('Test error message');
};

// Suppress console.error during tests
const originalError = console.error;
beforeEach(() => {
  console.error = vi.fn();
  shouldThrowError = false; // Reset the throw flag before each test
});

afterEach(() => {
  console.error = originalError;
});

describe('ErrorBoundary', () => {
  it('should render children when no error occurs', () => {
    render(
      <ErrorBoundary>
        <div>Child content</div>
      </ErrorBoundary>
    );

    expect(screen.getByText('Child content')).toBeInTheDocument();
  });

  it('should render error UI when an error occurs', () => {
    render(
      <ErrorBoundary>
        <AlwaysThrowError />
      </ErrorBoundary>
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    expect(
      screen.getByText('An unexpected error occurred. Please try again or refresh the page.')
    ).toBeInTheDocument();
  });

  it('should display error details when expanded', () => {
    render(
      <ErrorBoundary>
        <AlwaysThrowError />
      </ErrorBoundary>
    );

    // Find and click the details summary
    const details = screen.getByText('Error details');
    fireEvent.click(details);

    expect(screen.getByText('Test error message')).toBeInTheDocument();
  });

  it('should render custom fallback when provided', () => {
    const customFallback = <div>Custom error UI</div>;

    render(
      <ErrorBoundary fallback={customFallback}>
        <AlwaysThrowError />
      </ErrorBoundary>
    );

    expect(screen.getByText('Custom error UI')).toBeInTheDocument();
    expect(screen.queryByText('Something went wrong')).not.toBeInTheDocument();
  });

  it('should have Try Again button', () => {
    render(
      <ErrorBoundary>
        <AlwaysThrowError />
      </ErrorBoundary>
    );

    expect(screen.getByRole('button', { name: 'Try Again' })).toBeInTheDocument();
  });

  it('should have Refresh Page button', () => {
    render(
      <ErrorBoundary>
        <AlwaysThrowError />
      </ErrorBoundary>
    );

    expect(screen.getByRole('button', { name: 'Refresh Page' })).toBeInTheDocument();
  });

  it('should reset error state when Try Again is clicked', () => {
    // Start with error enabled
    shouldThrowError = true;

    render(
      <ErrorBoundary>
        <ThrowError />
      </ErrorBoundary>
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();

    // Disable throwing before clicking Try Again
    shouldThrowError = false;

    // Click Try Again - this resets the error boundary state and re-renders children
    fireEvent.click(screen.getByRole('button', { name: 'Try Again' }));

    // Now the component should render successfully
    expect(screen.getByText('Child content')).toBeInTheDocument();
  });

  it('should call window.location.reload when Refresh Page is clicked', () => {
    const reloadMock = vi.fn();
    Object.defineProperty(window, 'location', {
      value: { reload: reloadMock },
      writable: true,
    });

    render(
      <ErrorBoundary>
        <AlwaysThrowError />
      </ErrorBoundary>
    );

    fireEvent.click(screen.getByRole('button', { name: 'Refresh Page' }));

    expect(reloadMock).toHaveBeenCalledTimes(1);
  });

  it('should log error to console', () => {
    render(
      <ErrorBoundary>
        <AlwaysThrowError />
      </ErrorBoundary>
    );

    expect(console.error).toHaveBeenCalled();
  });
});
