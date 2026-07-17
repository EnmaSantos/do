"use client";

import {
  Activity,
  AlertTriangle,
  ArrowDownRight,
  ArrowRight,
  ArrowUpRight,
  Bell,
  BookOpen,
  Boxes,
  Check,
  CheckCircle2,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  CircleDot,
  Clock3,
  Command,
  ExternalLink,
  FileText,
  Filter,
  Gauge,
  HelpCircle,
  History,
  Inbox,
  Laptop,
  LayoutDashboard,
  LifeBuoy,
  LockKeyhole,
  Menu,
  MessageSquareText,
  MoreHorizontal,
  Network,
  PackagePlus,
  Plus,
  Search,
  Server,
  Settings,
  ShieldCheck,
  SlidersHorizontal,
  Sparkles,
  Tag,
  ThumbsUp,
  X,
} from "lucide-react";
import { FormEvent, useMemo, useState } from "react";

type View = "dashboard" | "assets" | "incidents" | "services" | "knowledge" | "audit";
type Role = "Administrator" | "Support Engineer" | "Inventory Manager" | "Viewer";
type ModalKind = "asset" | "incident" | "article" | null;

type Asset = {
  tag: string;
  name: string;
  category: string;
  location: string;
  department: string;
  quantity: number;
  threshold: number;
  status: "In stock" | "Assigned" | "Maintenance" | "Retired";
  warranty: string;
  assignedTo?: string;
};

type Incident = {
  number: string;
  title: string;
  description: string;
  severity: "SEV-1" | "SEV-2" | "SEV-3" | "SEV-4";
  status: "New" | "Investigating" | "In progress" | "Monitoring" | "Resolved";
  service: string;
  assignee: string;
  age: string;
  updated: string;
  timeline: { time: string; author: string; body: string }[];
};

type Article = {
  id: string;
  title: string;
  summary: string;
  service: string;
  tags: string[];
  author: string;
  status: "Published" | "Draft";
  reviewed: string;
  helpful: number;
};

const initialAssets: Asset[] = [
  { tag: "AST-1048", name: "MacBook Pro 14-inch", category: "Laptop", location: "Boise HQ", department: "Engineering", quantity: 12, threshold: 5, status: "In stock", warranty: "Nov 18, 2027" },
  { tag: "AST-1042", name: "Dell Latitude 7440", category: "Laptop", location: "Idaho Falls", department: "Operations", quantity: 3, threshold: 5, status: "In stock", warranty: "Aug 02, 2026" },
  { tag: "AST-0981", name: "ThinkSystem SR650", category: "Server", location: "Boise Data Center", department: "Infrastructure", quantity: 1, threshold: 1, status: "Maintenance", warranty: "Jul 29, 2026" },
  { tag: "AST-0874", name: "iPhone 15", category: "Mobile", location: "Twin Falls", department: "Sales", quantity: 8, threshold: 4, status: "Assigned", warranty: "Jan 14, 2027", assignedTo: "Regional sales team" },
  { tag: "AST-0733", name: "Cisco Catalyst 9300", category: "Network", location: "Boise HQ", department: "Infrastructure", quantity: 2, threshold: 2, status: "Assigned", warranty: "Sep 04, 2026", assignedTo: "Network closet B" },
  { tag: "AST-0521", name: "HP EliteDisplay E243", category: "Monitor", location: "Idaho Falls", department: "Operations", quantity: 0, threshold: 4, status: "Retired", warranty: "Expired" },
];

const initialIncidents: Incident[] = [
  {
    number: "INC-2026-0147",
    title: "Employee Portal authentication failures",
    description: "Employees are receiving intermittent 401 responses after the identity certificate rotation.",
    severity: "SEV-1",
    status: "Investigating",
    service: "Employee Portal",
    assignee: "Maya Chen",
    age: "43m",
    updated: "4 min ago",
    timeline: [
      { time: "9:17 AM", author: "Priya Shah", body: "Incident declared after failed-login rate crossed 35%." },
      { time: "9:25 AM", author: "Maya Chen", body: "Compared gateway and identity-provider logs; failure starts after certificate rotation." },
      { time: "9:39 AM", author: "Maya Chen", body: "Rollback is staged. Validating token signatures in canary." },
    ],
  },
  {
    number: "INC-2026-0146",
    title: "Inventory sync queue is delayed",
    description: "Warehouse updates are processing approximately 18 minutes behind real time.",
    severity: "SEV-2",
    status: "Monitoring",
    service: "Inventory Synchronization",
    assignee: "Noah Williams",
    age: "2h 14m",
    updated: "12 min ago",
    timeline: [
      { time: "7:46 AM", author: "Noah Williams", body: "Scaled consumers from four to eight instances." },
      { time: "8:52 AM", author: "Noah Williams", body: "Backlog cleared. Monitoring new message latency." },
    ],
  },
  {
    number: "INC-2026-0144",
    title: "Payment receipt emails missing",
    description: "Completed orders are not consistently generating receipt emails.",
    severity: "SEV-3",
    status: "In progress",
    service: "Notification Service",
    assignee: "Elena Ruiz",
    age: "5h 09m",
    updated: "28 min ago",
    timeline: [{ time: "5:10 AM", author: "Elena Ruiz", body: "Reproduced with receipts containing non-ASCII customer names." }],
  },
  {
    number: "INC-2026-0141",
    title: "Office Wi-Fi roaming instability",
    description: "Users on the third floor briefly disconnect while moving between access points.",
    severity: "SEV-4",
    status: "New",
    service: "Corporate Network",
    assignee: "Unassigned",
    age: "1d 3h",
    updated: "Yesterday",
    timeline: [{ time: "Yesterday", author: "Alex Kim", body: "Four affected devices identified in Boise HQ." }],
  },
];

const services = [
  { name: "Customer Ordering API", owner: "Commerce Platform", environment: "Production", status: "Operational", uptime: "99.99%", incidents: 0, dependencies: 4 },
  { name: "Employee Portal", owner: "Workplace Systems", environment: "Production", status: "Major incident", uptime: "99.82%", incidents: 1, dependencies: 6 },
  { name: "Inventory Synchronization", owner: "Supply Chain", environment: "Production", status: "Degraded", uptime: "99.91%", incidents: 1, dependencies: 3 },
  { name: "Payment Processing", owner: "Finance Technology", environment: "Production", status: "Operational", uptime: "99.995%", incidents: 0, dependencies: 5 },
  { name: "Notification Service", owner: "Core Services", environment: "Production", status: "Degraded", uptime: "99.94%", incidents: 1, dependencies: 4 },
  { name: "Corporate Network", owner: "Infrastructure", environment: "Production", status: "Operational", uptime: "99.97%", incidents: 1, dependencies: 2 },
];

