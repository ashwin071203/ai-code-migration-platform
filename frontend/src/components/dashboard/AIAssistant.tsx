import { useState, type FormEvent } from "react";
import axios from "axios";
import { Sparkles } from "lucide-react";
import { api, authHeaders } from "../../lib/api";
import type { AIResponse } from "../../types";

export function AIAssistant({
  onError,
}: {
  onError: (message: string) => void;
}) {
  const [prompt, setPrompt] = useState("");
  const [response, setResponse] = useState<AIResponse | null>(null);
  const [busy, setBusy] = useState(false);

  const ask = async (event: FormEvent) => {
    event.preventDefault();
    if (!prompt.trim()) return;
    setBusy(true);
    try {
      const result = await api.post<AIResponse>(
        "/api/v1/ai/complete",
        { prompt },
        { headers: authHeaders() },
      );
      setResponse(result.data);
    } catch (requestError) {
      onError(
        axios.isAxiosError(requestError) && requestError.response?.data?.message
          ? requestError.response.data.message
          : "The AI assistant could not be reached. Check the configured provider.",
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="assistant-panel">
      <div className="assistant-copy">
        <span className="eyebrow">MIGRATION ASSISTANT</span>
        <h2>Ask about your next move.</h2>
        <p>
          Use the configured local or compatible AI provider for migration
          guidance.
        </p>
      </div>
      <form className="assistant-form" onSubmit={ask}>
        <textarea
          value={prompt}
          onChange={(event) => setPrompt(event.target.value)}
          placeholder="How should I review a Spring Boot controller before migration?"
          rows={3}
          maxLength={100000}
        />
        <button className="primary-button" disabled={busy || !prompt.trim()}>
          <Sparkles size={16} />
          {busy ? "Thinking..." : "Ask assistant"}
        </button>
      </form>
      {response && (
        <div className="assistant-response">
          <div className="assistant-response-meta">
            {response.provider} / {response.model}
          </div>
          <p>{response.content}</p>
        </div>
      )}
    </section>
  );
}
