import { useEffect, useState } from 'react';
import { apiClient, WorkItem } from '../api/client';

export default function WorkItemsPage() {
  const [items, setItems] = useState<WorkItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('Open');

  const load = async () => {
    setLoading(true);
    setItems(await apiClient.workItems.list(filter || undefined));
    setLoading(false);
  };

  useEffect(() => { load(); }, [filter]);

  const aiSummary = async (id: string) => {
    await apiClient.workItems.aiSummary(id);
    await load();
  };

  const resolve = async (id: string) => {
    await apiClient.workItems.update(id, { status: 'Resolved', resolutionNotes: 'Resolved via console' });
    await load();
  };

  const priorityClass = (p: string) =>
    p === 'Critical' || p === 'High' ? 'badge-danger' : p === 'Medium' ? 'badge-warning' : 'badge-info';

  return (
    <div>
      <div className="page-header">
        <h1>Exception Work Queue</h1>
        <p>Human-in-the-loop review for denials, posting exceptions, and ack rejections</p>
      </div>

      <div className="toolbar">
        <select value={filter} onChange={e => setFilter(e.target.value)}
          style={{ padding: '0.5rem', borderRadius: '8px', background: 'var(--surface)', color: 'var(--text)', border: '1px solid var(--border)' }}>
          <option value="">All</option>
          <option value="Open">Open</option>
          <option value="Assigned">Assigned</option>
          <option value="Resolved">Resolved</option>
        </select>
        <button className="btn btn-secondary" onClick={load}>Refresh</button>
      </div>

      {loading ? <div className="loading">Loading...</div> : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {items.map(w => (
            <div key={w.id} className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div>
                  <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.5rem' }}>
                    <span className={`badge ${priorityClass(w.priority)}`}>{w.priority}</span>
                    <span className="badge badge-info">{w.type}</span>
                    {w.carcCode && <span className="badge badge-warning">{w.carcCode}</span>}
                  </div>
                  <strong>{w.title}</strong>
                  <p style={{ color: 'var(--muted)', marginTop: '0.25rem', fontSize: '0.875rem' }}>{w.description}</p>
                </div>
                <div className="actions">
                  {!w.aiSummary && (
                    <button className="btn btn-sm btn-secondary" onClick={() => aiSummary(w.id)}>AI Summary</button>
                  )}
                  {w.status === 'Open' && (
                    <button className="btn btn-sm" onClick={() => resolve(w.id)}>Resolve</button>
                  )}
                </div>
              </div>
              {w.aiSummary && (
                <div className="ai-box">
                  <strong>AI Analysis</strong>
                  {w.aiSummary}
                  {w.suggestedAction && <p style={{ marginTop: '0.5rem' }}><strong>Suggested:</strong> {w.suggestedAction}</p>}
                </div>
              )}
            </div>
          ))}
          {items.length === 0 && <div className="card" style={{ textAlign: 'center', color: 'var(--muted)' }}>No work items</div>}
        </div>
      )}
    </div>
  );
}
