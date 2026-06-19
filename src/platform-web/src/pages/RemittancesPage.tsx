import { useEffect, useState } from 'react';
import { apiClient, Remittance } from '../api/client';

export default function RemittancesPage() {
  const [remittances, setRemittances] = useState<Remittance[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setLoading(true);
    setRemittances(await apiClient.remittances.list());
    setLoading(false);
  };

  useEffect(() => { load(); }, []);

  const poll = async () => {
    setBusy(true);
    await apiClient.remittances.poll();
    await load();
    setBusy(false);
  };

  const autoPost = async (id: string) => {
    setBusy(true);
    const result = await apiClient.remittances.autoPost(id);
    alert(`Posted ${result.postedCount} lines`);
    await load();
    setBusy(false);
  };

  return (
    <div>
      <div className="page-header">
        <h1>ERA / 835 Remittances</h1>
        <p>Payment matching, auto-posting, and exception routing</p>
      </div>

      <div className="toolbar">
        <button className="btn" onClick={poll} disabled={busy}>Poll Clearinghouse</button>
        <button className="btn btn-secondary" onClick={load} disabled={busy}>Refresh</button>
      </div>

      {loading ? <div className="loading">Loading...</div> : (
        <div className="card">
          <table>
            <thead>
              <tr>
                <th>ERA Reference</th><th>Payer</th><th>Payment Date</th><th>Amount</th>
                <th>Status</th><th>Posted</th><th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {remittances.map(r => (
                <tr key={r.id}>
                  <td>{r.eraReference}</td>
                  <td>{r.payerName}</td>
                  <td>{r.paymentDate}</td>
                  <td>${r.totalPaymentAmount.toFixed(2)}</td>
                  <td><span className="badge badge-info">{r.status}</span></td>
                  <td>{r.postedLines}/{r.lineCount}</td>
                  <td>
                    {r.status !== 'Posted' && (
                      <button className="btn btn-sm" onClick={() => autoPost(r.id)} disabled={busy}>Auto-Post</button>
                    )}
                  </td>
                </tr>
              ))}
              {remittances.length === 0 && (
                <tr><td colSpan={7} style={{ textAlign: 'center', color: 'var(--muted)' }}>
                  No remittances — click Poll Clearinghouse
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
