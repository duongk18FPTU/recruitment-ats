// @vitest-environment node
import { File } from "node:buffer";
import { afterEach, describe, expect, test, vi } from "vitest";

afterEach(() => vi.unstubAllGlobals());

describe("createApplication", () => {
  test("sends the details and the CV as multipart/form-data, leaving empty optional fields out", async () => {
    // The client reads the origin when it is created, so it is imported after the stub.
    vi.stubGlobal("location", { origin: "http://ats.test" });
    vi.resetModules();
    const { createApplication } = await import("./api");
    let sent: Request | undefined;
    vi.stubGlobal("fetch", async (request: Request) => {
      sent = request;
      return new Response(JSON.stringify({ candidateMatched: false, application: {} }), {
        status: 201,
        headers: { "content-type": "application/json" },
      });
    });

    await createApplication({
      positionId: "0190a1b2-0000-7000-8000-0000000000a1",
      fullName: "Trần Thị Lan",
      email: "lan.tran@example.test",
      phone: "",
      source: "OTHER",
      sourceNote: "Hội chợ việc làm",
      notes: "",
      cv: new File(["%PDF-1.7"], "cv-tran-thi-lan.pdf", { type: "application/pdf" }) as unknown as globalThis.File,
    });

    expect(sent?.method).toBe("POST");
    expect(new URL(sent!.url).pathname).toBe("/api/applications");
    expect(sent?.headers.get("content-type")).toMatch(/^multipart\/form-data; boundary=/);
    const form = await sent!.formData();
    expect([...form.keys()].sort()).toEqual(["cv", "email", "fullName", "positionId", "source", "sourceNote"]);
    expect(form.get("fullName")).toBe("Trần Thị Lan");
    expect(form.get("source")).toBe("OTHER");
    const cv = form.get("cv") as File;
    expect(cv.name).toBe("cv-tran-thi-lan.pdf");
    expect(cv.type).toBe("application/pdf");
    expect(await cv.text()).toBe("%PDF-1.7");
  });
});
