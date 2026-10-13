export type Project = {
  id: string;
  name: string;
  description: string;
  sourceLanguage: string;
  framework: string;
  status: string;
};

export type ProjectDetail = Project & {
  fileCount: number;
  hasAnalysis: boolean;
  updatedAt: string;
};

export type SourceProfile = {
  language: string;
  languageVersion: string;
  framework: string;
  frameworkVersion: string;
  projectType: string;
  buildTool: string;
  files: number;
  dependencies: number;
  evidence: string[];
};

export type User = { fullName: string; email: string; role: string };

export type MigrationOption = {
  type: string;
  source: string;
  target: string;
  description: string;
};

export type MigrationPlan = {
  migrationId: string;
  source: string;
  target: string;
  status: string;
  steps: string[];
  risks: string[];
};

export type Migration = {
  id: string;
  projectId: string;
  sourceTechnology: string;
  targetTechnology: string;
  status: string;
  progress: number;
  errorMessage?: string;
  hasOutput: boolean;
};

export type AIResponse = {
  content: string;
  provider: string;
  model: string;
  promptTokens?: number;
  completionTokens?: number;
};
export type SecurityFinding = {
  rule: string;
  severity: string;
  relativePath: string;
  line: number;
  evidence: string;
};

export type SecurityAnalysis = {
  status: string;
  filesScanned: number;
  findings: number;
  results: SecurityFinding[];
};

export type Validation = {
  status: string;
  filesChecked: number;
  errors: number;
  warnings: number;
  summary?: string;
};
