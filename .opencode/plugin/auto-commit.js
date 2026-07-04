import { execSync } from "node:child_process";

export default async () => {
  return {
    "tool.execute.before": async (input) => {
      if (!["edit", "write", "bash"].includes(input.name)) return;

      try {
        execSync("git add -A", {
          cwd: process.cwd(),
          timeout: 15000,
          stdio: "pipe",
        });
      } catch {
        // ignore
      }
    },
  };
};
