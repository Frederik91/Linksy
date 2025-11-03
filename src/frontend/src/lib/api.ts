import type { Tenant, Connector, Binding, SyncJob, SyncJobStats } from '../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

// Tenant API
export const tenantApi = {
  getAll: async (): Promise<Tenant[]> => {
    const res = await fetch(`${API_URL}/api/tenants`);
    if (!res.ok) throw new Error('Failed to fetch tenants');
    return res.json();
  },

  getById: async (id: string): Promise<Tenant> => {
    const res = await fetch(`${API_URL}/api/tenants/${id}`);
    if (!res.ok) throw new Error('Failed to fetch tenant');
    return res.json();
  },

  create: async (data: { name: string; billingProfile?: string }): Promise<Tenant> => {
    const res = await fetch(`${API_URL}/api/tenants`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create tenant');
    return res.json();
  },
};

// Connector API
export const connectorApi = {
  getByTenant: async (tenantId: string): Promise<Connector[]> => {
    const res = await fetch(`${API_URL}/api/connectors/tenant/${tenantId}`);
    if (!res.ok) throw new Error('Failed to fetch connectors');
    return res.json();
  },

  create: async (data: {
    tenantId: string;
    type: number;
    name: string;
    configuration: string;
    encryptedCredential: string;
    keyIdentifier: string;
  }): Promise<Connector> => {
    const res = await fetch(`${API_URL}/api/connectors`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create connector');
    return res.json();
  },

  checkHealth: async (connectorId: string) => {
    const res = await fetch(`${API_URL}/api/connectors/${connectorId}/health-check`, {
      method: 'POST',
    });
    if (!res.ok) throw new Error('Failed to check connector health');
    return res.json();
  },
};

// Binding API
export const bindingApi = {
  getByTenant: async (tenantId: string): Promise<Binding[]> => {
    const res = await fetch(`${API_URL}/api/bindings/tenant/${tenantId}`);
    if (!res.ok) throw new Error('Failed to fetch bindings');
    return res.json();
  },

  create: async (data: {
    tenantId: string;
    sourceConnectorId: string;
    targetConnectorId: string;
    sourcePath: string;
    targetPath: string;
    direction: number;
    conflictPolicy: number;
    scheduleCadenceMinutes: number;
    filters?: string;
  }): Promise<Binding> => {
    const res = await fetch(`${API_URL}/api/bindings`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create binding');
    return res.json();
  },

  pause: async (bindingId: string) => {
    const res = await fetch(`${API_URL}/api/bindings/${bindingId}/pause`, {
      method: 'POST',
    });
    if (!res.ok) throw new Error('Failed to pause binding');
    return res.json();
  },

  resume: async (bindingId: string) => {
    const res = await fetch(`${API_URL}/api/bindings/${bindingId}/resume`, {
      method: 'POST',
    });
    if (!res.ok) throw new Error('Failed to resume binding');
    return res.json();
  },
};

// Sync Job API
export const syncJobApi = {
  getByBinding: async (bindingId: string): Promise<SyncJob[]> => {
    const res = await fetch(`${API_URL}/api/sync-jobs/binding/${bindingId}`);
    if (!res.ok) throw new Error('Failed to fetch sync jobs');
    return res.json();
  },

  create: async (data: { bindingId: string; isManualTrigger: boolean }): Promise<SyncJob> => {
    const res = await fetch(`${API_URL}/api/sync-jobs`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error('Failed to create sync job');
    return res.json();
  },

  getStats: async (tenantId: string): Promise<SyncJobStats> => {
    const res = await fetch(`${API_URL}/api/sync-jobs/tenant/${tenantId}/stats`);
    if (!res.ok) throw new Error('Failed to fetch sync job stats');
    return res.json();
  },
};
