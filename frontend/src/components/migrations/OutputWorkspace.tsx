import { useEffect, useState } from "react";
import Editor from "@monaco-editor/react";
import { api, authHeaders } from "../../lib/api";

export function OutputWorkspace({
  migrationId,
  onClose,
}: {
  migrationId: string;
  onClose: () => void;
}) {
  const [files, setFiles] = useState<string[]>([]);
  const [selected, setSelected] = useState("");
  const [content, setContent] = useState("");
  const [loading, setLoading] = useState(false);
  useEffect(() => {
    api
      .get<string[]>(`/api/v1/migrations/${migrationId}/files`, {
        headers: authHeaders(),
      })
      .then((response) => {
        setFiles(response.data);
        setSelected(response.data[0] ?? "");
      })
      .catch(() => setFiles([]));
  }, [migrationId]);
  useEffect(() => {
    if (!selected) return;
    setLoading(true);
    api
      .get<{ content: string }>(
        `/api/v1/migrations/${migrationId}/files/${selected}`,
        { headers: authHeaders() },
      )
      .then((response) => setContent(response.data.content))
      .catch(() => setContent("Unable to load this file."))
      .finally(() => setLoading(false));
  }, [migrationId, selected]);
  return (
    <div className="modal-backdrop">
      <div className="output-workspace">
        <div className="modal-header">
          <div>
            <span className="eyebrow">GENERATED OUTPUT</span>
            <h2>Review workspace</h2>
          </div>
          <button className="icon-button" onClick={onClose}>
            ×
          </button>
        </div>
        <div className="output-layout">
          <aside className="output-files">
            {files.map((file) => (
              <button
                key={file}
                className={file === selected ? "selected" : ""}
                onClick={() => setSelected(file)}
              >
                {file}
              </button>
            ))}
          </aside>
          <div className="output-editor">
            {loading ? (
              <p className="auth-subtitle">Loading file...</p>
            ) : (
              <Editor
                height="60vh"
                language={languageFor(selected)}
                theme="vs"
                value={content}
                options={{
                  readOnly: true,
                  minimap: { enabled: false },
                  wordWrap: "on",
                  fontSize: 13,
                }}
              />
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function languageFor(path: string) {
  const extension = path.split(".").pop()?.toLowerCase();
  return extension === "cs"
    ? "csharp"
    : extension === "java"
      ? "java"
      : extension === "json"
        ? "json"
        : extension === "md"
          ? "markdown"
          : extension === "ts" || extension === "tsx"
            ? "typescript"
            : extension === "js" || extension === "jsx"
              ? "javascript"
              : "plaintext";
}
