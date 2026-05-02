---
mode: agent
description: Convert a DataWeave (.dwl) transformation into equivalent C# mapping code.
---

Convert the provided DataWeave script into idiomatic C#.

Steps:
1. Read the `.dwl` file the user references (default: search `mulesoft/src/main/resources/weave/`).
2. Identify input and output schemas (use `application-types.xml` and sample JSON in `mulesoft/src/main/resources/` when available).
3. Generate C# `record` types for both input and output models under `src/MovieFunctionApp/Models/`.
4. Generate a static mapper class `XxxMapper` with a pure `Map(input)` method.
5. Use `System.Text.Json` attributes (`JsonPropertyName`) to match Mule's JSON field casing exactly.
6. Preserve null-handling semantics from DataWeave (`default`, `?`, `!`).
7. Add a brief XML doc summary citing the source `.dwl` filename.

Do NOT invent fields — if a field's intent is ambiguous, stop and ask the user.
