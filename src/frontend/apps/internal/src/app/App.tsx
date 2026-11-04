import { HashRouter } from "react-router";
import { AppProviders } from "./providers";
import { AppRoutes } from "./router";
import "./app.css";

export function App() {
  return (
    <HashRouter>
      <AppProviders>
        <AppRoutes />
      </AppProviders>
    </HashRouter>
  );
}
