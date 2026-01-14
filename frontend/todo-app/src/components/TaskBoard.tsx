import { Task, TaskStatus, getStatusLabel } from '../types';
import TaskCard from './TaskCard';
import TaskColumn from './TaskColumn';
import {
  DndContext,
  DragOverlay,
  closestCorners,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragStartEvent,
  DragEndEvent,
} from '@dnd-kit/core';
import { useState } from 'react';

interface TaskBoardProps {
  tasksByStatus: {
    [TaskStatus.Todo]: Task[];
    [TaskStatus.InProgress]: Task[];
    [TaskStatus.Done]: Task[];
  };
  onTaskClick: (task: Task) => void;
  onStatusChange: (taskId: number, newStatus: TaskStatus) => Promise<void>;
  onDeleteTask: (taskId: number) => Promise<void>;
}

export default function TaskBoard({
  tasksByStatus,
  onTaskClick,
  onStatusChange,
  onDeleteTask,
}: TaskBoardProps) {
  const [activeTask, setActiveTask] = useState<Task | null>(null);
  
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
      },
    }),
    useSensor(KeyboardSensor)
  );
  
  const columns = [
    {
      status: TaskStatus.Todo,
      title: getStatusLabel(TaskStatus.Todo),
      tasks: tasksByStatus[TaskStatus.Todo],
      color: 'surface',
    },
    {
      status: TaskStatus.InProgress,
      title: getStatusLabel(TaskStatus.InProgress),
      tasks: tasksByStatus[TaskStatus.InProgress],
      color: 'primary',
    },
    {
      status: TaskStatus.Done,
      title: getStatusLabel(TaskStatus.Done),
      tasks: tasksByStatus[TaskStatus.Done],
      color: 'emerald',
    },
  ];
  
  const handleDragStart = (event: DragStartEvent) => {
    const task = findTask(event.active.id as number);
    setActiveTask(task || null);
  };
  
  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    setActiveTask(null);
    
    if (!over) return;
    
    const taskId = active.id as number;
    const overId = over.id as string;
    
    // Determine new status from the drop target
    let newStatus: TaskStatus | null = null;
    
    if (overId.startsWith('column-')) {
      newStatus = parseInt(overId.replace('column-', '')) as TaskStatus;
    } else {
      // Dropped on another task, find which column it's in
      const targetTask = findTask(parseInt(overId));
      if (targetTask) {
        newStatus = targetTask.status;
      }
    }
    
    if (newStatus !== null) {
      const task = findTask(taskId);
      if (task && task.status !== newStatus) {
        await onStatusChange(taskId, newStatus);
      }
    }
  };
  
  const findTask = (id: number): Task | undefined => {
    for (const tasks of Object.values(tasksByStatus)) {
      const task = tasks.find((t) => t.id === id);
      if (task) return task;
    }
    return undefined;
  };
  
  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div className="h-full p-4 lg:p-6 overflow-x-auto">
        <div className="flex gap-4 lg:gap-6 h-full min-w-max lg:min-w-0">
          {columns.map((column) => (
            <TaskColumn
              key={column.status}
              id={`column-${column.status}`}
              title={column.title}
              color={column.color}
              count={column.tasks.length}
            >
              {column.tasks.map((task) => (
                <TaskCard
                  key={task.id}
                  task={task}
                  onClick={() => onTaskClick(task)}
                  onDelete={() => onDeleteTask(task.id)}
                />
              ))}
            </TaskColumn>
          ))}
        </div>
      </div>
      
      <DragOverlay>
        {activeTask && (
          <div className="opacity-80 rotate-3">
            <TaskCard
              task={activeTask}
              onClick={() => {}}
              onDelete={() => {}}
              isDragging
            />
          </div>
        )}
      </DragOverlay>
    </DndContext>
  );
}