const initialArticles: Article[] = [
  { id: "KB-0087", title: "Recovering from stale identity certificates", summary: "How to identify certificate mismatch errors, roll back safely, and validate token signatures.", service: "Employee Portal", tags: ["identity", "certificates", "runbook"], author: "Maya Chen", status: "Published", reviewed: "Jul 11, 2026", helpful: 42 },
  { id: "KB-0084", title: "Clearing inventory consumer backlogs", summary: "Safe scaling procedure and validation queries for the synchronization message queue.", service: "Inventory Synchronization", tags: ["queue", "scaling"], author: "Noah Williams", status: "Published", reviewed: "Jun 28, 2026", helpful: 31 },
  { id: "KB-0081", title: "Receipt generation with Unicode customer data", summary: "Symptoms and remediation for template rendering failures caused by unexpected character encodings.", service: "Notification Service", tags: ["email", "encoding"], author: "Elena Ruiz", status: "Draft", reviewed: "Jul 16, 2026", helpful: 0 },
  { id: "KB-0079", title: "Restoring point-of-sale VPN connectivity", summary: "A decision tree for branch VPN failures, including tunnel reset and escalation steps.", service: "Corporate Network", tags: ["network", "vpn", "runbook"], author: "Owen Park", status: "Published", reviewed: "Jun 18, 2026", helpful: 56 },
];

const auditEvents = [
  { time: "9:39 AM", user: "Maya Chen", action: "added investigation update", entity: "INC-2026-0147", type: "Incident", tone: "purple" },
  { time: "9:25 AM", user: "Maya Chen", action: "changed status to Investigating", entity: "INC-2026-0147", type: "Incident", tone: "purple" },
  { time: "9:17 AM", user: "Priya Shah", action: "created critical incident", entity: "INC-2026-0147", type: "Incident", tone: "red" },
  { time: "8:52 AM", user: "Noah Williams", action: "changed status to Monitoring", entity: "INC-2026-0146", type: "Incident", tone: "blue" },
  { time: "8:14 AM", user: "Jordan Lee", action: "transferred asset to Idaho Falls", entity: "AST-1042", type: "Asset", tone: "green" },
  { time: "7:58 AM", user: "Jordan Lee", action: "adjusted quantity from 5 to 3", entity: "AST-1042", type: "Asset", tone: "amber" },
  { time: "Yesterday", user: "Elena Ruiz", action: "created knowledge draft", entity: "KB-0081", type: "Article", tone: "blue" },
  { time: "Yesterday", user: "System", action: "flagged warranty expiration", entity: "AST-0981", type: "Alert", tone: "amber" },
];

const navItems: { id: View; label: string; icon: typeof LayoutDashboard; count?: number }[] = [
  { id: "dashboard", label: "Overview", icon: LayoutDashboard },
  { id: "incidents", label: "Incidents", icon: LifeBuoy, count: 4 },
  { id: "assets", label: "Assets", icon: Boxes },
  { id: "services", label: "Services", icon: Network },
  { id: "knowledge", label: "Knowledge", icon: BookOpen },
  { id: "audit", label: "Audit log", icon: History },
];

const viewMeta: Record<View, { eyebrow: string; title: string; description: string }> = {
  dashboard: { eyebrow: "Friday, July 17", title: "Operations overview", description: "Here’s what needs attention across production and inventory." },
  incidents: { eyebrow: "Production support", title: "Incidents", description: "Triage, investigate, and resolve active production issues." },
  assets: { eyebrow: "Inventory", title: "Asset registry", description: "Track equipment, assignments, stock, and lifecycle history." },
  services: { eyebrow: "Service registry", title: "Applications & services", description: "Ownership, health, dependencies, and production impact." },
  knowledge: { eyebrow: "Knowledge base", title: "Runbooks & resolutions", description: "Turn solved incidents into searchable operational knowledge." },
  audit: { eyebrow: "Governance", title: "Audit activity", description: "An immutable trail of important changes across OpsFlow." },
};

function LogoMark() {
  return (
    <span className="logo-mark" aria-hidden="true">
      <span />
      <span />
      <span />
    </span>
  );
}

function Badge({ children, tone = "neutral" }: { children: React.ReactNode; tone?: string }) {
  return <span className={`badge badge-${tone}`}>{children}</span>;
}

function severityTone(severity: Incident["severity"]) {
  return severity === "SEV-1" ? "red" : severity === "SEV-2" ? "orange" : severity === "SEV-3" ? "amber" : "blue";
}

function statusTone(status: string) {
  if (["Operational", "Published", "Resolved", "In stock"].includes(status)) return "green";
  if (["Major incident", "Maintenance"].includes(status)) return "red";
  if (["Degraded", "Monitoring", "Draft"].includes(status)) return "amber";
  if (["Investigating", "In progress", "Assigned"].includes(status)) return "purple";
  return "neutral";
}

