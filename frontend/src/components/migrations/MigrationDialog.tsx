import { useEffect, useState } from "react";
import { ArrowUpRight } from "lucide-react";
import { api, authHeaders } from "../../lib/api";
import type { MigrationOption, MigrationPlan, Project } from "../../types";

export function MigrationDialog({
  project,
  onClose,
  onError,
}: {
  project: Project;
  onClose: () => void;
  onError: (message: string) => void;
}) {
  const [options, setOptions] = useState<MigrationOption[]>([]);
  const [selected, setSelected] = useState<MigrationOption | null>(null);
  const [plan, setPlan] = useState<MigrationPlan | null>(null);
  const [busy, setBusy] = useState(true);
  useEffect(() => {
    api
      .get<MigrationOption[]>("/api/v1/migrations/options", {
        headers: authHeaders(),
      })
      .then((response) => setOptions(response.data))
      .catch(() => onError("Migration options could not be loaded."))
      .finally(() => setBusy(false));
  }, [onError]);
  const createPlan = async () => {
    if (!selected) return;
    setBusy(true);
    try {
      const created = await api.post<{ id: string }>(
        "/api/v1/migrations",
        {
          projectId: project.id,
          sourceTechnology: selected.source,
          targetTechnology: selected.target,
        },
        { headers: authHeaders() },
      );
      const response = await api.post<MigrationPlan>(
        `/api/v1/migrations/${created.data.id}/plan`,
        {},
        { headers: authHeaders() },
      );
      setPlan(response.data);
    } catch {
      onError("The migration plan could not be generated.");
    } finally {
      setBusy(false);
    }
  };
  const start = async () => {
    if (!plan) return;
    setBusy(true);
    try {
      await api.post(
        `/api/v1/migrations/${plan.migrationId}/start`,
        {},
        { headers: authHeaders() },
      );
      onClose();
    } catch {
      onError("The migration could not be queued.");
    } finally {
      setBusy(false);
    }
  };
  return (
    <div className="modal-backdrop">
      <div className="modal migration-modal">
        <div className="modal-header">
          <div>
            <span className="eyebrow">MIGRATION CONFIGURATION</span>
            <h2>{project.name}</h2>
          </div>
          <button type="button" className="icon-button" onClick={onClose}>
            ×
          </button>
        </div>
        {busy && !plan ? (
          <p className="auth-subtitle">Loading supported targets...</p>
        ) : plan ? (
          <>
            <div className="migration-summary">
              <strong>{plan.source}</strong>
              <ArrowUpRight size={15} />
              <strong>{plan.target}</strong>
            </div>
            <h3>Review plan</h3>
            <ol className="plan-list">
              {plan.steps.map((step) => (
                <li key={step}>{step}</li>
              ))}
            </ol>
            <h3>Risks and review items</h3>
            <ul className="risk-list">
              {plan.risks.map((risk) => (
                <li key={risk}>{risk}</li>
              ))}
            </ul>
            <div className="modal-actions">
              <button className="secondary-button" onClick={onClose}>
                Cancel
              </button>
              <button
                className="primary-button"
                onClick={start}
                disabled={busy}
              >
                Start migration <ArrowUpRight size={16} />
              </button>
            </div>
          </>
        ) : (
          <>
            <p className="auth-subtitle">
              Targets come from the backend and are limited to supported
              providers.
            </p>
            <div className="option-grid">
              {options.map((option) => (
                <button
                  key={`${option.source}-${option.target}`}
                  className={`option-card ${selected?.target === option.target ? "selected" : ""}`}
                  onClick={() => setSelected(option)}
                >
                  <span>{option.type}</span>
                  <strong>
                    {option.source} <ArrowUpRight size={13} /> {option.target}
                  </strong>
                  <small>{option.description}</small>
                </button>
              ))}
            </div>
            <div className="modal-actions">
              <button className="secondary-button" onClick={onClose}>
                Cancel
              </button>
              <button
                className="primary-button"
                onClick={createPlan}
                disabled={!selected || busy}
              >
                Generate plan <ArrowUpRight size={16} />
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
