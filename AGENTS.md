# AGENTS.md

## Comment policy

Only add comments where absolutely necessary. Prefer clear naming and
self-documenting code over comments. When a comment is required, it
must explain *why* something is done, never *what* the code already
says.

Do **not** add:

- XML doc summaries that restate the type or method name
- Section separator banners (`// ─── ...`)
- Inline comments that narrate the next line of code
- Comments on obvious assertions in tests

Keep comments when:

- The reason for a non-obvious choice or workaround is needed
- The code references a private constant in another file that
  cannot be referenced directly

## Testing Requirements

**ALWAYS** add tests for features and code

- all code needs to have tests covering functionality