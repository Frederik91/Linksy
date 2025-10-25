# Feature Specification: Project Initialization with .NET Web API, Aspire, React + Vite + shadcn/ui

**Feature Branch**: `002-project-setup`  
**Created**: October 25, 2025  
**Status**: Draft  
**Input**: User description: "Initialize the project, set up the dotnet web api, aspire (use template from dotnet, this should be the first step, will create most of the boilerplate), then set up the react project with vite and chadcn and add this to aspire."

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Developer can initialize a complete full-stack project environment (Priority: P1)

Developers need to quickly bootstrap a new Linksy project with a modern full-stack architecture using .NET 8 for the backend API and React for the frontend, integrated through Aspire for orchestration and local development.

**Why this priority**: This is the foundational setup that enables all subsequent development. Without this, the team cannot begin building features.

**Independent Test**: Can be tested by running the complete initialization script/process and verifying that both the API server and React frontend start successfully in local development mode with Aspire orchestration.

**Acceptance Scenarios**:

1. **Given** an empty repository with initial git structure, **When** a developer executes the project initialization process, **Then** a complete project structure is created with .NET Web API and React frontend ready for development
2. **Given** a newly initialized project, **When** a developer starts the Aspire host, **Then** both the .NET API and React frontend are running and accessible without additional manual configuration
3. **Given** the initialized project, **When** a developer makes changes to backend or frontend code, **Then** hot reload/fast refresh works correctly for rapid iteration

---

### User Story 2 - Backend developer can develop against a fully functional .NET 8 Web API with Aspire (Priority: P1)

Backend developers need a professional .NET 8 Web API template with Aspire configured for service orchestration, debugging, and local development workflows.

**Why this priority**: The API is the core backend service and must be properly initialized to support feature development and integration with the frontend.

**Independent Test**: Can be tested by running the .NET API in isolation, making HTTP requests to health check endpoints, and verifying API responsiveness and integration with Aspire's service discovery.

**Acceptance Scenarios**:

1. **Given** the .NET API project is initialized with Aspire, **When** the API starts, **Then** it registers itself with the Aspire service host and is discoverable
2. **Given** the API is running, **When** a developer calls the health check endpoint, **Then** the API responds successfully with 200 OK
3. **Given** the .NET project, **When** a developer reviews the code structure, **Then** common best practices are in place (dependency injection, logging, configuration management, etc.)

---

### User Story 3 - Frontend developer can develop a modern React UI with component library and build tooling (Priority: P1)

Frontend developers need a React project built with Vite for fast development experience and shadcn/ui for a comprehensive component library with pre-built, customizable components.

**Why this priority**: The React frontend is essential for user-facing features and must have a performant developer experience and professional component system.

**Independent Test**: Can be tested by running the React development server, verifying hot module replacement works, loading shadcn/ui components in the browser, and building for production.

**Acceptance Scenarios**:

1. **Given** the React project is initialized with Vite, **When** the dev server starts, **Then** it launches quickly and hot module replacement works for both component and style changes
2. **Given** shadcn/ui is configured, **When** a developer imports and uses a shadcn component (e.g., Button, Card), **Then** it renders correctly with full styling and functionality
3. **Given** the React project, **When** building for production, **Then** the build succeeds and generates optimized bundles

---

### User Story 4 - DevOps/Local Development Lead can manage full application lifecycle through Aspire (Priority: P2)

Project leads and DevOps engineers need Aspire configured to orchestrate both backend and frontend services, providing unified logging, service discovery, and local development environment management.

**Why this priority**: While essential for professional development workflows, this can be completed after the individual services are working, as it primarily improves the development experience rather than blocking core functionality.

**Independent Test**: Can be tested by starting the Aspire dashboard, verifying both services are registered, checking unified logging, and simulating service failures/restarts.

**Acceptance Scenarios**:

