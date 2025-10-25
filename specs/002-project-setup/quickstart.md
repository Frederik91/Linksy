# Quick Start Guide: Project Initialization with Aspire, .NET 8, React + Vite + shadcn/ui

**Date**: October 25, 2025  
**Audience**: Developers new to the Linksy project  
**Duration**: ~15 minutes from clone to running app

---

## Prerequisites

Before starting, ensure you have:

1. **.NET 8 SDK** or later
   ```bash
   dotnet --version
   ```
   If not installed: https://dotnet.microsoft.com/download/dotnet/8.0

2. **Node.js 18+** and npm
   ```bash
   node --version
   npm --version
   ```
   If not installed: https://nodejs.org/

3. **Git** (to clone the repository)

---

## Step 1: Clone the Repository

```bash
git clone https://github.com/Frederik91/Linksy.git
cd Linksy
```

---

## Step 2: Run the Setup Script

### macOS / Linux

```bash
./scripts/setup.sh
```

### Windows (PowerShell)

```powershell
.\scripts\setup.ps1
```

The script will:
- ✅ Verify .NET 8 SDK is installed
- ✅ Verify Node.js 18+ is installed
- ✅ Run `dotnet restore` to restore .NET dependencies
   - ✅ Run `npm install` in the src/frontend folder to install React dependencies
- ✅ Print launch instructions

---

## Step 3: Launch the Application

Once the setup script completes, run:

```bash
dotnet run --project Linksy.AppHost
```

This starts:
- **Aspire Dashboard** at http://localhost:18888
- **.NET API** at http://localhost:5000
- **React Dev Server** at http://localhost:5173

Wait for all services to appear in the Aspire dashboard with ✅ status (usually ~5 seconds).

---

## Step 4: Verify Everything Works

### Check the Dashboard
Open http://localhost:18888 in your browser and confirm:
- `api` service (green checkmark) ✅
- `frontend` service (green checkmark) ✅

### Check the Frontend
Open http://localhost:5173 in your browser. You should see:
- React app loaded
- No console errors
- Able to interact with UI components

### Check the API
Open http://localhost:5000/health in your browser or run:
```bash
curl http://localhost:5000/health
```
Expected response: `200 OK` with health data.

---

## Step 5: Start Developing

### Making Backend Changes
Edit files in `Linksy.Api/` folder. The .NET runtime will detect changes and recompile.

### Making Frontend Changes
Edit files in `src/frontend/src/` folder. Vite's Hot Module Replacement (HMR) will refresh the browser automatically within 3 seconds.

### Accessing the API from React
The React frontend automatically receives `VITE_API_URL` environment variable pointing to the API. Use it like:

```typescript
const apiUrl = import.meta.env.VITE_API_URL;
const response = await fetch(`${apiUrl}/endpoint`);
```

---

## Troubleshooting

### Port Already in Use
If port 5000 (API) or 5173 (React) is already in use:

**Option A**: Free the port (find process and kill it)
```bash
# macOS/Linux (find process on port 5000)
lsof -i :5000
kill -9 <PID>
```

**Option B**: Change the port in configuration
- **API**: Edit `Linksy.AppHost/Program.cs` and change the port binding
- **React**: Edit `src/frontend/vite.config.ts` and change `server.port`

### Setup Script Fails with "Node.js not found"
Install Node.js 18+ from https://nodejs.org/ and rerun the setup script.

### Setup Script Fails with ".NET SDK not found"
Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 and rerun the setup script.

### React App Shows "Failed to fetch"
Ensure the `.NET API` is running (check Aspire dashboard). If API crashed:
```bash
# Stop Aspire (Ctrl+C)
# Check API logs in Aspire dashboard
# Restart Aspire: dotnet run --project Linksy.AppHost
```

### HMR Not Working
- Ensure you're running the app via Aspire (`dotnet run --project Linksy.AppHost`), not manually starting the dev server.
- Check browser console for errors; refresh page manually if needed.

---

## Next Steps

1. **Backend API**: Explore the `.NET 8 Minimal API` in `Linksy.Api/Program.cs`. Add your first endpoint.
2. **Frontend UI**: Import shadcn/ui components in React. See `src/frontend/src/components/`.
3. **Database** (future): Phase 2 will add PostgreSQL for data persistence.

---

## Useful Commands

### Stop All Services
Press `Ctrl+C` in the terminal running Aspire.

### View Logs
Open Aspire Dashboard at http://localhost:18888 and click on each service for logs.

### Rebuild Everything
```bash
dotnet clean
dotnet restore
npm install --prefix ./src/frontend
dotnet run --project Linksy.AppHost
```

### Production Build (Frontend Only)
```bash
npm run build --prefix ./src/frontend
```
Output is in `src/frontend/dist/` and ready to deploy to a CDN.

---

## Architecture Overview

```
┌─────────────────────────────────────────┐
│        Aspire AppHost                   │
│   (Orchestrates all services)           │
├─────────────────────────────────────────┤
│                                         │
│  ┌──────────────┐   ┌──────────────┐  │
│  │ .NET 8 API   │   │ React App    │  │
│  │ :5000        │◄──┤ :5173        │  │
│  │              │   │              │  │
│  │ - REST       │   │ - TypeScript │  │
│  │ - Minimal API│   │ - Vite       │  │
│  │              │   │ - shadcn/ui  │  │
│  └──────────────┘   └──────────────┘  │
│                                         │
│  ┌──────────────────────────────────┐  │
│  │ Unified Dashboard & Logging      │  │
│  │ :18888                           │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

---

## Need Help?

1. Check the **Troubleshooting** section above
2. Review the **README.md** in the root for more detailed setup info
3. Open an issue on GitHub with error logs and steps to reproduce

Happy coding! 🚀
