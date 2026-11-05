import { setupServer } from "msw/node";

/** API mocks by operationId are added per test with `server.use(...)`; unmocked calls fail the test. */
export const server = setupServer();
