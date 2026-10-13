import { useEffect, useState } from "react";
import { api, authHeaders } from "../../lib/api";
import type {
  Project,
  ProjectDetail,
  SecurityAnalysis,
  SourceProfile,
} from "../../types";

export function ProjectDetailDialog({
  project,
  onClose,
  onError,
}: {
  project: Project;
  onClose: () => void;
  onError: (message: string) => void;
}) {
  const [detail, setDetail] = useState<ProjectDetail | null>(null);
  const [profile, setProfile] = useState<SourceProfile | null>(null);
  const [security, setSecurity] = useState<SecurityAnalysis | null>(null);
  const [securityBusy, setSecurityBusy] = useState(false);
  useEffect(() => {
    Promise.all([
      api.get<ProjectDetail>(`/api/v1/projects/${project.id}`, {
        headers: authHeaders(),
      }),
      api
        .get<SourceProfile>(`/api/v1/projects/${project.id}/analysis`, {
          headers: authHeaders(),
        })
        .catch(() => null),
    ])
      .then(([detailResponse, profileResponse]) => {
        setDetail(detailResponse.data);
        setProfile(profileResponse?.data ?? null);
      })
      .catch(() => onError("Project details could not be loaded."));
  }, [onError, project.id]);
  const runSecurityScan = async () => {
    setSecurityBusy(true);
    try {
      const response = await api.get<SecurityAnalysis>(
        `/api/v1/projects/${project.id}/security`,
        { headers: authHeaders() },
      );
      setSecurity(response.data);
    } catch {
      onError("The security analysis could not be completed.");
    } finally {
      setSecurityBusy(false);
    }
  };
  return (
    <div className="modal-backdrop">
      <div className="modal detail-modal">
        <div className="modal-header">
          <div>
            <span className="eyebrow">PROJECT DETAIL</span>
            <h2>{project.name}</h2>
          </div>
          <button className="icon-button" onClick={onClose}>
            ×
          </button>
        </div>
        {detail ? (
          <>
            <p className="auth-subtitle">
              {detail.description || "No description added."}
            </p>
            <div className="detail-grid">
              <div>
                <span>Status</span>
                <strong>{detail.status}</strong>
              </div>
              <div>
                <span>Files</span>
                <strong>{detail.fileCount}</strong>
              </div>
              <div>
                <span>Language</span>
                <strong>{profile?.language ?? project.sourceLanguage}</strong>
              </div>
              <div>
                <span>Framework</span>
                <strong>{profile?.framework ?? project.framework}</strong>
              </div>
              <div>
                <span>Build tool</span>
                <strong>{profile?.buildTool ?? "Not analyzed"}</strong>
              </div>
              <div>
                <span>Dependencies</span>
                <strong>{profile?.dependencies ?? "Not analyzed"}</strong>
              </div>
            </div>
            {profile && (
              <>
                <h3>Evidence</h3>
                <ul className="evidence-list">
                  {profile.evidence.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              </>
            )}
            <div className="security-section">
              <div className="section-heading">
                <div>
                  <span className="eyebrow">SECURITY</span>
                  <h3>Source scan</h3>
                </div>
                <button
                  className="secondary-button"
                  onClick={runSecurityScan}
                  disabled={securityBusy}
                >
                  {securityBusy ? "Scanning..." : "Run scan"}
                </button>
              </div>
              {security && (
                <p className="auth-subtitle">
                  {security.findings === 0
                    ? "No matching security patterns found."
                    : `${security.findings} finding(s) across ${security.filesScanned} files.`}
                </p>
              )}
              {security?.results.map((finding) => (
                <div
                  className="security-finding"
                  key={`${finding.relativePath}-${finding.line}-${finding.rule}`}
                >
                  <strong>
                    {finding.severity} / {finding.rule}
                  </strong>
                  <small>
                    {finding.relativePath}:{finding.line}
                  </small>
                  <code>{finding.evidence}</code>
                </div>
              ))}
            </div>
          </>
        ) : (
          <p className="auth-subtitle">Loading project details...</p>
        )}
        <div className="modal-actions">
          <button className="secondary-button" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
