import axios from "axios";

export const API_BASE_URL =
  import.meta.env.VITE_API_URL ?? "http://localhost:5195";

export const api = axios.create({
  baseURL: API_BASE_URL,
});

export function authHeaders() {
  return { Authorization: `Bearer ${localStorage.getItem("migration-token")}` };
}

export function saveSession(user: unknown, token: string) {
  localStorage.setItem("migration-user", JSON.stringify(user));
  localStorage.setItem("migration-token", token);
}

export function clearSession() {
  localStorage.removeItem("migration-token");
  localStorage.removeItem("migration-user");
}
