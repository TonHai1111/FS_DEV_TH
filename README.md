# TaskFlow - Todo Task Management Application

A full-stack task management application built with .NET Core 8 and React TypeScript. Features JWT authentication, drag-and-drop task management, categories, priorities, due dates, and a modern responsive UI.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Features](#features)
- [Project Structure](#project-structure)
- [Setup Instructions](#setup-instructions)
- [API Documentation](#api-documentation)
- [Data Model](#data-model)
- [Trade-offs and Assumptions](#trade-offs-and-assumptions)
- [Future Improvements](#future-improvements)

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Frontend (React)                        │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐       │
│  │  Pages  │  │Components│  │ Hooks   │  │ Services │       │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘       │
│       │            │            │            │              │
│       └────────────┴────────────┴────────────┘              │
│                         │                                   │
│                    Axios Client                             │
└─────────────────────────┬───────────────────────────────────┘
                          │ HTTP/JSON
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                   Backend (.NET Core 8)                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ Controllers │──│  Services   │──│   EF Core   │        │
│  └──────┬──────┘  └─────────────┘  └──────┬──────┘        │
│         │                                  │                │
│  ┌──────┴──────┐              ┌───────────┴───────────┐   │
│  │ JWT Auth    │              │      SQLite DB        │   │
│  │ Middleware  │              │  (todo.db file)       │   │
│  └─────────────┘              └───────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Tech Stack

### Backend
- **.NET 8 Web API** - Modern, cross-platform web framework
- **Entity Framework Core 8** - ORM for database operations
- **SQLite** - Lightweight, file-based database (easy to swap for SQL Server/PostgreSQL)
- **JWT Bearer Authentication** - Stateless, secure authentication
- **BCrypt.Net** - Password hashing
- **FluentValidation** - Request validation
- **Swagger/OpenAPI** - API documentation

### Frontend
- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Fast build tool
- **Tailwind CSS** - Utility-first styling
- **React Query** (`@tanstack/react-query`) - Server state management with caching
- **React Router v6** - Client-side routing
- **Axios** - HTTP client with interceptors
- **dnd-kit** - Accessible drag-and-drop
- **React Hook Form + Zod** - Form handling and validation
- **date-fns** - Date manipulation utilities
- **Lucide React** - Icon library
- **React Hot Toast** - Notifications

## Features

### Production MVP Features

1. **Authentication & Security**
   - User registration and login
   - JWT tokens with refresh mechanism
   - Secure password hashing (BCrypt)
   - Protected API endpoints
   - CORS configuration

2. **Task Management**
   - Create, read, update, delete tasks
   - Status workflow: Todo → In Progress → Done
   - Priority levels: Low, Medium, High
   - Due dates with overdue highlighting
   - Drag-and-drop status changes
   - Task search and filtering

3. **Categories**
   - Create custom categories with colors
   - Assign tasks to categories
   - Filter tasks by category
   - Default categories on registration

4. **User Experience**
   - Modern, responsive design
   - Kanban-style board layout
   - Loading states and error handling
   - Toast notifications
   - Client and server-side validation

5. **API Best Practices**
   - RESTful design
   - Request/Response DTOs
   - Global exception handling
   - Swagger documentation
   - Consistent response format

## Project Structure

```
FS_DEV_TH/
├── backend/
│   ├── TodoApi/
│   │   ├── Controllers/          # API endpoints
│   │   │   ├── AuthController.cs
│   │   │   ├── TasksController.cs
│   │   │   └── CategoriesController.cs
│   │   ├── Data/
│   │   │   └── AppDbContext.cs   # EF Core context
│   │   ├── Middleware/
│   │   │   └── ExceptionMiddleware.cs
│   │   ├── Migrations/           # EF Core migrations
│   │   ├── Models/
│   │   │   ├── User.cs
│   │   │   ├── TodoTask.cs
│   │   │   ├── Category.cs
│   │   │   └── DTOs/             # Data transfer objects
│   │   │       ├── ApiResponse.cs
│   │   │       ├── AuthDTOs.cs
│   │   │       ├── CategoryDTOs.cs
│   │   │       └── TaskDTOs.cs
│   │   ├── Services/             # Business logic layer
│   │   │   ├── IAuthService.cs
│   │   │   ├── AuthService.cs
│   │   │   ├── ITokenService.cs
│   │   │   ├── TokenService.cs
│   │   │   ├── ITaskService.cs
│   │   │   ├── TaskService.cs
│   │   │   ├── ICategoryService.cs
│   │   │   └── CategoryService.cs
│   │   ├── Program.cs            # App configuration
│   │   ├── appsettings.json
│   │   └── TodoApi.csproj
│   │
│   └── TodoApi.Tests/            # Unit tests (xUnit + Moq)
│       ├── Controllers/
│       │   ├── AuthControllerTests.cs
│       │   ├── TasksControllerTests.cs
│       │   └── CategoriesControllerTests.cs
│       ├── Services/
│       │   ├── AuthServiceTests.cs
│       │   ├── TaskServiceTests.cs
│       │   ├── CategoryServiceTests.cs
│       │   └── TokenServiceTests.cs
│       └── TodoApi.Tests.csproj
│
├── frontend/
│   └── todo-app/
│       ├── src/
│       │   ├── components/       # React components
│       │   │   ├── Header.tsx
│       │   │   ├── LoadingSpinner.tsx
│       │   │   ├── Sidebar.tsx
│       │   │   ├── TaskBoard.tsx
│       │   │   ├── TaskCard.tsx
│       │   │   ├── TaskColumn.tsx
│       │   │   └── TaskModal.tsx
│       │   ├── context/          # Auth context
│       │   │   └── AuthContext.tsx
│       │   ├── hooks/            # Custom hooks
│       │   │   └── useTasks.ts   # React Query hooks
│       │   ├── pages/            # Page components
│       │   │   ├── Dashboard.tsx
│       │   │   ├── LoginPage.tsx
│       │   │   └── RegisterPage.tsx
│       │   ├── services/         # API services
│       │   │   └── api.ts
│       │   └── types/            # TypeScript types
│       │       └── index.ts
│       ├── package.json
│       └── vite.config.ts
│
└── README.md
```

## Setup Instructions

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- npm or yarn

### Backend Setup

```bash
# Navigate to backend directory
cd backend/TodoApi

# Restore packages
dotnet restore

# Run the API (default: http://localhost:5000)
dotnet run

# Or run with hot reload
dotnet watch run
```

The API will be available at `http://localhost:5000` with Swagger at `/swagger`.

#### Testing the API

You can test the API using Swagger UI or cURL:

**1. Register a test account:**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Password123!"
  }'
```

**2. Login to get JWT token:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!"
  }'
```

Response will include `accessToken` and `refreshToken`. Use the `accessToken` for authenticated requests.

**3. Create a task (with authorization):**
```bash
curl -X POST http://localhost:5000/api/tasks \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "title": "My first task",
    "description": "This is a test task",
    "priority": 1,
    "dueDate": "2024-12-31T23:59:59Z"
  }'
```

Priority values: `0` = Low, `1` = Medium, `2` = High

### Frontend Setup

```bash
# Navigate to frontend directory
cd frontend/todo-app

# Install dependencies
npm install

# Start development server (default: http://localhost:5173)
npm run dev

# Build for production
npm run build
```

#### Using the Application

**1. Register a new account:**
- Open http://localhost:5173 in your browser
- Click "Register" or navigate to `/register`
- Fill in username, email, and password
- Click "Create Account"
- You'll be automatically logged in and redirected to the dashboard

**2. Login with existing account:**
- Navigate to http://localhost:5173/login
- Enter your email and password
- Click "Sign In"

**3. Create a new task:**
- Once logged in, click the "+ Add Task" button in any column (Todo, In Progress, Done)
- Fill in the task details:
  - **Title** (required): Name of your task
  - **Description** (optional): Additional details
  - **Priority**: Low, Medium, or High
  - **Due Date** (optional): When the task is due
  - **Category** (optional): Select from your categories
- Click "Create Task"

**4. Manage tasks:**
- **Drag and drop** tasks between columns to change status
- **Click** on a task to view/edit details
- **Delete** tasks using the delete button on the task card
- **Filter** tasks by category using the sidebar
- **Search** tasks using the search bar

### Configuration

**Backend** (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=todo.db"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "TodoApi",
    "Audience": "TodoApp",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  }
}
```

**Frontend**: The Vite dev server proxies `/api` requests to `http://localhost:5000`.

## API Documentation

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | Login and get JWT |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | Invalidate refresh token |
| GET | `/api/auth/me` | Get current user info |

### Tasks

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks` | Get all tasks (with filtering) |
| GET | `/api/tasks/{id}` | Get task by ID |
| POST | `/api/tasks` | Create new task |
| PUT | `/api/tasks/{id}` | Update task |
| PATCH | `/api/tasks/{id}/status` | Update task status |
| DELETE | `/api/tasks/{id}` | Delete task |
| GET | `/api/tasks/stats` | Get task statistics |

**Query Parameters** (GET /api/tasks):
- `status` (0=Todo, 1=InProgress, 2=Done)
- `priority` (0=Low, 1=Medium, 2=High)
- `categoryId`
- `search`
- `overdue` (boolean)
- `sortBy` (createdAt, dueDate, priority, title)
- `sortDescending` (boolean)

### Categories

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` | Get all categories |
| GET | `/api/categories/{id}` | Get category by ID |
| POST | `/api/categories` | Create category |
| PUT | `/api/categories/{id}` | Update category |
| DELETE | `/api/categories/{id}` | Delete category |

## Data Model

### User
```
User
├── Id (PK)
├── Username (unique)
├── Email (unique)
├── PasswordHash
├── CreatedAt
├── RefreshToken
└── RefreshTokenExpiryTime
```

### TodoTask
```
TodoTask
├── Id (PK)
├── Title
├── Description
├── Status (enum: Todo, InProgress, Done)
├── Priority (enum: Low, Medium, High)
├── DueDate
├── CreatedAt
├── UpdatedAt
├── UserId (FK → User)
└── CategoryId (FK → Category, nullable)
```

### Category
```
Category
├── Id (PK)
├── Name
├── Color (hex code)
└── UserId (FK → User)
```

## Trade-offs and Assumptions

### Design Decisions

| Decision | Trade-off | Rationale |
|----------|-----------|-----------|
| **SQLite** | Less scalable than SQL Server/PostgreSQL | Zero configuration, file-based, easy to migrate later via EF Core |
| **JWT Authentication** | Requires refresh token logic | Stateless, better for API-first design, no server-side sessions |
| **Single API Project** | Less separation of concerns | Appropriate for MVP scope, faster development |
| **Kanban UI** | Opinionated user experience | Intuitive for task status management, common pattern |
| **No pagination** | May slow with many tasks | MVP assumption: manageable task counts per user |

### Assumptions

1. **Single-tenant**: Each user sees only their own tasks and categories
2. **No team collaboration**: Tasks are not shared between users
3. **Moderate scale**: Designed for individual productivity, not enterprise
4. **Modern browsers**: No IE11 support
5. **Secure deployment**: HTTPS in production, secure JWT secret

### Security Considerations

- Passwords hashed with BCrypt (12 rounds)
- JWT tokens with short expiration (1 hour)
- Refresh tokens for extended sessions
- CORS restricted to allowed origins
- Input validation on client and server
- User-scoped data access (no cross-user data leaks)

## Future Improvements

### Short-term
- [ ] Email verification on registration
- [ ] Password reset functionality
- [ ] Task search with full-text search
- [ ] Pagination for task lists
- [ ] Bulk task operations
- [ ] Keyboard shortcuts

### Medium-term
- [ ] Task attachments/files
- [ ] Task comments
- [ ] Recurring tasks
- [ ] Task templates
- [ ] Export tasks (CSV, PDF)
- [ ] Dark/light theme toggle
- [ ] Mobile app (React Native)

### Long-term (Enterprise)
- [ ] Team workspaces
- [ ] Task assignment
- [ ] Role-based permissions
- [ ] Audit logging
- [ ] Webhooks/integrations
- [ ] Real-time updates (SignalR)
- [ ] Analytics dashboard
- [ ] Multi-language support

### Infrastructure
- [ ] Docker containerization
- [ ] CI/CD pipeline
- [ ] Health checks endpoint
- [ ] Rate limiting
- [ ] Caching (Redis)
- [ ] Database migration to PostgreSQL/SQL Server

## Development Notes

### Running Tests (Backend)
```bash
cd backend/TodoApi.Tests
dotnet test

# Run a specific test
dotnet test --filter "FullyQualifiedName~TaskServiceTests.GetTasksAsync_ReturnsUserTasks"
```

### Database Migrations
The application uses EF Core's `EnsureCreated()` for simplicity. For production:

```bash
# Create migration
dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update
```

### Environment Variables

For production, set these environment variables:
- `ConnectionStrings__DefaultConnection` - Database connection string
- `JwtSettings__SecretKey` - Strong, unique secret (32+ characters)
- `CorsSettings__AllowedOrigins` - Production frontend URL

---

Built with care for the FS_DEV_TH assessment.
