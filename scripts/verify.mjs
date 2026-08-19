import process from 'node:process';
import { runTool } from '../.harness/runtime/windows-cli.mjs';

// Bootstrap replaces this marker with commands detected from the project. Keep
// the sequence deterministic: restore -> build/analyze -> format -> tests ->
// dependency/security gates. CI runs this same file.
const steps = [
  {
    "name": "Restore .NET",
    "command": "dotnet",
    "args": [
      "restore",
      "--locked-mode"
    ]
  },
  {
    "name": "Restore frontend",
    "command": "pnpm",
    "args": [
      "install",
      "--frozen-lockfile"
    ],
    "cwd": "web"
  },
  {
    "name": "Build .NET",
    "command": "dotnet",
    "args": [
      "build",
      "--configuration",
      "Release",
      "--no-incremental",
      "--no-restore"
    ]
  },
  {
    "name": "Format .NET whitespace",
    "command": "dotnet",
    "args": [
      "format",
      "whitespace",
      "--verify-no-changes",
      "--no-restore"
    ]
  },
  {
    "name": "Format .NET style",
    "command": "dotnet",
    "args": [
      "format",
      "style",
      "--verify-no-changes",
      "--severity",
      "info",
      "--no-restore"
    ]
  },
  {
    "name": "Lint frontend",
    "command": "pnpm",
    "args": [
      "run",
      "lint"
    ],
    "cwd": "web"
  },
  {
    "name": "Test .NET",
    "command": "dotnet",
    "args": [
      "test",
      "--configuration",
      "Release",
      "--no-build"
    ]
  },
  {
    "name": "Build frontend",
    "command": "pnpm",
    "args": [
      "run",
      "build"
    ],
    "cwd": "web"
  },
  {
    "name": "Gitleaks full-history scan",
    "command": "gitleaks",
    "args": [
      "git",
      "--redact",
      "-v"
    ]
  },
  {
    "name": "GitHub Actions security",
    "command": "zizmor",
    "args": [
      ".github/workflows"
    ]
  }
];

for (const step of steps) {
  console.log(`\n==> ${step.name}`);
  const projectRoot = new URL('..', import.meta.url);
  const options = {
    cwd: step.cwd ? new URL(`${step.cwd}/`, projectRoot) : projectRoot,
    stdio: 'inherit',
    shell: false,
  };
  const result = runTool(step.command, step.args, options);
  if (result.error) {
    console.error(result.error.message);
    process.exit(1);
  }
  if (result.status !== 0) process.exit(result.status ?? 1);
}

console.log('\nVerification passed.');
