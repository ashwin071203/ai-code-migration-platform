import { useState, type FormEvent } from "react";
import axios from "axios";
import { ArrowUpRight, Sparkles } from "lucide-react";
import { api } from "../../lib/api";
import type { User } from "../../types";

export function AuthPanel({
  onLogin,
}: {
  onLogin: (user: User, token: string) => void;
}) {
  const [registering, setRegistering] = useState(false);
  const [resetting, setResetting] = useState(false);
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [resetToken, setResetToken] = useState("");
  const [acceptedTerms, setAcceptedTerms] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (resetting && !resetToken)
      return setError("Request a reset token first.");
    if ((registering || resetting) && password !== confirmPassword)
      return setError("Passwords do not match.");
    if (registering && !acceptedTerms)
      return setError("Accept the terms to create an account.");
    setBusy(true);
    try {
      if (resetting) {
        await api.post("/api/v1/auth/reset-password", {
          email,
          token: resetToken,
          newPassword: password,
        });
        setResetting(false);
        setError("Password reset. You can sign in now.");
      } else {
        const response = await api.post(
          registering ? "/api/v1/auth/register" : "/api/v1/auth/login",
          registering ? { fullName, email, password } : { email, password },
        );
        onLogin(response.data.user, response.data.token);
      }
    } catch (requestError) {
      setError(
        axios.isAxiosError(requestError)
          ? (requestError.response?.data?.message ??
              "The request could not be completed.")
          : "The request could not be completed.",
      );
    } finally {
      setBusy(false);
    }
  };
  return (
    <div className="auth-page">
      <div className="auth-art">
        <div className="brand">
          <span className="brand-mark">
            <Sparkles size={17} />
          </span>
          <span>
            Forge<span className="brand-accent">Shift</span>
          </span>
        </div>
        <div className="art-copy">
          <span className="eyebrow light">AI-ASSISTED MODERNIZATION</span>
          <h1>
            Make legacy
            <br />
            <em>feel lighter.</em>
          </h1>
          <p>A precise workspace for the teams rebuilding what matters.</p>
        </div>
        <div className="art-footer">
          Local-first. Evidence-led. Built for engineers.
        </div>
      </div>
      <form className="auth-card" onSubmit={submit}>
        <span className="eyebrow">
          {resetting
            ? "ACCOUNT RECOVERY"
            : registering
              ? "NEW WORKSPACE"
              : "WELCOME BACK"}
        </span>
        <h2>
          {resetting
            ? "Reset your password"
            : registering
              ? "Create your workspace"
              : "Sign in to your workspace"}
        </h2>
        <p className="auth-subtitle">
          {resetting
            ? "Use a one-time reset token from the local recovery flow."
            : registering
              ? "Start with a secure local developer account."
              : "Your migration command center is ready."}
        </p>
        {error && <div className="notice">{error}</div>}
        {registering && (
          <label>
            Full name
            <input
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              placeholder="Alex Morgan"
              required
            />
          </label>
        )}
        {resetting && (
          <label>
            Reset token
            <input
              value={resetToken}
              onChange={(event) => setResetToken(event.target.value)}
              placeholder="Paste your local reset token"
              required
            />
          </label>
        )}
        <label>
          Email address
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="you@company.com"
            required
          />
        </label>
        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder="Enter your password"
            required
          />
        </label>
        {(registering || resetting) && (
          <label>
            Confirm password
            <input
              type="password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              placeholder="Repeat your password"
              required
            />
          </label>
        )}
        {registering && (
          <label className="terms-label">
            <input
              type="checkbox"
              checked={acceptedTerms}
              onChange={(event) => setAcceptedTerms(event.target.checked)}
            />{" "}
            I agree to the local development terms.
          </label>
        )}
        <button className="primary-button full-width" disabled={busy}>
          {busy
            ? "Working..."
            : resetting
              ? "Reset password"
              : registering
                ? "Create account"
                : "Sign in"}{" "}
          <ArrowUpRight size={16} />
        </button>
        <p className="auth-note">
          {!resetting && !registering && (
            <button
              type="button"
              className="auth-switch"
              onClick={async () => {
                setBusy(true);
                try {
                  const response = await api.post(
                    "/api/v1/auth/forgot-password",
                    { email },
                  );
                  setResetToken(response.data.developmentToken ?? "");
                  setResetting(true);
                } catch {
                  setError("The recovery request could not be completed.");
                } finally {
                  setBusy(false);
                }
              }}
            >
              Forgot password?
            </button>
          )}
          <button
            type="button"
            className="auth-switch"
            onClick={() => {
              setResetting(false);
              setRegistering((current) => !current);
              setError("");
            }}
          >
            {resetting
              ? "Back to sign in"
              : registering
                ? "Already have an account? Sign in"
                : "Need an account? Create one"}
          </button>
        </p>
      </form>
    </div>
  );
}
