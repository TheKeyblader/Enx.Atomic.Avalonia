---
description: Build, test and format-check the solution — the repo's definition of "done"
---

Verify the working tree the way CI does, and report the outcome faithfully.

1. Run `dotnet csharpier check .`. If it reports unformatted files that *this session* touched,
   run `dotnet csharpier format <file>` on them and re-check. If the only unformatted files are
   ones this session did not touch, don't reformat them — report them.
2. Run `dotnet build Enx.Atomic.Avalonia.slnx -nologo`. It must end with 0 warnings and 0 errors;
   list every warning/error with its `file:line`.
3. Run `dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj --no-build`
   (drop `--no-build` if step 2 changed nothing buildable). Report passed/failed/skipped counts and,
   for each failure, the test name and the assertion message.

If a step fails, stop and diagnose it instead of moving on; never mark the work done while
anything is red. Don't commit, push or update docs as part of this command.

$ARGUMENTS
