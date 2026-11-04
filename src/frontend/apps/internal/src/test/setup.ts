import { authSession } from "@ats/api-client";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => {
  cleanup();
  authSession.set(null);
});
