import { useEffect, useState } from "react";
import { api, authHeaders } from "../../lib/api";
import type { Migration, User, Validation } from "../../types";

type AdminUser = User & { id: string; isEnabled: boolean; createdAt: string };
type AuditEntry = {
  id: string;
  action: string;
  entityType?: string;
  createdAt: string;
};

export function ReportsPanel({
  migrations,
  onError,
}: {
  migrations: Migration[];
  onError: (message: string) => void;
}) {
  const [validation, setValidation] = useState<Record<string, Validation>>({});
  useEffect(() => {
    const completed = migrations.filter((migration) => migration.hasOutput);
    Promise.all(
      completed.map(async (migration) => {
        try {
          const response = await api.get<Validation>(
            `/api/v1/migrations/${migration.id}/validation`,
            { headers: authHeaders() },
          );
          return [migration.id, response.data] as const;
        } catch {
          return null;
        }
      }),
    ).then((results) =>
      setValidation(
        Object.fromEntries(
          results.filter(
            (result): result is [string, Validation] => result !== null,
          ),
        ),
      ),
    );
  }, [migrations]);
  return (
    <section id="reports" className="workspace-panel">
      <div className="section-heading">
        <div>
          <span className="eyebrow">REPORTS</span>
          <h2>Migration history</h2>
        </div>
      </div>
      {migrations.length === 0 ? (
        <p className="auth-subtitle">
          Completed migration reports will appear here.
        </p>
      ) : (
        <div className="report-list">
          {migrations.map((migration) => (
            <div className="report-row" key={migration.id}>
              <div>
                <strong>
                  {migration.sourceTechnology} <span>to</span>{" "}
                  {migration.targetTechnology}
                </strong>
                <small>
                  {migration.status}{" "}
                  {validation[migration.id]
                    ? `/ ${validation[migration.id].status}`
                    : ""}
                </small>
              </div>
              <span>{validation[migration.id]?.filesChecked ?? 0} files</span>
            </div>
          ))}
        </div>
      )}
      <button
        className="secondary-button"
        onClick={() =>
          onError("Reports are generated from completed migration output.")
        }
      >
        Refresh reports
      </button>
    </section>
  );
}

export function AdminPanel({
  onError,
}: {
  onError: (message: string) => void;
}) {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [audit, setAudit] = useState<AuditEntry[]>([]);
  useEffect(() => {
    Promise.all([
      api.get<AdminUser[]>("/api/v1/admin/users", { headers: authHeaders() }),
      api.get<AuditEntry[]>("/api/v1/admin/audit", { headers: authHeaders() }),
    ])
      .then(([userResponse, auditResponse]) => {
        setUsers(userResponse.data);
        setAudit(auditResponse.data.slice(0, 8));
      })
      .catch(() => onError("Admin data could not be loaded."));
  }, [onError]);
  const updateUser = async (user: AdminUser, action: "enable" | "disable") => {
    try {
      const response = await api.post<AdminUser>(
        `/api/v1/admin/users/${user.id}/${action}`,
        {},
        { headers: authHeaders() },
      );
      setUsers((current) =>
        current.map((item) => (item.id === user.id ? response.data : item)),
      );
    } catch {
      onError("The user status could not be changed.");
    }
  };
  return (
    <section id="admin" className="workspace-panel">
      <div className="section-heading">
        <div>
          <span className="eyebrow">ADMINISTRATION</span>
          <h2>Users and audit activity</h2>
        </div>
      </div>
      <div className="admin-grid">
        <div>
          <h3>Users</h3>
          {users.map((user) => (
            <div className="admin-row" key={user.id}>
              <div>
                <strong>{user.fullName}</strong>
                <small>
                  {user.email} / {user.role}
                </small>
              </div>
              <button
                className="secondary-button"
                onClick={() =>
                  updateUser(user, user.isEnabled ? "disable" : "enable")
                }
              >
                {user.isEnabled ? "Disable" : "Enable"}
              </button>
            </div>
          ))}
        </div>
        <div>
          <h3>Recent audit events</h3>
          {audit.map((entry) => (
            <div className="audit-row" key={entry.id}>
              <strong>{entry.action}</strong>
              <small>
                {entry.entityType ?? "System"} /{" "}
                {new Date(entry.createdAt).toLocaleString()}
              </small>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

export function SettingsPanel({
  user,
  onLogout,
}: {
  user: User;
  onLogout: () => void;
}) {
  return (
    <section id="settings" className="workspace-panel">
      <div className="section-heading">
        <div>
          <span className="eyebrow">SETTINGS</span>
          <h2>Account settings</h2>
        </div>
      </div>
      <div className="settings-list">
        <div>
          <span>Name</span>
          <strong>{user.fullName}</strong>
        </div>
        <div>
          <span>Email</span>
          <strong>{user.email}</strong>
        </div>
        <div>
          <span>Role</span>
          <strong>{user.role}</strong>
        </div>
      </div>
      <button className="secondary-button" onClick={onLogout}>
        Sign out
      </button>
    </section>
  );
}
