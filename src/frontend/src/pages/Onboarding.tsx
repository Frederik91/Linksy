import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { tenantApi, connectorApi, bindingApi } from '../lib/api';
import { ConnectorType, SyncDirection, ConflictPolicy } from '../types';

export default function Onboarding() {
  const [step, setStep] = useState(1);
  const [tenantId, setTenantId] = useState('');
  const [tenantName, setTenantName] = useState('');
  const [sourceConnectorId, setSourceConnectorId] = useState('');
  const [targetConnectorId, setTargetConnectorId] = useState('');

  const handleCreateTenant = async () => {
    try {
      const tenant = await tenantApi.create({ name: tenantName });
      setTenantId(tenant.id);
      setStep(2);
    } catch (error) {
      console.error('Failed to create tenant:', error);
      alert('Failed to create tenant');
    }
  };

  const handleCreateConnector = async (type: ConnectorType, name: string) => {
    try {
      const connector = await connectorApi.create({
        tenantId,
        type,
        name,
        configuration: JSON.stringify({}),
        encryptedCredential: 'placeholder-credential',
        keyIdentifier: 'placeholder-key',
      });

      if (type === ConnectorType.AutodeskConstructionCloud) {
        setSourceConnectorId(connector.id);
      } else {
        setTargetConnectorId(connector.id);
      }

      return connector.id;
    } catch (error) {
      console.error('Failed to create connector:', error);
      throw error;
    }
  };

  const handleCreateBinding = async () => {
    try {
      await bindingApi.create({
        tenantId,
        sourceConnectorId,
        targetConnectorId,
        sourcePath: '/project/docs',
        targetPath: '/Shared Documents',
        direction: SyncDirection.Bidirectional,
        conflictPolicy: ConflictPolicy.LastWriterWins,
        scheduleCadenceMinutes: 30,
      });
      setStep(4);
    } catch (error) {
      console.error('Failed to create binding:', error);
      alert('Failed to create binding');
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 to-slate-800">
      <header className="bg-slate-900/50 backdrop-blur-sm border-b border-slate-700">
        <div className="max-w-4xl mx-auto px-4 py-6">
          <h1 className="text-3xl font-bold text-white">Tenant Onboarding</h1>
          <p className="text-slate-400 mt-1">Set up your file synchronization in 4 easy steps</p>
        </div>
      </header>

      <main className="max-w-4xl mx-auto px-4 py-12">
        <div className="mb-8 flex items-center justify-center">
          {[1, 2, 3, 4].map((s) => (
            <div key={s} className="flex items-center">
              <div
                className={`w-10 h-10 rounded-full flex items-center justify-center font-semibold ${
                  s <= step
                    ? 'bg-blue-600 text-white'
                    : 'bg-slate-700 text-slate-400'
                }`}
              >
                {s}
              </div>
              {s < 4 && (
                <div
                  className={`w-16 h-1 ${
                    s < step ? 'bg-blue-600' : 'bg-slate-700'
                  }`}
                />
              )}
            </div>
          ))}
        </div>

        <div className="bg-slate-800 rounded-lg p-8 border border-slate-700">
          {step === 1 && (
            <div>
              <h2 className="text-2xl font-bold text-white mb-4">Create Tenant</h2>
              <p className="text-slate-400 mb-6">
                Enter your organization name to get started
              </p>
              <input
                type="text"
                placeholder="Organization Name"
                className="w-full bg-slate-900 text-white border border-slate-700 rounded-lg px-4 py-3 mb-4"
                value={tenantName}
                onChange={(e) => setTenantName(e.target.value)}
              />
              <Button
                onClick={handleCreateTenant}
                className="w-full bg-blue-600 hover:bg-blue-700"
                disabled={!tenantName}
              >
                Continue
              </Button>
            </div>
          )}

          {step === 2 && (
            <div>
              <h2 className="text-2xl font-bold text-white mb-4">Configure Source Connector</h2>
              <p className="text-slate-400 mb-6">
                Connect to Autodesk Construction Cloud
              </p>
              <Button
                onClick={async () => {
                  await handleCreateConnector(
                    ConnectorType.AutodeskConstructionCloud,
                    'ACC Docs'
                  );
                  setStep(3);
                }}
                className="w-full bg-blue-600 hover:bg-blue-700"
              >
                Connect ACC Docs
              </Button>
            </div>
          )}

          {step === 3 && (
            <div>
              <h2 className="text-2xl font-bold text-white mb-4">Configure Target Connector</h2>
              <p className="text-slate-400 mb-6">
                Connect to Microsoft SharePoint
              </p>
              <Button
                onClick={async () => {
                  await handleCreateConnector(ConnectorType.SharePoint, 'SharePoint');
                  await handleCreateBinding();
                }}
                className="w-full bg-blue-600 hover:bg-blue-700"
              >
                Connect SharePoint
              </Button>
            </div>
          )}

          {step === 4 && (
            <div className="text-center">
              <div className="text-6xl mb-4">🎉</div>
              <h2 className="text-2xl font-bold text-white mb-4">Setup Complete!</h2>
              <p className="text-slate-400 mb-6">
                Your file synchronization has been configured successfully
              </p>
              <Button
                onClick={() => (window.location.href = '/')}
                className="bg-blue-600 hover:bg-blue-700"
              >
                Go to Dashboard
              </Button>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