function Login({ onLogin }: { onLogin: (role: Role, name: string) => void }) {
  const [email, setEmail] = useState("admin@opsflow.demo");
  const [password, setPassword] = useState("Demo123!");
  const [showHelp, setShowHelp] = useState(false);

  const accounts: { email: string; name: string; role: Role; initials: string }[] = [
    { email: "admin@opsflow.demo", name: "Priya Shah", role: "Administrator", initials: "PS" },
    { email: "support@opsflow.demo", name: "Maya Chen", role: "Support Engineer", initials: "MC" },
    { email: "inventory@opsflow.demo", name: "Jordan Lee", role: "Inventory Manager", initials: "JL" },
    { email: "viewer@opsflow.demo", name: "Alex Kim", role: "Viewer", initials: "AK" },
  ];

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const account = accounts.find((item) => item.email === email) ?? accounts[0];
    onLogin(account.role, account.name);
  };

  return (
    <main className="login-shell">
      <section className="login-story">
        <div className="login-brand"><LogoMark /><span>OpsFlow</span></div>
        <div className="story-copy">
          <Badge tone="purple"><Sparkles size={13} /> Operations, connected</Badge>
          <h1>Know what’s running.<br />Fix what’s not.</h1>
          <p>One workspace for production incidents, equipment inventory, and the knowledge your team earns along the way.</p>
          <div className="story-proof">
            <div><span className="proof-icon"><Gauge size={20} /></span><span><strong>Respond with context</strong><small>See affected services and assets together</small></span></div>
            <div><span className="proof-icon"><History size={20} /></span><span><strong>Keep the full history</strong><small>Audit every operational change</small></span></div>
            <div><span className="proof-icon"><BookOpen size={20} /></span><span><strong>Make every fix reusable</strong><small>Turn resolutions into trusted runbooks</small></span></div>
          </div>
        </div>
        <div className="story-footer"><span><ShieldCheck size={15} /> Internal operations portal</span><span>v1.0 demo</span></div>
        <div className="orb orb-one" /><div className="orb orb-two" /><div className="grid-glow" />
      </section>
      <section className="login-panel">
        <div className="login-card">
          <div className="mobile-brand"><LogoMark /><span>OpsFlow</span></div>
          <p className="login-kicker">WELCOME BACK</p>
          <h2>Sign in to OpsFlow</h2>
          <p className="login-subtitle">Use a demo account to explore each role.</p>
          <form onSubmit={submit}>
            <label>Email address<input value={email} onChange={(e) => setEmail(e.target.value)} type="email" /></label>
            <label>Password<span className="password-wrap"><input value={password} onChange={(e) => setPassword(e.target.value)} type="password" /><LockKeyhole size={16} /></span></label>
            <button className="button button-primary login-button" type="submit">Sign in <ArrowRight size={17} /></button>
          </form>
          <div className="divider"><span>or choose a demo role</span></div>
          <div className="demo-accounts">
            {accounts.map((account) => (
              <button key={account.email} onClick={() => { setEmail(account.email); setPassword("Demo123!"); }} className={email === account.email ? "selected" : ""}>
                <span className="avatar avatar-small">{account.initials}</span>
                <span><strong>{account.role}</strong><small>{account.email}</small></span>
                {email === account.email && <Check size={16} />}
              </button>
            ))}
          </div>
          <button className="security-note" onClick={() => setShowHelp(!showHelp)}><ShieldCheck size={15} /> Demo data only · Password: Demo123! <HelpCircle size={14} /></button>
          {showHelp && <p className="demo-help">Changes made in this hosted preview reset when the page refreshes. The full repository includes the authenticated SQL Server API.</p>}
        </div>
      </section>
    </main>
  );
}

