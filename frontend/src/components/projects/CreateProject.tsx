import { useState, type FormEvent } from "react";
import { ArrowUpRight } from "lucide-react";
import { api, authHeaders } from "../../lib/api";
import type { Project } from "../../types";

export function CreateProject({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: (project: Project) => void;
}) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const response = await api.post<Project>(
      "/api/v1/projects",
      { name, description },
      { headers: authHeaders() },
    );
    onCreated(response.data);
  };
  return (
    <div className="modal-backdrop">
      <form className="modal" onSubmit={submit}>
        <div className="modal-header">
          <div>
            <span className="eyebrow">NEW WORKSPACE</span>
            <h2>Create a project</h2>
          </div>
          <button type="button" className="icon-button" onClick={onClose}>
            ×
          </button>
        </div>
        <label>
          Project name
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Payments API"
            required
          />
        </label>
        <label>
          Description
          <textarea
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            placeholder="What are you modernizing?"
            rows={3}
          />
        </label>
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>
            Cancel
          </button>
          <button className="primary-button">
            Create project <ArrowUpRight size={16} />
          </button>
        </div>
      </form>
    </div>
  );
}
