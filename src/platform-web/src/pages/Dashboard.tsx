import { useEffect, useState } from 'react';
import { apiClient, Kpis, Location } from '../api/client';

function statusBadge(status: string) {
  const map: Record<string, string> = {
    healthy: 'badge-success',
    Verified: 'badge-success',
    Open: 'badge-warning',
    Failed: 'badge-danger',
  };
  return <span className={`badge ${map[status] || 'badge-info'}`}>{status}</span>;
}

export default function Dashboard() {
  const [health, setHealth] = useState<string>('');
  const [locations, setLocations] = useState<Location[]>([]);
  const [kpis, setKpis] = useState<Kpis | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    (async () => {
      try {
        const h = await apiClient.health();
        setHealth(h.status);
        const locs = await apiClient.locations();
        setLocations(locs);
        if (locs[0]) setKpis(await apiClient.kpis(locs[0].id));
      } catch (e) {
        setError((e as Error).message);
      }
    })();
  }, []);

  if (error) return <div className="error">API Error: {error}. Ensure the API is running on port 5000.</div>;

  return (
    <div>
      <div className="page-header">
        <h1>RCM Dashboard</h1>
        <p>Hybrid automation platform — eligibility, claims, remittances, exceptions</p>
      </div>

      <div className="card-grid">
        <div className="card stat-card">
          <h3>Platform Health</h3>
          <div className="value">{health ? statusBadge(health) : '...'}</div>
        </div>
        {kpis && (
          <>
            <div className="card stat-card">
              <h3>Total Claims</h3>
              <div className="value">{kpis.totalClaims}</div>
            </div>
            <div className="card stat-card">
              <h3>Open Work Items</h3>
              <div className="value">{kpis.openWorkItems}</div>
            </div>
            <div className="card stat-card">
              <h3>Eligibility Verified</h3>
              <div className="value">{kpis.eligibilityVerifiedRate}%</div>
            </div>
            <div className="card stat-card">
              <h3>Denial Rate</h3>
              <div className="value">{kpis.denialRate}%</div>
            </div>
            <div className="card stat-card">
              <h3>Remittances</h3>
              <div className="value">{kpis.remittancesReceived}</div>
            </div>
          </>
        )}
      </div>

      <div className="card">
        <h3 style={{ marginBottom: '1rem' }}>Locations</h3>
        <table>
          <thead>
            <tr><th>Name</th><th>Clinic ID</th><th>PMS</th><th>Region</th></tr>
          </thead>
          <tbody>
            {locations.map(l => (
              <tr key={l.id}>
                <td>{l.name}</td>
                <td>{l.externalClinicId}</td>
                <td>{l.pmsType}</td>
                <td>{l.region || '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