1. **Given** Aspire host is configured with both API and React frontend, **When** starting via Aspire, **Then** both services register with service discovery and are visible in the Aspire dashboard
2. **Given** both services are running in Aspire, **When** checking the dashboard, **Then** unified logs from both services are displayed and filterable
3. **Given** the Aspire configuration, **When** documentation is reviewed, **Then** it explains how to add new services and manage local development

---

### User Story 5 - Team member can quickly onboard to the project environment (Priority: P2)

New team members need clear documentation and a straightforward process to get the development environment running locally with minimal configuration.

**Why this priority**: Improves team velocity and reduces onboarding friction, but doesn't block initial development.

**Independent Test**: Can be tested by following the documentation as a new user and successfully running the project without external help.

**Acceptance Scenarios**:

1. **Given** the project repository and README, **When** a new developer follows the setup instructions, **Then** they can get the full project running locally within 15 minutes
2. **Given** the setup instructions, **When** a developer encounters an issue, **Then** the README includes troubleshooting section for common problems
3. **Given** the initialized project, **When** code is committed, **Then** the repository includes essential .gitignore entries to prevent committing build artifacts and node_modules

### Edge Cases

- What happens if a developer clones the project but doesn't have .NET 8 SDK installed? (System should fail with clear message directing them to install prerequisites)
- What happens if Node.js/npm is not installed for the React project? (Similar clear error messaging)
- What happens if port 5000 (API) or 5173 (React dev server) is already in use? (Developer must manually free the port or update AppHost/vite.config.ts to use different ports)
- What happens if a developer modifies package.json or .csproj dependencies during development? (Dependencies should be recoverable and clearly documented)

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST initialize the project using the Aspire Empty App template via `dotnet new aspire` (from Aspire.ProjectTemplates). This command scaffolds the AppHost, a .NET API service, and ServiceDefaults—all foundational Aspire components without extraneous frameworks. This setup provides the base orchestration layer and API skeleton.
- **FR-002**: System MUST configure Aspire as the orchestration host that manages both the API and frontend services during local development. The Aspire AppHost will use `AddProject` for the .NET API and `AddNpmApp` for the React frontend, establishing service references and injecting environment variables (e.g., `VITE_API_URL`) for cross-service communication.
- **FR-003**: System MUST initialize a React project using Vite as the build tool and module bundler. The React app runs as a Node process managed by Aspire's `AddNpmApp`, receiving injected environment variables for API endpoint discovery.
- **FR-004**: System MUST install and configure shadcn/ui component library in the React project with full styling support
- **FR-005**: System MUST configure the .NET API to register with Aspire's service discovery so the frontend can discover and communicate with it via environment variables injected by the AppHost
- **FR-006**: System MUST configure React development server to work seamlessly within Aspire orchestration by accepting injected environment variables (VITE_API_URL, etc.) and reading them at build/runtime to communicate with the API service
- **FR-007**: System MUST include Hot Module Replacement (HMR) for React development with fast refresh capabilities
- **FR-008**: System MUST include proper project structure with conventional folders (src, models, components, services, etc.)
- **FR-009**: System MUST configure appropriate build configurations for both development and production environments
- **FR-010**: System MUST include essential tooling configuration files (.gitignore, environment variable templates, build scripts) and automated setup scripts (setup.sh for macOS/Linux, setup.ps1 for Windows) that verify prerequisites, restore dependencies, and display launch instructions
- **FR-011**: System MUST include comprehensive README documentation with setup instructions, prerequisites, and local development workflow

### Key Entities *(include if feature involves data)*