export function OpsFlowApp() {
  const [session, setSession] = useState<{ role: Role; name: string } | null>(null);
  const [view, setView] = useState<View>("dashboard");
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [modal, setModal] = useState<ModalKind>(null);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [assetFilter, setAssetFilter] = useState("All statuses");
  const [incidentFilter, setIncidentFilter] = useState("All incidents");
  const [assets, setAssets] = useState(initialAssets);
  const [incidents, setIncidents] = useState(initialIncidents);
  const [articles, setArticles] = useState(initialArticles);
  const [selectedIncident, setSelectedIncident] = useState<Incident | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const canEdit = session?.role !== "Viewer";
  const userInitials = session?.name.split(" ").map((part) => part[0]).join("") ?? "PS";

  const showToast = (message: string) => {
    setToast(message);
    window.setTimeout(() => setToast(null), 2600);
  };

  const filteredAssets = useMemo(() => assets.filter((asset) => {
    const matchesSearch = `${asset.tag} ${asset.name} ${asset.category} ${asset.location} ${asset.department}`.toLowerCase().includes(search.toLowerCase());
    const matchesFilter = assetFilter === "All statuses" || asset.status === assetFilter;
    return matchesSearch && matchesFilter;
  }), [assets, assetFilter, search]);

  const filteredIncidents = useMemo(() => incidents.filter((incident) => {
    const matchesSearch = `${incident.number} ${incident.title} ${incident.service}`.toLowerCase().includes(search.toLowerCase());
    const matchesFilter = incidentFilter === "All incidents" || incident.severity === incidentFilter || incident.status === incidentFilter;
    return matchesSearch && matchesFilter;
  }), [incidents, incidentFilter, search]);

  if (!session) return <Login onLogin={(role, name) => setSession({ role, name })} />;

  const changeView = (next: View) => {
    setView(next);
    setSearch("");
    setSidebarOpen(false);
  };

  const createAsset = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setAssets((current) => [{
      tag: `AST-${1050 + current.length}`,
      name: String(data.get("name")),
      category: String(data.get("category")),
      location: String(data.get("location")),
      department: String(data.get("department")),
      quantity: Number(data.get("quantity")),
      threshold: Number(data.get("threshold")),
      status: "In stock",
      warranty: "Jul 17, 2029",
    }, ...current]);
    setModal(null);
    showToast("Asset created and audit event recorded");
  };

  const createIncident = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const created: Incident = {
      number: `INC-2026-${String(148 + incidents.length).padStart(4, "0")}`,
      title: String(data.get("title")),
      description: String(data.get("description")),
      severity: data.get("severity") as Incident["severity"],
      status: "New",
      service: String(data.get("service")),
      assignee: String(data.get("assignee")),
      age: "Just now",
      updated: "Just now",
      timeline: [{ time: "Just now", author: session.name, body: "Incident reported and triage started." }],
    };
    setIncidents((current) => [created, ...current]);
    setModal(null);
    showToast(`${created.number} created successfully`);
  };

  const createArticle = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const created: Article = {
      id: `KB-${String(88 + articles.length).padStart(4, "0")}`,
      title: String(data.get("title")),
      summary: String(data.get("summary")),
      service: String(data.get("service")),
      tags: String(data.get("tags")).split(",").map((tag) => tag.trim()).filter(Boolean),
      author: session.name,
      status: "Draft",
      reviewed: "Jul 17, 2026",
      helpful: 0,
    };
    setArticles((current) => [created, ...current]);
    setModal(null);
    showToast("Knowledge draft created");
  };

  const updateIncidentStatus = (incident: Incident, status: Incident["status"]) => {
    setIncidents((current) => current.map((item) => item.number === incident.number ? { ...item, status, updated: "Just now", timeline: [...item.timeline, { time: "Just now", author: session.name, body: `Status changed to ${status}.` }] } : item));
    setSelectedIncident((current) => current?.number === incident.number ? { ...current, status, timeline: [...current.timeline, { time: "Just now", author: session.name, body: `Status changed to ${status}.` }] } : current);
    showToast(`${incident.number} moved to ${status}`);
  };

  return (
    <div className="app-shell">
      {sidebarOpen && <button className="sidebar-backdrop" aria-label="Close navigation" onClick={() => setSidebarOpen(false)} />}
      <aside className={`sidebar ${sidebarOpen ? "sidebar-open" : ""}`}>
        <div className="brand"><LogoMark /><span>OpsFlow</span><button aria-label="Close menu" onClick={() => setSidebarOpen(false)}><X size={19} /></button></div>
        <div className="workspace-switcher"><span className="workspace-icon">OF</span><span><small>WORKSPACE</small><strong>Northstar Operations</strong></span><ChevronDown size={15} /></div>
        <nav aria-label="Primary navigation">
          <p className="nav-label">OPERATIONS</p>
          {navItems.slice(0, 4).map((item) => <NavButton key={item.id} item={item} active={view === item.id} onClick={() => changeView(item.id)} />)}
          <p className="nav-label nav-label-spaced">KNOWLEDGE & CONTROL</p>
          {navItems.slice(4).map((item) => <NavButton key={item.id} item={item} active={view === item.id} onClick={() => changeView(item.id)} />)}
        </nav>
        <div className="sidebar-spacer" />
        <div className="support-card"><span><Sparkles size={17} /></span><strong>OpsFlow demo</strong><p>Explore the connected incident-to-knowledge workflow.</p><button onClick={() => changeView("knowledge")}>View knowledge base <ArrowRight size={14} /></button></div>
        <div className="sidebar-footer"><button><Settings size={18} /> Settings</button><button><HelpCircle size={18} /> Help center</button></div>
      </aside>

      <main className="app-main">
        <header className="topbar">
          <button className="mobile-menu" onClick={() => setSidebarOpen(true)} aria-label="Open menu"><Menu size={21} /></button>
          <div className="global-search"><Search size={17} /><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search OpsFlow..." /><kbd>⌘ K</kbd></div>
          <div className="top-actions">
            <button className="icon-button" onClick={() => setNotificationsOpen(!notificationsOpen)} aria-label="Notifications"><Bell size={19} /><span className="notification-dot" /></button>
            <div className="header-divider" />
            <div className="profile-menu"><span className="avatar">{userInitials}</span><span className="profile-copy"><strong>{session.name}</strong><small>{session.role}</small></span><ChevronDown size={15} /></div>
          </div>
          {notificationsOpen && <Notifications onClose={() => setNotificationsOpen(false)} />}
        </header>

        <div className="page-wrap">
          <section className="page-heading">
            <div><p>{viewMeta[view].eyebrow}</p><h1>{viewMeta[view].title}</h1><span>{viewMeta[view].description}</span></div>
            <div className="heading-actions">
              {view === "dashboard" && <button className="button button-secondary" onClick={() => changeView("audit")}><History size={16} /> View activity</button>}
              {view === "assets" && canEdit && <button className="button button-primary" onClick={() => setModal("asset")}><Plus size={17} /> Add asset</button>}
              {view === "incidents" && canEdit && <button className="button button-primary" onClick={() => setModal("incident")}><Plus size={17} /> Report incident</button>}
              {view === "services" && session.role === "Administrator" && <button className="button button-primary" onClick={() => showToast("Service creation is available through the API")}><Plus size={17} /> Add service</button>}
              {view === "knowledge" && canEdit && <button className="button button-primary" onClick={() => setModal("article")}><Plus size={17} /> New article</button>}
              {view === "audit" && <button className="button button-secondary"><ArrowDownRight size={16} /> Export CSV</button>}
            </div>
          </section>

          {view === "dashboard" && <Dashboard assets={assets} incidents={incidents} articles={articles} onView={changeView} onOpenIncident={setSelectedIncident} />}
          {view === "assets" && <AssetsView assets={filteredAssets} search={search} setSearch={setSearch} filter={assetFilter} setFilter={setAssetFilter} canEdit={canEdit} showToast={showToast} />}
          {view === "incidents" && <IncidentsView incidents={filteredIncidents} search={search} setSearch={setSearch} filter={incidentFilter} setFilter={setIncidentFilter} onOpen={setSelectedIncident} />}
          {view === "services" && <ServicesView search={search} setSearch={setSearch} onViewIncidents={() => changeView("incidents")} />}
          {view === "knowledge" && <KnowledgeView articles={articles} search={search} setSearch={setSearch} setArticles={setArticles} showToast={showToast} />}
          {view === "audit" && <AuditView />}
        </div>
      </main>

      {modal && <CreateModal kind={modal} onClose={() => setModal(null)} onAsset={createAsset} onIncident={createIncident} onArticle={createArticle} />}
      {selectedIncident && <IncidentDrawer incident={incidents.find((item) => item.number === selectedIncident.number) ?? selectedIncident} canEdit={canEdit} onClose={() => setSelectedIncident(null)} onStatus={updateIncidentStatus} onConvert={(incident) => { setSelectedIncident(null); setModal("article"); showToast(`${incident.number} linked to the new article draft`); }} />}
      {toast && <div className="toast"><CheckCircle2 size={18} />{toast}</div>}
    </div>
  );
}

function NavButton({ item, active, onClick }: { item: (typeof navItems)[number]; active: boolean; onClick: () => void }) {
  const Icon = item.icon;
  return <button className={`nav-item ${active ? "active" : ""}`} onClick={onClick}><Icon size={19} /><span>{item.label}</span>{item.count && <b>{item.count}</b>}</button>;
}

function Notifications({ onClose }: { onClose: () => void }) {
  return <div className="notifications-popover"><div><strong>Notifications</strong><button onClick={onClose}><X size={16} /></button></div><button><span className="notice-icon red"><AlertTriangle size={17} /></span><span><strong>SEV-1 incident declared</strong><small>Employee Portal · 43 minutes ago</small></span></button><button><span className="notice-icon amber"><Clock3 size={17} /></span><span><strong>Warranty expires in 12 days</strong><small>ThinkSystem SR650 · AST-0981</small></span></button><button><span className="notice-icon purple"><MessageSquareText size={17} /></span><span><strong>You were mentioned</strong><small>Noah mentioned you in INC-2026-0146</small></span></button><a>View all notifications <ArrowRight size={14} /></a></div>;
}

