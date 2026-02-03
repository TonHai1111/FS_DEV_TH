import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import TaskCard from '../../components/TaskCard';
import { Task, TaskStatus, TaskPriority } from '../../types';

// Mock dnd-kit
vi.mock('@dnd-kit/core', () => ({
  useDraggable: () => ({
    attributes: {},
    listeners: {},
    setNodeRef: vi.fn(),
    transform: null,
  }),
}));

const createMockTask = (overrides: Partial<Task> = {}): Task => ({
  id: 1,
  title: 'Test Task',
  description: 'Test description',
  status: TaskStatus.Todo,
  priority: TaskPriority.Medium,
  dueDate: null,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  categoryId: null,
  category: null,
  isOverdue: false,
  ...overrides,
});

describe('TaskCard', () => {
  const mockOnClick = vi.fn();
  const mockOnDelete = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render task title', () => {
    const task = createMockTask({ title: 'My Test Task' });
    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    expect(screen.getByText('My Test Task')).toBeInTheDocument();
  });

  it('should render task description when provided', () => {
    const task = createMockTask({ description: 'This is a description' });
    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    expect(screen.getByText('This is a description')).toBeInTheDocument();
  });

  it('should not render description when null', () => {
    const task = createMockTask({ description: null });
    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    expect(screen.queryByText('This is a description')).not.toBeInTheDocument();
  });

  it('should render priority label', () => {
    const task = createMockTask({ priority: TaskPriority.High });
    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    expect(screen.getByText('High')).toBeInTheDocument();
  });

  it('should render category when provided', () => {
    const task = createMockTask({
      category: { id: 1, name: 'Work', color: '#3B82F6', taskCount: 5 },
      categoryId: 1,
    });
    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    expect(screen.getByText('Work')).toBeInTheDocument();
  });

  it('should not render category when null', () => {
    const task = createMockTask({ category: null });
    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    expect(screen.queryByText('Work')).not.toBeInTheDocument();
  });

  it('should call onClick when card is clicked', () => {
    const task = createMockTask();
    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    fireEvent.click(screen.getByText('Test Task'));

    expect(mockOnClick).toHaveBeenCalledTimes(1);
  });

  it('should call onDelete when delete button is clicked', () => {
    const task = createMockTask();
    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    // Find the delete button by its role or by querying the container
    const deleteButton = document.querySelector('button[class*="hover:text-red"]');
    if (deleteButton) {
      fireEvent.click(deleteButton);
    }

    expect(mockOnDelete).toHaveBeenCalledTimes(1);
    expect(mockOnClick).not.toHaveBeenCalled(); // Should not trigger onClick
  });

  it('should render due date when provided', () => {
    // Set a future date to avoid "overdue" styling
    const futureDate = new Date();
    futureDate.setDate(futureDate.getDate() + 7);
    const task = createMockTask({ dueDate: futureDate.toISOString() });

    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    // Should show formatted date (e.g., "Jan 15")
    expect(document.querySelector('svg.lucide-calendar')).toBeInTheDocument();
  });

  it('should show "Today" for today\'s due date', () => {
    const today = new Date();
    today.setHours(23, 59, 59); // End of today
    const task = createMockTask({ dueDate: today.toISOString() });

    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    expect(screen.getByText('Today')).toBeInTheDocument();
  });

  it('should show "Tomorrow" for tomorrow\'s due date', () => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const task = createMockTask({ dueDate: tomorrow.toISOString() });

    render(<TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />);

    expect(screen.getByText('Tomorrow')).toBeInTheDocument();
  });

  it('should apply overdue styling for past due dates', () => {
    const pastDate = new Date();
    pastDate.setDate(pastDate.getDate() - 1);
    const task = createMockTask({
      dueDate: pastDate.toISOString(),
      status: TaskStatus.Todo, // Not completed
    });

    const { container } = render(
      <TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />
    );

    // Check for overdue styling (red border or background)
    const card = container.firstChild as HTMLElement;
    expect(card.className).toContain('border-red');
  });

  it('should not apply overdue styling for completed tasks', () => {
    const pastDate = new Date();
    pastDate.setDate(pastDate.getDate() - 1);
    const task = createMockTask({
      dueDate: pastDate.toISOString(),
      status: TaskStatus.Done, // Completed
    });

    const { container } = render(
      <TaskCard task={task} onClick={mockOnClick} onDelete={mockOnDelete} />
    );

    const card = container.firstChild as HTMLElement;
    expect(card.className).not.toContain('border-red');
  });

  it('should render different priority colors', () => {
    const lowTask = createMockTask({ priority: TaskPriority.Low });
    const { rerender } = render(
      <TaskCard task={lowTask} onClick={mockOnClick} onDelete={mockOnDelete} />
    );
    expect(screen.getByText('Low')).toBeInTheDocument();

    const mediumTask = createMockTask({ priority: TaskPriority.Medium });
    rerender(
      <TaskCard task={mediumTask} onClick={mockOnClick} onDelete={mockOnDelete} />
    );
    expect(screen.getByText('Medium')).toBeInTheDocument();

    const highTask = createMockTask({ priority: TaskPriority.High });
    rerender(
      <TaskCard task={highTask} onClick={mockOnClick} onDelete={mockOnDelete} />
    );
    expect(screen.getByText('High')).toBeInTheDocument();
  });
});
