import { describe, it, expect } from 'vitest';
import {
  TaskStatus,
  TaskPriority,
  getStatusLabel,
  getPriorityLabel,
  getPriorityColor,
} from '../../types';

describe('getStatusLabel', () => {
  it('should return "To Do" for TaskStatus.Todo', () => {
    expect(getStatusLabel(TaskStatus.Todo)).toBe('To Do');
  });

  it('should return "In Progress" for TaskStatus.InProgress', () => {
    expect(getStatusLabel(TaskStatus.InProgress)).toBe('In Progress');
  });

  it('should return "Done" for TaskStatus.Done', () => {
    expect(getStatusLabel(TaskStatus.Done)).toBe('Done');
  });

  it('should return "Unknown" for invalid status', () => {
    expect(getStatusLabel(99 as TaskStatus)).toBe('Unknown');
  });
});

describe('getPriorityLabel', () => {
  it('should return "Low" for TaskPriority.Low', () => {
    expect(getPriorityLabel(TaskPriority.Low)).toBe('Low');
  });

  it('should return "Medium" for TaskPriority.Medium', () => {
    expect(getPriorityLabel(TaskPriority.Medium)).toBe('Medium');
  });

  it('should return "High" for TaskPriority.High', () => {
    expect(getPriorityLabel(TaskPriority.High)).toBe('High');
  });

  it('should return "Unknown" for invalid priority', () => {
    expect(getPriorityLabel(99 as TaskPriority)).toBe('Unknown');
  });
});

describe('getPriorityColor', () => {
  it('should return emerald classes for Low priority', () => {
    const color = getPriorityColor(TaskPriority.Low);
    expect(color).toContain('emerald');
  });

  it('should return amber classes for Medium priority', () => {
    const color = getPriorityColor(TaskPriority.Medium);
    expect(color).toContain('amber');
  });

  it('should return red classes for High priority', () => {
    const color = getPriorityColor(TaskPriority.High);
    expect(color).toContain('red');
  });

  it('should return surface classes for invalid priority', () => {
    const color = getPriorityColor(99 as TaskPriority);
    expect(color).toContain('surface');
  });
});
