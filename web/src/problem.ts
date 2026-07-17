/** Pulls a human-readable message out of an ASP.NET ProblemDetails payload. */
export async function problemText(res: Response, fallback: string): Promise<string> {
  const problem = (await res.json().catch(() => null)) as { errors?: Record<string, string[]> } | null
  return problem?.errors ? Object.values(problem.errors).flat().join(' ') : fallback
}
