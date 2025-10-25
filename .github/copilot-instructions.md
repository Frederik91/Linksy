# Linksy Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-10-25

## Active Technologies

### Backend
- **.NET 9.0** (SDK & Target Framework)
- **ASP.NET Core Minimal API** - Lightweight REST endpoint pattern
- **Aspire 9.0.0** - Service orchestration for local development
- **xUnit 2.9.3** - Unit testing framework
- **Swagger/OpenAPI 9.0.0** - API documentation

### Frontend  
- **React 19.0+** - UI framework (TypeScript only - no JavaScript files)
- **TypeScript 5.3+** - Strict mode required (`tsconfig.json`)
- **Vite 5.0+** - Modern bundler with HMR
- **Tailwind CSS 4.0+** - Utility-first CSS framework
- **shadcn/ui** - Component library (Radix UI + Tailwind)
- **Vitest 1.0+** - Unit testing framework
- **React Testing Library 15.0+** - Component testing utilities

### Infrastructure
- **Node.js 22 LTS** - Required (enforced in `scripts/setup.sh`, `package.json` engine field, `.nvmrc`)
- **npm** - Package manager for frontend dependencies

## Project Structure

```text
Linksy.sln                    # .NET solution file
Linksy.ServiceDefaults/       # Shared .NET defaults
Linksy.Api/                   # REST API service (Minimal API pattern)
Linksy.Api.Tests/             # Backend unit tests (xUnit)
Linksy.AppHost/               # Aspire orchestration (frontend + backend)
frontend/                     # React application (TypeScript only)
  src/
    App.tsx                   # Root component
    main.tsx                  # Entry point
    index.css                 # Tailwind + global styles
    components/ui/            # shadcn/ui components
    lib/                      # Utilities (cn helper, etc.)
  public/                     # Static assets
  tests/                      # Frontend unit tests (Vitest)
scripts/
  setup.sh                    # macOS/Linux setup automation
  setup.ps1                   # Windows PowerShell setup
  verify-prereqs.sh           # CI prerequisite validation
docs/
  idea.md
  react-19.md                 # React 19 migration notes
  tailwindcss-v4.md           # Tailwind CSS v4 migration
specs/002-project-setup/      # Feature specification & tasks
```

## Key Commands

### Setup
```bash
./scripts/setup.sh            # macOS/Linux: Install dependencies
./scripts/setup.ps1           # Windows: Install dependencies
```

### Development
```bash
dotnet build Linksy.sln       # Build .NET projects
dotnet run --project Linksy.AppHost  # Start Aspire (orchestrates API + frontend)
npm run dev --prefix ./frontend      # Frontend dev server (port 5173)
```

### Testing
```bash
dotnet test Linksy.Api.Tests         # Backend unit tests
npm run test --prefix ./frontend     # Frontend unit tests (Vitest)
```

### Linting & Formatting
```bash
npm run lint --prefix ./frontend     # ESLint
npm run preview --prefix ./frontend  # Vite preview
```

### Production Build
```bash
npm run build --prefix ./frontend    # Production React bundle
dotnet publish -c Release            # Publish API for production
```

## Technology Decisions

### Why .NET 9.0?
- Latest stable (Aspire 9.0.0 requires it, not 8.x)
- Minimal API pattern offers lean, modern REST endpoint definition
- Strong C# ecosystem for backend services

### Why React 19 + TypeScript?
- React 19 has automatic JSX transform, new hooks, and improved developer experience
- TypeScript strict mode enforced for type safety across entire frontend
- No JavaScript files allowed in `frontend/src/` (TypeScript-only enforcement)

### Why Aspire?
- Unified orchestration dashboard for local development (no container setup needed)
- Service discovery and health checks out-of-the-box
- Single point of environment configuration (VITE_API_URL injected for frontend)

### Why Tailwind CSS 4 + shadcn/ui?
- Modern utility-first approach with excellent TypeScript support
- shadcn/ui provides production-ready component system (Radix UI primitives)
- Dark mode support and design system consistency

### Why Vite?
- <3 second HMR for fast feedback loop
- Modern ES modules only (no legacy CommonJS overhead)
- Excellent TypeScript + React support out-of-the-box

## Development Workflow

### Local Development (Typical Flow)
1. Run `./scripts/setup.sh` once (installs .NET + npm dependencies)
2. Start Aspire: `dotnet run --project Linksy.AppHost`
3. Open http://localhost:15217 (Aspire Dashboard)
4. Frontend: http://localhost:5173
5. API: http://localhost:5000/health

### Adding New Components
- Use shadcn/ui for UI components
- Keep all frontend code in TypeScript (`.tsx` only)
- Add tests in `frontend/src/__tests__/` or alongside components as `.test.tsx`

### Adding New API Endpoints
- Use Minimal API pattern in `Linksy.Api/Program.cs`
- Pattern: `app.MapGet("/api/endpoint", () => {...}).WithName("Name").WithOpenApi();`
- Add unit tests in `Linksy.Api.Tests/`

## Enforcement Rules

1. **TypeScript-Only Frontend**: No `.js` or `.jsx` files in `frontend/src/`. Enforce via `find frontend/src -name "*.js" -o -name "*.jsx"` should return 0 files.

2. **Strict TypeScript Mode**: `frontend/tsconfig.json` must have `"strict": true`. All frontend code must pass `tsc --noEmit` with zero errors.

3. **Node.js 22 LTS**: 
   - Required version specified in `scripts/setup.sh`, `package.json` ("engines": {"node": "^22.0.0"}), and `.nvmrc`
   - Setup script validates and fails if Node.js < 22 detected

4. **CORS**: API CORS policy restricted to `http://localhost:5173` only (frontend dev server)

5. **Environment Variables**: 
   - Frontend: `VITE_API_URL` injected by Aspire, defaults to `http://localhost:5000`
   - Backend: No secrets in `appsettings.json` (use secrets.json for development)

6. **Git Ignore**: `.gitignore` properly excludes build artifacts (bin/, obj/, dist/, node_modules/) and environment files (.env)

## Recent Changes

- **002-project-setup**: Implemented complete project scaffold
  - .NET 9.0 + Aspire 9.0.0 orchestration
  - React 19 + TypeScript + Vite + shadcn/ui + Tailwind CSS 4
  - Node.js 22 LTS enforcement
  - Automated setup scripts
  - Comprehensive documentation and testing

## Reference Documents

- `docs/react-19.md` — React 19 migration notes and new APIs. Consult when generating React code for React 19 compatibility.
- `docs/tailwindcss-v4.md` — Tailwind CSS v4 migration guidance. Consult when producing Tailwind-related code or upgrading from v3.
- `README.md` — Comprehensive developer onboarding (prerequisites, setup, troubleshooting, useful commands)
- `specs/002-project-setup/spec.md` — Feature specification (user stories, requirements, success criteria)
- `specs/002-project-setup/plan.md` — Implementation plan (tech decisions, architecture)