- **.NET Web API Service**: A .NET 8 service that provides backend REST API functionality, discoverable through Aspire
- **React Frontend Application**: A Vite-based React application with shadcn/ui components and styling
- **Aspire Orchestration Host**: Central service coordinator that manages service registration, discovery, logging, and local development environment
- **Development Environment**: The local developer machine with configured services, hot reload capabilities, and debugging support

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can clone the repository and have a fully running application (API + Frontend) within 15 minutes following setup documentation
- **SC-002**: Both the .NET API and React frontend respond to requests and display appropriate status/health indicators within 5 seconds of starting Aspire
- **SC-003**: Code changes in either backend or frontend are reflected in the running application within 3 seconds (hot reload/HMR)
- **SC-004**: The Aspire dashboard displays both services with proper health status and accessible logs from both services
- **SC-005**: 100% of project structure follows industry best practices for .NET 8 and React + Vite projects as defined in official templates
- **SC-006**: README documentation includes prerequisites, setup steps, and troubleshooting that enables a new team member to successfully set up the environment without assistance
- **SC-007**: The repository .gitignore is configured to exclude build artifacts, dependencies, and environment-specific files, reducing accidental commits of non-source files by 100%

## Clarifications

### Session 2025-10-25

- Q: Which Aspire template should be used for FR-001 scaffolding? → A: `dotnet new aspire` (Aspire Empty App from Aspire.ProjectTemplates). This template creates AppHost, API service, and ServiceDefaults without extraneous UI frameworks, providing a clean foundation for adding React.
- Q: How should React integrate with Aspire orchestration? → A: React runs as a Node process via `AddNpmApp` in the Aspire AppHost. Aspire injects environment variables (e.g., `VITE_API_URL`) to allow React to discover and communicate with the .NET API. Both services appear unified in the Aspire dashboard with unified logging and tracing.
- Q: What port strategy for local dev (API & React dev server)? → A: Fixed ports for local development. Choose conventional defaults (e.g., API on 5000, React dev server on 5173) with no fallback/retry logic if ports are already in use; developers must free the port or adjust configuration manually.
- Q: How should developers initialize the project after cloning? → A: Automated setup script (setup.sh for macOS/Linux, setup.ps1 for Windows) that verifies prerequisites (.NET 8 SDK, Node.js 22 LTS), restores .NET dependencies (`dotnet restore`), installs npm packages (`npm install`), and displays launch instructions for running Aspire AppHost.
- Q: Production build & deployment strategy? → A: React and .NET API are deployed separately. React builds to static files (dist folder) deployed to CDN or static hosting; .NET API deployed independently to cloud/container platform. Local Aspire development is focused on integrated dev experience; production architecture is deferred to deployment/infrastructure spec.
- **Enforcement Decision (2025-10-25)**: React frontend MUST be TypeScript-only (no JavaScript allowed). All source files in `frontend/src/` MUST have `.tsx` or `.ts` extensions. Test files MUST be `.test.tsx` or `.test.ts`. This ensures type safety and consistency across the codebase. Node.js 22 LTS is the minimum required version for all frontend development.
 - **Enforcement Decision (2025-10-25)**: React frontend MUST be TypeScript-only (no JavaScript allowed). All source files in `frontend/src/` MUST have `.tsx` or `.ts` extensions. Test files MUST be `.test.tsx` or `.test.ts`. This ensures type safety and consistency across the codebase. Node.js 22 LTS is the minimum required version for all frontend development. React 19+ is the required React major version for the frontend, and Tailwind CSS 4+ is the required styling framework version; configuration should follow shadcn/ui guidance for Tailwind 4+.

## Assumptions

- .NET 8 SDK will be used as the target framework (latest stable at time of specification)
- Node.js 22 LTS is the required runtime for React frontend development (mandatory, not 18+)
- React frontend MUST be TypeScript-only; no JavaScript (.js) files allowed in src/ directory
- npm or yarn are expected to be installed by developers (standard with Node.js)
- Local development will use Aspire for orchestration (not Docker Compose for local development, though Docker may be used later for deployment)
- shadcn/ui will use React's default styling system (CSS/Tailwind as appropriate for the template)
- The API will expose HTTP endpoints (not gRPC as primary protocol) for frontend consumption
- Developers will use VS Code or Visual Studio for development (common IDEs with Aspire support)
- Basic networking and service discovery will use local DNS/service names configured by Aspire
- Production deployment is out of scope for this feature; React and .NET API are deployed separately (React to CDN/static hosting, API to cloud/container platform) per infrastructure/deployment spec
