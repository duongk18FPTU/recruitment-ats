import { ApiError } from "@ats/api-client";
import { useQuery } from "@tanstack/react-query";
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