function Dashboard({ assets, incidents, articles, onView, onOpenIncident }: { assets: Asset[]; incidents: Incident[]; articles: Article[]; onView: (view: View) => void; onOpenIncident: (incident: Incident) => void }) {
  const open = incidents.filter((item) => item.status !== "Resolved");
  const lowStock = assets.filter((item) => item.quantity <= item.threshold && item.status !== "Retired");
  return <div className="dashboard-grid">
    <section className="incident-banner"><div className="incident-pulse"><span /></div><div><span className="banner-kicker">ACTIVE CRITICAL INCIDENT</span><h2>{incidents[0].title}</h2><p>{incidents[0].number} · {incidents[0].service} · Open for {incidents[0].age}</p></div><div className="banner-owner"><span className="avatar avatar-maya">MC</span><span><small>INCIDENT COMMANDER</small><strong>{incidents[0].assignee}</strong></span></div><button onClick={() => onOpenIncident(incidents[0])}>Open incident <ArrowRight size={16} /></button></section>

    <section className="metric-grid">
      <MetricCard label="Open incidents" value={String(open.length)} helper="1 critical" trend="+1 today" tone="red" icon={LifeBuoy} />
      <MetricCard label="Mean time to resolve" value="2h 18m" helper="Last 30 days" trend="↓ 14%" tone="green" icon={Clock3} />
      <MetricCard label="Assets tracked" value={String(assets.reduce((sum, asset) => sum + asset.quantity, 0))} helper={`${lowStock.length} need attention`} trend="Across 3 offices" tone="purple" icon={Laptop} />
      <MetricCard label="Service health" value="83%" helper="5 of 6 operational" trend="2 degraded" tone="amber" icon={Activity} />
    </section>

    <section className="card incidents-chart-card">
      <CardHeader title="Incident volume" subtitle="Last 7 days" action="View all" onAction={() => onView("incidents")} />
      <div className="chart-legend"><span><i className="dot-purple" />Resolved</span><span><i className="dot-red" />Open</span></div>
      <div className="bar-chart" aria-label="Incident volume chart">
        {[{d:"Fri",r:3,o:1},{d:"Sat",r:2,o:0},{d:"Sun",r:1,o:1},{d:"Mon",r:4,o:1},{d:"Tue",r:3,o:2},{d:"Wed",r:5,o:1},{d:"Thu",r:2,o:3}].map((bar) => <div key={bar.d} className="bar-group"><div className="bar-stack"><span className="bar-open" style={{height:`${bar.o*18}px`}} /><span className="bar-resolved" style={{height:`${bar.r*18}px`}} /></div><small>{bar.d}</small></div>)}
      </div>
    </section>

    <section className="card fleet-card">
      <CardHeader title="Fleet status" subtitle="All tracked inventory" action="View assets" onAction={() => onView("assets")} />
      <div className="fleet-content"><div className="donut"><div><strong>126</strong><small>ASSETS</small></div></div><div className="fleet-legend"><div><span><i className="green" />In service</span><strong>103 <small>82%</small></strong></div><div><span><i className="purple" />In stock</span><strong>14 <small>11%</small></strong></div><div><span><i className="amber" />Maintenance</span><strong>7 <small>6%</small></strong></div><div><span><i className="gray" />Retired</span><strong>2 <small>1%</small></strong></div></div></div>
    </section>

    <section className="card attention-card">
      <CardHeader title="Needs attention" subtitle="Inventory alerts" action="Review all" onAction={() => onView("assets")} />
      <div className="attention-list"><button onClick={() => onView("assets")}><span className="attention-icon red"><Boxes size={18} /></span><span><strong>Dell Latitude 7440</strong><small>3 in stock · Reorder at 5</small></span><Badge tone="red">Low stock</Badge></button><button onClick={() => onView("assets")}><span className="attention-icon amber"><Clock3 size={18} /></span><span><strong>ThinkSystem SR650</strong><small>Warranty ends Jul 29</small></span><Badge tone="amber">12 days</Badge></button><button onClick={() => onView("assets")}><span className="attention-icon amber"><Clock3 size={18} /></span><span><strong>Cisco Catalyst 9300</strong><small>Warranty ends Sep 04</small></span><Badge tone="neutral">49 days</Badge></button></div>
    </section>

    <section className="card response-card">
      <CardHeader title="Active response" subtitle="Unresolved incidents" action="Open queue" onAction={() => onView("incidents")} />
      <div className="response-table table-scroll"><div className="table-row table-head"><span>Incident</span><span>Service</span><span>Owner</span><span>Status</span><span>Updated</span></div>{incidents.slice(0,3).map((incident) => <button key={incident.number} className="table-row" onClick={() => onOpenIncident(incident)}><span><Badge tone={severityTone(incident.severity)}>{incident.severity}</Badge><span><strong>{incident.title}</strong><small>{incident.number}</small></span></span><span>{incident.service}</span><span><span className="avatar avatar-tiny">{incident.assignee.split(" ").map((part)=>part[0]).join("")}</span>{incident.assignee}</span><span><Badge tone={statusTone(incident.status)}>{incident.status}</Badge></span><span>{incident.updated}<ChevronRight size={15} /></span></button>)}</div>
    </section>

    <section className="card knowledge-card">
      <CardHeader title="Fresh from the knowledge base" subtitle="Recently reviewed" action="Browse all" onAction={() => onView("knowledge")} />
      <div className="knowledge-mini-list">{articles.filter((article) => article.status === "Published").slice(0,3).map((article) => <button key={article.id} onClick={() => onView("knowledge")}><span className="article-icon"><FileText size={18} /></span><span><strong>{article.title}</strong><small>{article.service} · Reviewed {article.reviewed}</small></span><ChevronRight size={16} /></button>)}</div>
    </section>

    <section className="card activity-card">
      <CardHeader title="Recent activity" subtitle="Across OpsFlow" action="Full audit log" onAction={() => onView("audit")} />
      <div className="activity-mini-list">{auditEvents.slice(0,4).map((event, index) => <div key={`${event.entity}-${index}`}><span className={`activity-dot ${event.tone}`} /><p><strong>{event.user}</strong> {event.action} <b>{event.entity}</b><small>{event.time}</small></p></div>)}</div>
    </section>
  </div>;
}

