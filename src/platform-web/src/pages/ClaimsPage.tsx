import { useEffect, useState } from 'react';
import { apiClient, Claim, Location } from '../api/client';

export default function ClaimsPage() {
  const [claims, setClaims] = useState<Claim[]>([]);
  const [locations, setLocations] = useState<Location[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setLoading(true);
    setClaims(await apiClient.claims.list());
    setLocations(await apiClient.locations());
    setLoading(false);
  };

  useEffect(() => { load(); }, []);

  const ingest = async () => {
    if (!locations[0]) return;
    setBusy(true);
    await apiClient.claims.ingest(locations[0].id);
    await load();
    setBusy(false);
  };

  const submit = async (id: string) => {
    setBusy(true);
    await apiClient.claims.submit(id);
    await load();
    setBusy(false);
  };

  const statusClass = (s: string) => {
    if (s.includes('Accepted') || s === 'Submitted') return 'badge-success';
    if (s.includes('Failed') || s.includes('Rejected')) return 'badge-danger';
    return 'badge-info';
  };

  return (
    <div>
      <div className="page-header">
        <h1>Claims Lifecycle</h1>
        <p>Ingest from DentalBridge, scrub, submit 837D, monitor 999/277CA</p>
      </div>

      <div className="toolbar">
        <button className="btn" onClick={ingest} disabled={busy}>Ingest from PMS</button>
        <button className="btn btn-secondary" onClick={load} disabled={busy}>Refresh</button>
      </div>

      {loading ? <div className="loading">Loading...</div> : (
        <div className="card">
          <table>
            <thead>
              <tr>
                <th>Claim ID</th><th>Payer</th><th>Status</th><th>DOS</th><th>Amount</th><th>Lines</th><th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {claims.map(c => (
                <tr key={c.id}>
                  <td>{c.externalClaimId}</td>
                  <td>{c.payerName}</td>
                  <td><span className={`badge ${statusClass(c.status)}`}>{c.status}</span></td>
                  <td>{c.dateOfService}</td>
                  <td>${c.totalChargeAmount.toFixed(2)}</td>
                  <td>{c.lineCount}</td>
                  <td className="actions">
                    {(c.status === 'Draft' || c.status === 'ReadyToSubmit' || c.status === 'ScrubFailed') && (
                      <button className="btn btn-sm" onClick={() => submit(c.id)} disabled={busy}>Submit</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
