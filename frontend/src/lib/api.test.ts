import { afterEach, describe, expect, it } from "vitest";
import { authHeaders, clearSession, saveSession } from "./api";

describe("session API helpers", () => {
  afterEach(() => localStorage.clear());

  it("stores and clears the authenticated session", () => {
    const user = {
      fullName: "Test User",
      email: "test@example.com",
      role: "Developer",
    };
    saveSession(user, "token-value");
    expect(JSON.parse(localStorage.getItem("migration-user")!)).toEqual(user);
    expect(authHeaders()).toEqual({ Authorization: "Bearer token-value" });
    clearSession();
    expect(localStorage.getItem("migration-user")).toBeNull();
    expect(localStorage.getItem("migration-token")).toBeNull();
  });
});