function MetricCard({ label, value, helper, trend, tone, icon: Icon }: { label: string; value: string; helper: string; trend: string; tone: string; icon: typeof Activity }) {
  return <div className="metric-card"><div className={`metric-icon ${tone}`}><Icon size={20} /></div><p>{label}</p><strong>{value}</strong><div><span>{helper}</span><small className={tone === "green" ? "positive" : ""}>{trend}</small></div></div>;
}

function CardHeader({ title, subtitle, action, onAction }: { title: string; subtitle: string; action: string; onAction: () => void }) {
  return <div className="card-header"><div><h2>{title}</h2><p>{subtitle}</p></div><button onClick={onAction}>{action}<ArrowRight size={14} /></button></div>;
}

function Toolbar({ search, setSearch, children }: { search: string; setSearch: (value: string) => void; children?: React.ReactNode }) {
  return <div className="toolbar"><div className="table-search"><Search size={16} /><input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search records..." /></div>{children}<button className="button button-secondary compact"><SlidersHorizontal size={15} /> More filters</button></div>;
}

function AssetsView({ assets, search, setSearch, filter, setFilter, canEdit, showToast }: { assets: Asset[]; search: string; setSearch: (v: string) => void; filter: string; setFilter: (v: string) => void; canEdit: boolean; showToast: (v: string) => void }) {
  return <div className="module-stack">
    <section className="mini-metrics"><div><span className="mini-icon purple"><Laptop size={18} /></span><p><small>Total assets</small><strong>126</strong></p></div><div><span className="mini-icon green"><CheckCircle2 size={18} /></span><p><small>Assigned</small><strong>103</strong></p></div><div><span className="mini-icon red"><AlertTriangle size={18} /></span><p><small>Low stock</small><strong>2</strong></p></div><div><span className="mini-icon amber"><Clock3 size={18} /></span><p><small>Warranty alerts</small><strong>3</strong></p></div></section>
    <section className="card module-card"><Toolbar search={search} setSearch={setSearch}><select value={filter} onChange={(e)=>setFilter(e.target.value)}><option>All statuses</option><option>In stock</option><option>Assigned</option><option>Maintenance</option><option>Retired</option></select></Toolbar><div className="data-table assets-table"><div className="data-row data-head"><span>Asset</span><span>Category</span><span>Location</span><span>Inventory</span><span>Warranty</span><span>Status</span><span /></div>{assets.map((asset) => <div className="data-row" key={asset.tag}><span><span className="asset-thumb">{asset.category === "Server" ? <Server size={20} /> : asset.category === "Network" ? <Network size={20} /> : <Laptop size={20} />}</span><span><strong>{asset.name}</strong><small>{asset.tag} · {asset.department}</small></span></span><span>{asset.category}</span><span>{asset.location}</span><span><strong>{asset.quantity}</strong><small className={asset.quantity <= asset.threshold ? "danger-text" : ""}>Reorder at {asset.threshold}</small></span><span>{asset.warranty}<small>{asset.warranty.includes("2026") ? "Expiring soon" : "Covered"}</small></span><span><Badge tone={statusTone(asset.status)}>{asset.status}</Badge></span><span><button disabled={!canEdit} onClick={() => showToast(`${asset.tag} action menu opened`)}><MoreHorizontal size={18} /></button></span></div>)}</div>{assets.length === 0 && <EmptyState icon={Inbox} title="No assets found" text="Try changing your search or filters." />}<Pagination count={assets.length} /></section>
  </div>;
}

function IncidentsView({ incidents, search, setSearch, filter, setFilter, onOpen }: { incidents: Incident[]; search: string; setSearch: (v:string)=>void; filter:string; setFilter:(v:string)=>void; onOpen:(i:Incident)=>void }) {
  return <div className="module-stack">
    <section className="status-strip"><button className={filter === "All incidents" ? "active" : ""} onClick={()=>setFilter("All incidents")}><strong>{initialIncidents.length}</strong><span>All open</span></button><button className={filter === "SEV-1" ? "active" : ""} onClick={()=>setFilter("SEV-1")}><strong className="red-text">1</strong><span>Critical</span></button><button className={filter === "Investigating" ? "active" : ""} onClick={()=>setFilter("Investigating")}><strong>1</strong><span>Investigating</span></button><button className={filter === "Monitoring" ? "active" : ""} onClick={()=>setFilter("Monitoring")}><strong>1</strong><span>Monitoring</span></button><div className="sla-note"><Clock3 size={17} /><span><strong>1 SLA at risk</strong><small>Review within 17 minutes</small></span></div></section>
    <section className="card module-card"><Toolbar search={search} setSearch={setSearch}><select value={filter} onChange={(e)=>setFilter(e.target.value)}><option>All incidents</option><option>SEV-1</option><option>SEV-2</option><option>SEV-3</option><option>SEV-4</option><option>Investigating</option><option>Monitoring</option></select></Toolbar><div className="incident-list">{incidents.map((incident) => <button key={incident.number} onClick={() => onOpen(incident)} className={`incident-list-item severity-${severityTone(incident.severity)}`}><span className="severity-rail" /><span className="incident-main"><span><Badge tone={severityTone(incident.severity)}>{incident.severity}</Badge><small>{incident.number}</small><span className="updated-dot" /> <small>Updated {incident.updated}</small></span><strong>{incident.title}</strong><p>{incident.description}</p><span className="incident-links"><Badge tone="neutral"><Network size={12} />{incident.service}</Badge><Badge tone={statusTone(incident.status)}><CircleDot size={11} />{incident.status}</Badge></span></span><span className="incident-owner"><span className="avatar">{incident.assignee === "Unassigned" ? "?" : incident.assignee.split(" ").map((p)=>p[0]).join("")}</span><span><small>ASSIGNED TO</small><strong>{incident.assignee}</strong></span></span><span className="incident-age"><small>OPEN FOR</small><strong>{incident.age}</strong></span><ChevronRight size={18} /></button>)}</div>{incidents.length === 0 && <EmptyState icon={LifeBuoy} title="No incidents found" text="Your current filters have no matching incidents." />}<Pagination count={incidents.length} /></section>
  </div>;
}

