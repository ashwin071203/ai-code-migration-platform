import { useState, type ChangeEvent } from "react";
import { Eye, FileArchive, GitBranch, Trash2 } from "lucide-react";
import { api, authHeaders } from "../../lib/api";
import type { Project } from "../../types";

export function ProjectCard({
  project,
  onUpdated,
  onError,
  onMigrate,
  onDeleted,
  onOpen,
}: {
  project: Project;
  onUpdated: (project: Project) => void;
  onError: (message: string) => void;
  onMigrate: (project: Project) => void;
  onDeleted: () => void;
  onOpen: (project: Project) => void;
}) {
  const [busy, setBusy] = useState(false);
  const upload = async (event: ChangeEvent<HTMLInputElement>) => {
    const archive = event.target.files?.[0];
    if (!archive) return;
    setBusy(true);
    try {
      const formData = new FormData();
      formData.append("archive", archive);
      await api.post(`/api/v1/projects/${project.id}/upload`, formData, {
        headers: authHeaders(),
      });
      const response = await api.get<Project[]>("/api/v1/projects", {
        headers: authHeaders(),
      });
      const updated = response.data.find((item) => item.id === project.id);
      if (updated) onUpdated(updated);
    } catch {
      onError("The ZIP could not be uploaded or analyzed.");
    } finally {
      setBusy(false);
      event.target.value = "";
    }
  };
  const remove = async () => {
    if (!window.confirm(`Delete ${project.name}?`)) return;
    try {
      await api.delete(`/api/v1/projects/${project.id}`, {
        headers: authHeaders(),
      });
      onDeleted();
    } catch {
      onError("The project could not be deleted.");
    }
  };
  return (
    <div className="project-card">
      <span className="status-pill">{project.status}</span>
      <h3>{project.name}</h3>
      <p>{project.description || "No description added."}</p>
      <div className="project-meta">
        <span>{project.sourceLanguage}</span>
        <span>{project.framework}</span>
      </div>
      <button className="open-button" onClick={() => onOpen(project)}>
        <Eye size={14} /> Open
      </button>
      <label className="upload-button">
        <FileArchive size={14} />
        {busy ? "Analyzing..." : "Upload ZIP"}
        <input
          type="file"
          accept=".zip,application/zip"
          onChange={upload}
          disabled={busy}
        />
      </label>
      <button className="migration-button" onClick={() => onMigrate(project)}>
        <GitBranch size={14} /> Configure migration
      </button>
      <button className="delete-button" onClick={remove} title="Delete project">
        <Trash2 size={14} /> Delete
      </button>
    </div>
  );
}
