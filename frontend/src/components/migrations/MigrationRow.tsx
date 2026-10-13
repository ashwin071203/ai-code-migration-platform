import { useEffect, useState } from "react";
import { ArrowUpRight, Download, X } from "lucide-react";
import { api, authHeaders } from "../../lib/api";
import type { Migration, Validation } from "../../types";
import { OutputWorkspace } from "./OutputWorkspace";

export function MigrationRow({
  migration,
  onError,
  onChanged,
}: {
  migration: Migration;
  onError: (message: string) => void;
  onChanged: (migration: Migration) => void;
}) {
  const [busy, setBusy] = useState(false);
  const [validation, setValidation] = useState<Validation | null>(null);
  const [workspaceOpen, setWorkspaceOpen] = useState(false);
  const [detailsOpen, setDetailsOpen] = useState(false);
  useEffect(() => {
    if (!migration.hasOutput) return;
    api
      .get<Validation>(`/api/v1/migrations/${migration.id}/validation`, {
        headers: authHeaders(),
      })
      .then((response) => setValidation(response.data))
      .catch(() => setValidation(null));
  }, [migration.hasOutput, migration.id, migration.status]);
  const download = async () => {
    try {
      const response = await api.get(
        `/api/v1/migrations/${migration.id}/download`,
        {
          headers: authHeaders(),
          responseType: "blob",
        },
      );
      const url = URL.createObjectURL(response.data);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `migration-${migration.id}.zip`;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      onError("The migration output is not available for download.");
    }
  };
  const cancel = async () => {
    setBusy(true);
    try {
      const response = await api.post<Migration>(
        `/api/v1/migrations/${migration.id}/cancel`,
        {},
        { headers: authHeaders() },
      );
      onChanged(response.data);
    } catch {
      onError("This migration could not be cancelled.");
    } finally {
      setBusy(false);
    }
  };
  return (
    <div className="migration-row">
      <div>
        <strong>
          {migration.sourceTechnology}{" "}
          <ArrowUpRight size={13} aria-hidden="true" /> <span>to</span>{" "}
          {migration.targetTechnology}
        </strong>
        <small>
          {migration.status}
          {migration.errorMessage ? ` - ${migration.errorMessage}` : ""}
        </small>
        {validation && (
          <small className="validation-summary">
            Validation: {validation.status} ({validation.filesChecked} files)
          </small>
        )}
      </div>
      <div className="migration-row-actions">
        <span>{migration.progress}%</span>
        <button
          className="download-button migration-details-toggle"
          onClick={() => setDetailsOpen((open) => !open)}
          aria-expanded={detailsOpen}
        >
          {detailsOpen ? "Hide" : "Details"}
        </button>
        {["Created", "PlanReady", "Queued", "Generating"].includes(
          migration.status,
        ) && (
          <button
            className="download-button"
            onClick={cancel}
            disabled={busy}
            title="Cancel migration"
          >
            <X size={15} />
          </button>
        )}
        {migration.hasOutput && (
          <>
            <button
              className="download-button"
              onClick={() => setWorkspaceOpen(true)}
              title="Open generated output"
            >
              <span>Open</span>
            </button>
            <button
              className="download-button"
              onClick={download}
              title="Download generated output"
            >
              <Download size={15} />
            </button>
          </>
        )}
      </div>
      {detailsOpen && (
        <div className="migration-details">
          <div
            className="migration-progress-track"
            role="progressbar"
            aria-label="Migration progress"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={migration.progress}
          >
            <span style={{ width: `${migration.progress}%` }} />
          </div>
          <p>
            {migration.hasOutput
              ? "Generated output is ready to open."
              : ["Queued", "Generating"].includes(migration.status)
                ? "The migration is processing. Output will be available when it finishes."
                : "No generated output is available for this migration."}
          </p>
          {migration.errorMessage && <p>{migration.errorMessage}</p>}
        </div>
      )}
      {workspaceOpen && (
        <OutputWorkspace
          migrationId={migration.id}
          onClose={() => setWorkspaceOpen(false)}
        />
      )}
    </div>
  );
}
