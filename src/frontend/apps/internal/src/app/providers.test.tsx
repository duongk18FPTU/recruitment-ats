import { ApiError, authSession } from "@ats/api-client";
import { useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import { act, fireEvent, render, screen } from "@testing-library/react";
import { expect, test } from "vitest";
import { Link, MemoryRouter, Route, Routes } from "react-router";
import { signIn } from "../test/renderApp";
import { AppProviders } from "./providers";

function ForbiddenData() {
  useQuery({ queryKey: ["secret"], queryFn: () => Promise.reject(new ApiError(403)) });
  return <h1>Dữ liệu</h1>;
}

test("a 403 shows #/403 once; the user can then leave it", async () => {
  signIn("INTERVIEWER");
  render(
    <MemoryRouter initialEntries={["/data"]}>
      <AppProviders restoreSession={async () => null}>
        <Routes>
          <Route path="/data" element={<ForbiddenData />} />
          <Route path="/403" element={<Link to="/dashboard">Về trang Tổng quan</Link>} />
          <Route path="/dashboard" element={<h1>Tổng quan</h1>} />
        </Routes>
      </AppProviders>
    </MemoryRouter>,
  );

  const back = await screen.findByRole("link", { name: "Về trang Tổng quan" });
  act(() => fireEvent.click(back));

  expect((await screen.findByRole("heading", { level: 1 })).textContent).toBe("Tổng quan");
});

function ExposeClient({ onClient }: { onClient: (client: QueryClient) => void }) {
  onClient(useQueryClient());
  return null;
}

test("AC-US02 #1: signing out removes every cached record", () => {
  signIn("RECRUITER");
  let client: QueryClient | undefined;
  render(
    <MemoryRouter>
      <AppProviders restoreSession={async () => null}>
        <ExposeClient onClient={(c) => (client = c)} />
      </AppProviders>
    </MemoryRouter>,
  );
  client!.setQueryData(["applications"], [{ id: "a1", candidate: "Lê Hoàng Nam" }]);

  act(() => authSession.set(null));

  expect(client!.getQueryData(["applications"])).toBeUndefined();
});
