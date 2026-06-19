import { useEffect, useState } from 'react';
import { apiClient, EligibilityCheck, Location, Patient } from '../api/client';

export default function EligibilityPage() {
  const [checks, setChecks] = useState<EligibilityCheck[]>([]);
  const [locations, setLocations] = useState<Location[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedLocation, setSelectedLocation] = useState('');
  const [selectedPatient, setSelectedPatient] = useState('');

  const load = async () => {
    setLoading(true);
    const locs = await apiClient.locations();
    setLocations(locs);
    const locId = selectedLocation || locs[0]?.id;
    if (locId) {
      setChecks(await apiClient.eligibility.list(locId));
      setPatients(await apiClient.patients(locId));
    }
    setLoading(false);
  };

  useEffect(() => { load(); }, [selectedLocation]);

  const triggerCheck = async () => {
    const locId = selectedLocation || locations[0]?.id;
    const patId = selectedPatient || patients[0]?.id;
    if (!locId || !patId) return;
    await apiClient.eligibility.trigger({ locationId: locId, patientId: patId });
    await load();
  };

  const statusClass = (s: string) =>
    s === 'Verified' ? 'badge-success' : s === 'Failed' ? 'badge-danger' : 'badge-warning';

  return (
    <div>
      <div className="page-header">
        <h1>Eligibility Verification</h1>
        <p>270/271 orchestration with benefit snapshots and PMS write-back</p>
      </div>

      <div className="toolbar">
        <select value={selectedLocation} onChange={e => setSelectedLocation(e.target.value)}
          style={{ padding: '0.5rem', borderRadius: '8px', background: 'var(--surface)', color: 'var(--text)', border: '1px solid var(--border)' }}>
          <option value="">All locations</option>
          {locations.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}
        </select>
        <select value={selectedPatient} onChange={e => setSelectedPatient(e.target.value)}
          style={{ padding: '0.5rem', borderRadius: '8px', background: 'var(--surface)', color: 'var(--text)', border: '1px solid var(--border)' }}>
          {patients.map(p => <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>)}
        </select>
        <button className="btn" onClick={triggerCheck}>Run Eligibility Check</button>
        <button className="btn btn-secondary" onClick={load}>Refresh</button>
      </div>

      {loading ? <div className="loading">Loading...</div> : (
        <div className="card">
          <table>
            <thead>
              <tr>
                <th>Patient</th><th>Status</th><th>Confidence</th><th>Benefits</th><th>Completed</th>
              </tr>
            </thead>
            <tbody>
              {checks.map(c => (
                <tr key={c.id}>
                  <td>{c.patient ? `${c.patient.firstName} ${c.patient.lastName}` : c.patientId}</td>
                  <td><span className={`badge ${statusClass(c.status)}`}>{c.status}</span></td>
                  <td>{(c.confidenceScore * 100).toFixed(0)}%</td>
                  <td>
                    {c.benefits ? (
                      <small>{c.benefits.planName} — Max remaining: ${c.benefits.annualMaximumRemaining}</small>
                    ) : c.benefitSummary || '—'}
                  </td>
                  <td>{c.completedAt ? new Date(c.completedAt).toLocaleString() : '—'}</td>
                </tr>
              ))}
              {checks.length === 0 && <tr><td colSpan={5} style={{ textAlign: 'center', color: 'var(--muted)' }}>No eligibility checks yet</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
