import { useEffect, useState } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { Link, useLocation } from "react-router-dom";
import {
  Activity,
  ArrowUpRight,
  CheckCircle2,
  FolderKanban,
  GitBranch,
  LogOut,
  Plus,
  Search,
  Settings,
  ShieldCheck,
  Sparkles,
  UploadCloud,
} from "lucide-react";
import { AuthPanel } from "./components/auth/AuthPanel";
import { AIAssistant } from "./components/dashboard/AIAssistant";
import { Metric } from "./components/dashboard/Metric";
import { MigrationDialog } from "./components/migrations/MigrationDialog";
import { MigrationRow } from "./components/migrations/MigrationRow";
import { CreateProject } from "./components/projects/CreateProject";
import { ProjectCard } from "./components/projects/ProjectCard";
import { ProjectDetailDialog } from "./components/projects/ProjectDetailDialog";
import {
  AdminPanel,
  ReportsPanel,
  SettingsPanel,
} from "./components/workspace/WorkspacePanels";
import {
  API_BASE_URL,
  api,
  authHeaders,
  clearSession,
  saveSession,
} from "./lib/api";
import type { Migration, Project, User } from "./types";
import "./App.css";

function loadStoredUser(): User | null {
  if (!localStorage.getItem("migration-token")) return null;
  const saved = localStorage.getItem("migration-user");
  if (!saved) return null;
  try {
    return JSON.parse(saved) as User;
  } catch {
    clearSession();
    return null;
  }
}

