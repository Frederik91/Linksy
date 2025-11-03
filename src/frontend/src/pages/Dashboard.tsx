import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { tenantApi, bindingApi, syncJobApi } from '../lib/api';
import type { Tenant, Binding, SyncJobStats } from '../types';

export default function Dashboard() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [selectedTenant, setSelectedTenant] = useState<Tenant | null>(null);
  const [bindings, setBindings] = useState<Binding[]>([]);
  const [stats, setStats] = useState<SyncJobStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadTenants();
  }, []);

  useEffect(() => {
    if (selectedTenant) {
      loadBindings(selectedTenant.id);
      loadStats(selectedTenant.id);
    }
  }, [selectedTenant]);

  const loadTenants = async () => {
    try {
      const data = await tenantApi.getAll();
      setTenants(data);
      if (data.length > 0) {
        setSelectedTenant(data[0]);
      }
    } catch (error) {
      console.error('Failed to load tenants:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadBindings = async (tenantId: string) => {
    try {
      const data = await bindingApi.getByTenant(tenantId);
      setBindings(data);
    } catch (error) {
      console.error('Failed to load bindings:', error);
    }
  };

  const loadStats = async (tenantId: string) => {
    try {
      const data = await syncJobApi.getStats(tenantId);
      setStats(data);
    } catch (error) {
      console.error('Failed to load stats:', error);
    }
  };

  const handleManualSync = async (bindingId: string) => {
    try {
      await syncJobApi.create({ bindingId, isManualTrigger: true });
      alert('Sync job started successfully!');
      if (selectedTenant) {
        loadStats(selectedTenant.id);
      }
    } catch (error) {
      console.error('Failed to start sync:', error);
      alert('Failed to start sync job');
    }
  };

  const handlePauseBinding = async (bindingId: string, isPaused: boolean) => {
    try {
      if (isPaused) {
        await bindingApi.resume(bindingId);
      } else {
        await bindingApi.pause(bindingId);
      }
      if (selectedTenant) {
        loadBindings(selectedTenant.id);
      }
    } catch (error) {
      console.error('Failed to toggle binding:', error);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-slate-400">Loading...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 to-slate-800">
      <header className="bg-slate-900/50 backdrop-blur-sm border-b border-slate-700">
        <div className="max-w-7xl mx-auto px-4 py-6">
          <h1 className="text-3xl font-bold text-white">Linksy Dashboard</h1>
          <p className="text-slate-400 mt-1">Monitor and manage your file synchronization</p>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 py-8">
        {/* Tenant Selector */}
        <div className="mb-8">
          <label className="block text-sm font-medium text-slate-300 mb-2">
            Select Tenant
          </label>
          <select
            className="bg-slate-800 text-white border border-slate-700 rounded-lg px-4 py-2"
            value={selectedTenant?.id || ''}
            onChange={(e) => {
              const tenant = tenants.find((t) => t.id === e.target.value);
              setSelectedTenant(tenant || null);
            }}
          >
            {tenants.map((tenant) => (
              <option key={tenant.id} value={tenant.id}>
                {tenant.name}
              </option>
            ))}
          </select>
        </div>

        {/* Stats Cards */}
        {stats && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
            <div className="bg-slate-800 rounded-lg p-6 border border-slate-700">
              <h3 className="text-sm font-medium text-slate-400 mb-2">Total Jobs</h3>
              <p className="text-3xl font-bold text-white">{stats.totalJobs}</p>
            </div>
            <div className="bg-slate-800 rounded-lg p-6 border border-slate-700">
              <h3 className="text-sm font-medium text-slate-400 mb-2">Running Jobs</h3>
              <p className="text-3xl font-bold text-blue-400">{stats.runningJobs}</p>
            </div>
            <div className="bg-slate-800 rounded-lg p-6 border border-slate-700">
              <h3 className="text-sm font-medium text-slate-400 mb-2">Completed Jobs</h3>
              <p className="text-3xl font-bold text-green-400">{stats.completedJobs}</p>
            </div>
            <div className="bg-slate-800 rounded-lg p-6 border border-slate-700">
              <h3 className="text-sm font-medium text-slate-400 mb-2">Failed Jobs</h3>
              <p className="text-3xl font-bold text-red-400">{stats.failedJobs}</p>
            </div>
          </div>
        )}

        {/* Bindings List */}
        <div className="bg-slate-800 rounded-lg border border-slate-700 overflow-hidden">
          <div className="p-6 border-b border-slate-700">
            <h2 className="text-xl font-semibold text-white">Active Bindings</h2>
          </div>
          <div className="divide-y divide-slate-700">
            {bindings.length === 0 ? (
              <div className="p-6 text-center text-slate-400">
                No bindings configured yet
              </div>
            ) : (
              bindings.map((binding) => (
                <div key={binding.id} className="p-6 hover:bg-slate-700/30 transition-colors">
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <h3 className="text-lg font-medium text-white mb-2">
                        {binding.sourcePath} → {binding.targetPath}
                      </h3>
                      <div className="flex gap-4 text-sm text-slate-400">
                        <span>Direction: {binding.direction === 2 ? 'Bidirectional' : 'One-way'}</span>
                        <span>Cadence: {binding.scheduleCadenceMinutes} min</span>
                        <span className={binding.isActive ? 'text-green-400' : 'text-red-400'}>
                          {binding.isActive ? 'Active' : 'Inactive'}
                        </span>
                        <span className={binding.isPaused ? 'text-yellow-400' : ''}>
                          {binding.isPaused ? 'Paused' : 'Running'}
                        </span>
                      </div>
                    </div>
                    <div className="flex gap-2">
                      <Button
                        onClick={() => handlePauseBinding(binding.id, binding.isPaused)}
                        variant="outline"
                        className="border-slate-600"
                      >
                        {binding.isPaused ? 'Resume' : 'Pause'}
                      </Button>
                      <Button
                        onClick={() => handleManualSync(binding.id)}
                        className="bg-blue-600 hover:bg-blue-700"
                        disabled={!binding.isActive || binding.isPaused}
                      >
                        Sync Now
                      </Button>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
