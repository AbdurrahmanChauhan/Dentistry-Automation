import { Routes, Route, NavLink } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import EligibilityPage from './pages/EligibilityPage';
import ClaimsPage from './pages/ClaimsPage';
import RemittancesPage from './pages/RemittancesPage';
import WorkItemsPage from './pages/WorkItemsPage';

export default function App() {
  return (
    <div className="app">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-icon">DA</span>
          <div>
            <strong>Dentistry Automation</strong>
            <small>RCM Console</small>
          </div>
        </div>
        <nav>
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/eligibility">Eligibility</NavLink>
          <NavLink to="/claims">Claims</NavLink>
          <NavLink to="/remittances">Remittances</NavLink>
          <NavLink to="/work-items">Work Queue</NavLink>
        </nav>
      </aside>
      <main className="content">
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/eligibility" element={<EligibilityPage />} />
          <Route path="/claims" element={<ClaimsPage />} />
          <Route path="/remittances" element={<RemittancesPage />} />
          <Route path="/work-items" element={<WorkItemsPage />} />
        </Routes>
      </main>
    </div>
  );
}
