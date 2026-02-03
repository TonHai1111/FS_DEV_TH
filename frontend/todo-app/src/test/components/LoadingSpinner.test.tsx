import { render } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import LoadingSpinner from '../../components/LoadingSpinner';

describe('LoadingSpinner', () => {
  it('should render with default medium size', () => {
    const { container } = render(<LoadingSpinner />);
    const spinner = container.firstChild as HTMLElement;

    expect(spinner).toBeInTheDocument();
    expect(spinner.className).toContain('w-8');
    expect(spinner.className).toContain('h-8');
  });

  it('should render small size when specified', () => {
    const { container } = render(<LoadingSpinner size="sm" />);
    const spinner = container.firstChild as HTMLElement;

    expect(spinner.className).toContain('w-4');
    expect(spinner.className).toContain('h-4');
  });

  it('should render large size when specified', () => {
    const { container } = render(<LoadingSpinner size="lg" />);
    const spinner = container.firstChild as HTMLElement;

    expect(spinner.className).toContain('w-12');
    expect(spinner.className).toContain('h-12');
  });

  it('should apply custom className', () => {
    const { container } = render(<LoadingSpinner className="custom-class" />);
    const spinner = container.firstChild as HTMLElement;

    expect(spinner.className).toContain('custom-class');
  });

  it('should have animation class', () => {
    const { container } = render(<LoadingSpinner />);
    const spinner = container.firstChild as HTMLElement;

    expect(spinner.className).toContain('animate-spin');
  });

  it('should have proper styling classes', () => {
    const { container } = render(<LoadingSpinner />);
    const spinner = container.firstChild as HTMLElement;

    expect(spinner.className).toContain('rounded-full');
    expect(spinner.className).toContain('border-primary-500');
    expect(spinner.className).toContain('border-t-transparent');
  });
});