function ServicesView({ search, setSearch, onViewIncidents }: { search:string; setSearch:(v:string)=>void; onViewIncidents:()=>void }) {
  const filtered = services.filter((service)=>`${service.name} ${service.owner} ${service.status}`.toLowerCase().includes(search.toLowerCase()));
  return <div className="module-stack"><section className="service-summary"><div><span className="health-pulse green" /><p><strong>4 operational</strong><small>All systems normal</small></p></div><div><span className="health-pulse amber" /><p><strong>2 degraded</strong><small>Active investigation</small></p></div><div><span className="health-pulse red" /><p><strong>1 major incident</strong><small>Employee Portal</small></p></div><span>Last health check 32 seconds ago</span></section><section className="card module-card"><Toolbar search={search} setSearch={setSearch}><select><option>All environments</option><option>Production</option><option>Staging</option></select></Toolbar><div className="service-grid">{filtered.map((service) => <article key={service.name} className="service-card"><div className="service-card-top"><span className="service-symbol"><Server size={20} /></span><Badge tone={statusTone(service.status)}><span className={`status-dot ${statusTone(service.status)}`} />{service.status}</Badge></div><h3>{service.name}</h3><p>{service.owner}</p><div className="service-stats"><span><small>30-DAY UPTIME</small><strong>{service.uptime}</strong></span><span><small>OPEN INCIDENTS</small><strong>{service.incidents}</strong></span><span><small>DEPENDENCIES</small><strong>{service.dependencies}</strong></span></div><div className="service-footer"><Badge tone="neutral">{service.environment}</Badge><button onClick={service.incidents ? onViewIncidents : undefined}>{service.incidents ? "View incident" : "View service"}<ArrowRight size={14} /></button></div></article>)}</div></section></div>;
}

