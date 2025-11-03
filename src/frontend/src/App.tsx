import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import Dashboard from './pages/Dashboard';
import Onboarding from './pages/Onboarding';

function Home() {
  const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';

  const handleFetchInfo = async () => {
    try {
      const response = await fetch(`${apiUrl}/api/info`);
      const data = await response.json();
      console.log('API Response:', data);
      alert(JSON.stringify(data, null, 2));
    } catch (error) {
      console.error('Error fetching API:', error);
      alert(`Error: ${error}`);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 to-slate-800">
      <header className="bg-slate-900/50 backdrop-blur-sm border-b border-slate-700">
        <div className="max-w-7xl mx-auto px-4 py-6">
          <h1 className="text-3xl font-bold text-white">Linksy</h1>
          <p className="text-slate-400 mt-1">AEC File Synchronization Platform</p>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 py-12">
        <div className="bg-slate-800 rounded-lg shadow-xl p-8 border border-slate-700">
          <div className="space-y-6">
            <div>
              <h2 className="text-2xl font-bold text-white mb-2">Welcome to Linksy</h2>
              <p className="text-slate-300">
                Seamlessly synchronize files between Autodesk Construction Cloud and SharePoint.
              </p>
            </div>

            <div className="bg-slate-900/50 rounded p-4 border border-slate-600">
              <h3 className="text-lg font-semibold text-slate-200 mb-2">API Configuration</h3>
              <p className="text-sm text-slate-400">
                API URL: <code className="bg-slate-950 px-2 py-1 rounded text-slate-300">{apiUrl}</code>
              </p>
            </div>

            <div className="flex gap-4">
              <Link to="/onboarding">
                <Button className="bg-blue-600 hover:bg-blue-700">
                  Get Started
                </Button>
              </Link>
              <Link to="/dashboard">
                <Button variant="outline" className="border-slate-600 text-slate-300 hover:bg-slate-700">
                  View Dashboard
                </Button>
              </Link>
              <Button onClick={handleFetchInfo} variant="outline" className="border-slate-600 text-slate-300 hover:bg-slate-700">
                Test API
              </Button>
            </div>
          </div>
        </div>

        <div className="mt-12 grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-slate-800 rounded-lg p-6 border border-slate-700">
            <div className="text-2xl font-bold text-blue-400 mb-2">⚙️</div>
            <h3 className="text-lg font-semibold text-white mb-2">Automated Sync</h3>
            <p className="text-slate-400 text-sm">
              Schedule automatic synchronization between ACC Docs and SharePoint.
            </p>
          </div>

          <div className="bg-slate-800 rounded-lg p-6 border border-slate-700">
            <div className="text-2xl font-bold text-green-400 mb-2">🚀</div>
            <h3 className="text-lg font-semibold text-white mb-2">Conflict Resolution</h3>
            <p className="text-slate-400 text-sm">
              Multiple policies to handle file conflicts intelligently.
            </p>
          </div>

          <div className="bg-slate-800 rounded-lg p-6 border border-slate-700">
            <div className="text-2xl font-bold text-purple-400 mb-2">🔗</div>
            <h3 className="text-lg font-semibold text-white mb-2">Audit Trail</h3>
            <p className="text-slate-400 text-sm">
              Complete audit logging for compliance and accountability.
            </p>
          </div>
        </div>
      </main>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/onboarding" element={<Onboarding />} />
        <Route path="/dashboard" element={<Dashboard />} />
      </Routes>
    </BrowserRouter>
  );
}