export default function App() {
  const location = useLocation();
  const [user, setUser] = useState<User | null>(loadStoredUser);
  const [projects, setProjects] = useState<Project[]>([]);
  const [migrations, setMigrations] = useState<Migration[]>([]);
  const [error, setError] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [migrationProject, setMigrationProject] = useState<Project | null>(
    null,
  );
  const [detailProject, setDetailProject] = useState<Project | null>(null);

  useEffect(() => {
    if (!user) return;
    const token = localStorage.getItem("migration-token");
    if (!token) {
      clearSession();
      setUser(null);
      return;
    }
    const headers = { headers: authHeaders() };
    api
      .get<Project[]>("/api/v1/projects", headers)
      .then((response) => setProjects(response.data))
      .catch((requestError) => {
        if (requestError.response?.status === 401) {
          clearSession();
          setUser(null);
          return;
        }
        setError("Connect the API to load your projects.");
      });
    api
      .get<Migration[]>("/api/v1/migrations", headers)
      .then((response) => setMigrations(response.data))
      .catch(() => undefined);
    const connection = new HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/migrations`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    connection.on("MigrationUpdated", (updated: Migration) =>
      setMigrations((current) => [
        updated,
        ...current.filter((item) => item.id !== updated.id),
      ]),
    );
    connection
      .start()
      .then(() =>
        api
          .get<Migration[]>("/api/v1/migrations", headers)
          .then((response) =>
            Promise.all(
              response.data.map((migration) =>
                connection.invoke("Subscribe", migration.id),
              ),
            ),
          )
          .catch(() => undefined),
      )
      .catch(() => undefined);
    return () => {
      connection.stop().catch(() => undefined);
    };
  }, [user]);

  if (!user)
    return (
      <AuthPanel
        onLogin={(nextUser, token) => {
          saveSession(nextUser, token);
          setUser(nextUser);
        }}
      />
    );
  const logout = () => {
    clearSession();
    setUser(null);
  };
  const updateProject = (updated: Project) =>
    setProjects((current) =>
      current.map((item) => (item.id === updated.id ? updated : item)),
    );
  const updateMigration = (updated: Migration) =>
    setMigrations((current) =>
      current.map((item) => (item.id === updated.id ? updated : item)),
    );
  const route =
    location.pathname === "/" ? "overview" : location.pathname.slice(1);
  const pageTitles: Record<string, string> = {
    overview: "Overview",
    projects: "Projects",
    migrations: "Migrations",
    reports: "Reports",
    settings: "Settings",
    admin: "Admin",
  };
  const pageTitle = pageTitles[route] ?? "Overview";
  const isActive = (path: string) => location.pathname === path;

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">
            <Sparkles size={17} />
          </span>
          <span>
            Forge<span className="brand-accent">Shift</span>
          </span>
        </div>
        <div className="workspace-label">WORKSPACE</div>
        <nav>
          <Link className={isActive("/") ? "active" : ""} to="/">
            <Activity size={18} />
            Overview
          </Link>
          <Link
            className={isActive("/projects") ? "active" : ""}
            to="/projects"
          >
            <FolderKanban size={18} />
            Projects
          </Link>
          <Link
            className={isActive("/migrations") ? "active" : ""}
            to="/migrations"
          >
            <GitBranch size={18} />
            Migrations
          </Link>
          <Link className={isActive("/reports") ? "active" : ""} to="/reports">
            <ShieldCheck size={18} />
            Reports
          </Link>
          <Link
            className={isActive("/settings") ? "active" : ""}
            to="/settings"
          >
            <Settings size={18} />
            Settings
          </Link>
          {user.role === "Admin" && (
            <Link className={isActive("/admin") ? "active" : ""} to="/admin">
              <ShieldCheck size={18} />
              Admin
            </Link>
          )}
        </nav>
        <div className="sidebar-bottom">
          <div className="user-chip">
            <div className="avatar">{user.fullName[0]}</div>
            <div>
              <strong>{user.fullName}</strong>
              <small>{user.role}</small>
            </div>
          </div>
          <button className="icon-button" title="Log out" onClick={logout}>
            <LogOut size={17} />
          </button>
        </div>
      </aside>
      <main className="main-content">
        <header className="topbar">
          <div>
            <div className="eyebrow">WORKSPACE / {pageTitle.toUpperCase()}</div>
            <h1>
              {route === "overview"
                ? `Good morning, ${user.fullName.split(" ")[0]}.`
                : pageTitle}
            </h1>
          </div>
          <div className="top-actions">
            <button className="search-button">
              <Search size={16} />
              Search projects
            </button>
            <button className="profile-button">{user.fullName[0]}</button>
          </div>
        </header>
        {error && <div className="notice">{error}</div>}
        {route === "overview" && (
          <section className="hero-strip">
            <div>
              <span className="eyebrow light">MIGRATION CONTROL CENTER</span>
              <h2>
                Move complex systems
                <br />
                <em>with confidence.</em>
              </h2>
              <p>
                Analyze, plan, and modernize your codebase from one focused
                workspace.
              </p>
              <button
                className="primary-button"
                onClick={() => setShowCreate(true)}
              >
                <Plus size={17} />
                Create project <ArrowUpRight size={16} />
              </button>
            </div>
            <div className="hero-orbit">
              <div className="orbit orbit-one" />
              <div className="orbit orbit-two" />
              <Sparkles size={42} />
            </div>
          </section>
        )}
        {route === "overview" && (
          <section className="metrics">
            <Metric
              label="Projects"
              value={projects.length}
              hint="In your workspace"
              icon={<FolderKanban />}
            />
            <Metric
              label="Active migrations"
              value={
                migrations.filter((migration) =>
                  ["Queued", "Generating"].includes(migration.status),
                ).length
              }
              hint="Queued or running"
              icon={<GitBranch />}
            />
            <Metric
              label="Completed"
              value={
                migrations.filter((migration) =>
                  ["ManualReview", "Completed"].includes(migration.status),
                ).length
              }
              hint="Results available"
              icon={<CheckCircle2 />}
            />
            <Metric
              label="Health"
              value="Ready"
              hint="API connection"
              icon={<Activity />}
            />
          </section>
        )}
        {route === "overview" && <AIAssistant onError={setError} />}
        {route === "projects" && (
          <>
            <section className="section-heading">
              <div>
                <span className="eyebrow">YOUR WORKSPACE</span>
                <h2>Recent projects</h2>
              </div>
              <button
                className="text-button"
                onClick={() => setShowCreate(true)}
              >
                New project <Plus size={16} />
              </button>
            </section>
            {projects.length === 0 ? (
              <div className="empty-state">
                <div className="empty-icon">
                  <UploadCloud size={25} />
                </div>
                <h3>Your migration workspace is clear.</h3>
                <p>
                  Create a project to begin profiling a codebase and prepare its
                  first migration plan.
                </p>
                <button
                  className="secondary-button"
                  onClick={() => setShowCreate(true)}
                >
                  Create your first project
                </button>
              </div>
            ) : (
              <div className="project-grid">
                {projects.map((project) => (
                  <ProjectCard
                    key={project.id}
                    project={project}
                    onUpdated={updateProject}
                    onError={setError}
                    onMigrate={setMigrationProject}
                    onDeleted={() =>
                      setProjects((current) =>
                        current.filter((item) => item.id !== project.id),
                      )
                    }
                    onOpen={setDetailProject}
                  />
                ))}
              </div>
            )}
          </>
        )}
        {route === "migrations" && migrations.length > 0 && (
          <section className="migration-feed">
            <div className="section-heading">
              <div>
                <span className="eyebrow">PIPELINE ACTIVITY</span>
                <h2>Recent migrations</h2>
              </div>
            </div>
            {migrations.slice(0, 4).map((migration) => (
              <MigrationRow
                key={migration.id}
                migration={migration}
                onError={setError}
                onChanged={updateMigration}
              />
            ))}
          </section>
        )}
        {route === "reports" && (
          <ReportsPanel migrations={migrations} onError={setError} />
        )}
        {route === "admin" && user.role === "Admin" && (
          <AdminPanel onError={setError} />
        )}
        {route === "settings" && (
          <SettingsPanel user={user} onLogout={logout} />
        )}
      </main>
      {showCreate && (
        <CreateProject
          onClose={() => setShowCreate(false)}
          onCreated={(project) => {
            setProjects((current) => [project, ...current]);
            setShowCreate(false);
          }}
        />
      )}
      {migrationProject && (
        <MigrationDialog
          project={migrationProject}
          onClose={() => setMigrationProject(null)}
          onError={setError}
        />
      )}
      {detailProject && (
        <ProjectDetailDialog
          project={detailProject}
          onClose={() => setDetailProject(null)}
          onError={setError}
        />
      )}
    </div>
  );
}