function KnowledgeView({ articles, search, setSearch, setArticles, showToast }: { articles: Article[]; search:string; setSearch:(v:string)=>void; setArticles:React.Dispatch<React.SetStateAction<Article[]>>; showToast:(v:string)=>void }) {
  const [tagFilter, setTagFilter] = useState("All articles");
  const filtered = articles.filter((article) => `${article.title} ${article.summary} ${article.service} ${article.tags.join(" ")}`.toLowerCase().includes(search.toLowerCase()) && (tagFilter === "All articles" || article.status === tagFilter));
  return <div className="knowledge-layout"><aside className="knowledge-sidebar"><p>LIBRARY</p>{["All articles","Published","Draft"].map((item)=><button key={item} className={tagFilter===item?"active":""} onClick={()=>setTagFilter(item)}><FileText size={16}/>{item}<span>{item==="All articles"?articles.length:articles.filter(a=>a.status===item).length}</span></button>)}<p>POPULAR TAGS</p>{["runbook","identity","network","queue","email"].map((tag)=><button key={tag} onClick={()=>setSearch(tag)}><Tag size={15}/>{tag}</button>)}<div className="review-reminder"><Clock3 size={18}/><strong>2 reviews due soon</strong><p>Keep operational guidance accurate and trusted.</p><button onClick={()=>showToast("Review queue opened")}>Open review queue</button></div></aside><section className="card module-card knowledge-results"><Toolbar search={search} setSearch={setSearch}><select><option>Recently reviewed</option><option>Most helpful</option><option>A–Z</option></select></Toolbar><p className="result-count">{filtered.length} articles</p><div className="article-list">{filtered.map((article)=><article key={article.id}><span className="article-large-icon"><BookOpen size={21}/></span><div className="article-copy"><span><Badge tone={statusTone(article.status)}>{article.status}</Badge><small>{article.id}</small></span><h3>{article.title}</h3><p>{article.summary}</p><div><Badge tone="neutral"><Network size={12}/>{article.service}</Badge>{article.tags.map((tag)=><Badge key={tag} tone="neutral">#{tag}</Badge>)}</div><footer><span className="avatar avatar-tiny">{article.author.split(" ").map((p)=>p[0]).join("")}</span><span>By {article.author}</span><span>Reviewed {article.reviewed}</span><button onClick={()=>{setArticles(current=>current.map(item=>item.id===article.id?{...item,helpful:item.helpful+1}:item));showToast("Thanks for your feedback");}}><ThumbsUp size={14}/>{article.helpful} found this helpful</button></footer></div><button className="article-open" onClick={()=>showToast(`${article.id} opened`)}><ArrowUpRight size={18}/></button></article>)}</div>{filtered.length===0&&<EmptyState icon={BookOpen} title="No articles found" text="Try a different keyword or library filter."/>}<Pagination count={filtered.length}/></section></div>;
}

function AuditView() {
  return <div className="module-stack"><section className="audit-summary"><div><ShieldCheck size={20}/><span><strong>Audit trail is active</strong><small>Operational records are append-only and retained for 7 years.</small></span></div><span>Last event <strong>2 minutes ago</strong></span></section><section className="card module-card"><div className="toolbar"><div className="table-search"><Search size={16}/><input placeholder="Search user, entity, or action..."/></div><select><option>All entity types</option><option>Incident</option><option>Asset</option><option>Article</option></select><select><option>Today</option><option>Last 7 days</option><option>Last 30 days</option></select><button className="button button-secondary compact"><Filter size={15}/> Filters</button></div><div className="audit-timeline">{auditEvents.map((event,index)=><div key={`${event.entity}-${index}`} className="audit-event"><div className="audit-time">{event.time}</div><div className={`audit-node ${event.tone}`}><span/></div><div className="audit-avatar avatar avatar-small">{event.user==="System"?<Command size={15}/>:event.user.split(" ").map((p)=>p[0]).join("")}</div><div className="audit-copy"><p><strong>{event.user}</strong> {event.action}</p><span><Badge tone="neutral">{event.type}</Badge><b>{event.entity}</b></span></div><button><MoreHorizontal size={18}/></button></div>)}</div><Pagination count={auditEvents.length}/></section></div>;
}

function CreateModal({ kind, onClose, onAsset, onIncident, onArticle }: { kind:Exclude<ModalKind,null>; onClose:()=>void; onAsset:(e:FormEvent<HTMLFormElement>)=>void; onIncident:(e:FormEvent<HTMLFormElement>)=>void; onArticle:(e:FormEvent<HTMLFormElement>)=>void }) {
  const config = { asset:{title:"Add a new asset", subtitle:"Create an inventory record and initial stock entry.", icon:PackagePlus}, incident:{title:"Report an incident",subtitle:"Start triage and notify the assigned support team.",icon:LifeBuoy}, article:{title:"Create knowledge article",subtitle:"Capture a reusable resolution or operating procedure.",icon:BookOpen} }[kind];
  const Icon=config.icon;
  return <div className="modal-layer"><button className="modal-backdrop" onClick={onClose} aria-label="Close dialog"/><section className="modal" role="dialog" aria-modal="true"><header><span><Icon size={21}/></span><div><h2>{config.title}</h2><p>{config.subtitle}</p></div><button onClick={onClose}><X size={19}/></button></header>{kind==="asset"&&<form onSubmit={onAsset}><div className="form-grid"><label className="full">Asset name<input required name="name" placeholder="e.g. MacBook Pro 14-inch"/></label><label>Category<select name="category"><option>Laptop</option><option>Server</option><option>Network</option><option>Mobile</option><option>Monitor</option></select></label><label>Location<select name="location"><option>Boise HQ</option><option>Idaho Falls</option><option>Twin Falls</option><option>Boise Data Center</option></select></label><label>Department<select name="department"><option>Engineering</option><option>Operations</option><option>Infrastructure</option><option>Sales</option></select></label><label>Quantity<input required name="quantity" type="number" min="0" defaultValue="5"/></label><label>Reorder threshold<input required name="threshold" type="number" min="0" defaultValue="2"/></label><label>Warranty expiration<input name="warranty" type="date" defaultValue="2029-07-17"/></label><label className="full">Notes<textarea name="notes" placeholder="Purchase or assignment notes..."/></label></div><ModalFooter onClose={onClose} label="Create asset"/></form>}{kind==="incident"&&<form onSubmit={onIncident}><div className="form-grid"><label className="full">Incident title<input required name="title" placeholder="Describe the user-visible impact"/></label><label>Severity<select name="severity"><option>SEV-1</option><option>SEV-2</option><option>SEV-3</option><option>SEV-4</option></select></label><label>Priority<select name="priority"><option>Urgent</option><option>High</option><option>Normal</option><option>Low</option></select></label><label>Service<select name="service">{services.map(s=><option key={s.name}>{s.name}</option>)}</select></label><label>Assign to<select name="assignee"><option>Maya Chen</option><option>Noah Williams</option><option>Elena Ruiz</option><option>Unassigned</option></select></label><label className="full">Description<textarea required name="description" placeholder="What is happening, who is affected, and when did it begin?"/></label></div><ModalFooter onClose={onClose} label="Create incident"/></form>}{kind==="article"&&<form onSubmit={onArticle}><div className="form-grid"><label className="full">Article title<input required name="title" placeholder="A clear, searchable title"/></label><label>Related service<select name="service">{services.map(s=><option key={s.name}>{s.name}</option>)}</select></label><label>Tags<input name="tags" placeholder="runbook, identity, api"/></label><label className="full">Summary<textarea required name="summary" placeholder="What problem does this article help someone solve?"/></label><label className="full">Resolution steps<textarea name="steps" placeholder="1. Verify...&#10;2. Compare...&#10;3. Restore..."/></label></div><ModalFooter onClose={onClose} label="Save draft"/></form>}</section></div>;
}

function ModalFooter({ onClose, label }: { onClose:()=>void; label:string }) { return <footer className="modal-footer"><button type="button" className="button button-secondary" onClick={onClose}>Cancel</button><button className="button button-primary" type="submit">{label}<ArrowRight size={16}/></button></footer>; }

function IncidentDrawer({ incident, canEdit, onClose, onStatus, onConvert }: { incident:Incident; canEdit:boolean; onClose:()=>void; onStatus:(i:Incident,s:Incident["status"])=>void; onConvert:(i:Incident)=>void }) {
  return <div className="drawer-layer"><button className="drawer-backdrop" onClick={onClose} aria-label="Close incident"/><aside className="incident-drawer"><header><div><span><Badge tone={severityTone(incident.severity)}>{incident.severity}</Badge><small>{incident.number}</small></span><h2>{incident.title}</h2></div><button onClick={onClose}><X size={20}/></button></header><div className="drawer-scroll"><section className="incident-summary"><p>{incident.description}</p><div><span><small>STATUS</small><Badge tone={statusTone(incident.status)}>{incident.status}</Badge></span><span><small>ASSIGNEE</small><strong><span className="avatar avatar-tiny">{incident.assignee.split(" ").map((p)=>p[0]).join("")}</span>{incident.assignee}</strong></span><span><small>AFFECTED SERVICE</small><strong><Network size={14}/>{incident.service}</strong></span><span><small>OPEN FOR</small><strong><Clock3 size={14}/>{incident.age}</strong></span></div></section><section className="drawer-section"><div className="drawer-title"><h3>Response timeline</h3><span>{incident.timeline.length} updates</span></div><div className="response-timeline">{incident.timeline.map((item,index)=><div key={`${item.time}-${index}`}><span className="timeline-avatar avatar avatar-small">{item.author.split(" ").map((p)=>p[0]).join("")}</span><div><p><strong>{item.author}</strong><small>{item.time}</small></p><span>{item.body}</span></div></div>)}</div>{canEdit&&<div className="comment-box"><textarea placeholder="Add an internal update..."/><footer><span>Internal note</span><button onClick={()=>onStatus(incident,incident.status)} className="button button-primary compact">Add update</button></footer></div>}</section><section className="drawer-section"><div className="drawer-title"><h3>Linked records</h3></div><button className="linked-record"><span className="asset-thumb"><Server size={18}/></span><span><strong>ThinkSystem SR650</strong><small>AST-0981 · Boise Data Center</small></span><ExternalLink size={15}/></button></section></div>{canEdit&&<footer className="drawer-actions"><select value={incident.status} onChange={(e)=>onStatus(incident,e.target.value as Incident["status"])}><option>New</option><option>Investigating</option><option>In progress</option><option>Monitoring</option><option>Resolved</option></select><button className="button button-secondary" onClick={()=>onConvert(incident)}><BookOpen size={16}/>Convert to article</button></footer>}</aside></div>;
}

function EmptyState({ icon:Icon,title,text }: { icon:typeof Inbox; title:string; text:string }) { return <div className="empty-state"><Icon size={25}/><strong>{title}</strong><p>{text}</p></div>; }
function Pagination({ count }: { count:number }) { return <div className="pagination"><span>Showing <strong>{count}</strong> results</span><div><button disabled><ChevronLeft size={15}/></button><button className="active">1</button><button>2</button><button>3</button><span>…</span><button>8</button><button><ChevronRight size={15}/></button></div></div>; }
