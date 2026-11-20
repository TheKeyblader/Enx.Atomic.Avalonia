using System.Text;
using System.Text.RegularExpressions;
using Enx.Atomic.Avalonia.Preset.Mini;

namespace Enx.Atomic.Avalonia.Tests;

/// <summary>
/// Keeps <c>Sources/Enx.Atomic.Avalonia.Preset.Mini/PARITY.md</c> executable: every row marked ✅/🟡 must
/// list at least one example token and every listed token must actually resolve through the Mini preset,
/// while ⏳/❌ rows must leave <c>Examples</c> empty.
///
/// <para>
/// The file is located by walking up from <see cref="AppContext.BaseDirectory"/> to the repository root
/// (the directory holding <c>Sources/Enx.Atomic.Avalonia.Preset.Mini/PARITY.md</c>), rather than copied into
/// the test output by the csproj: there is then exactly one copy, so a test run can never validate a stale
/// build artefact while the markdown being edited says something else.
/// </para>
/// <para>
/// A row's examples are resolved through <see cref="AtomicGenerator{TTheme}.Generate(string, AtomicGenerator{TTheme}.Options)"/>
/// with a <c>Classes="..."</c> source, i.e. the real extraction path, not a hand-built token list — a typo
/// in a class attribute is exactly what this test is meant to catch. Codegen-only families (grid placement,
/// resource-backed colors) still resolve here; only the CLI emission/application differs.
/// </para>
/// </summary>
public partial class ParityMatrixTests
{
    private const string Ported = "✅";
    private const string Partial = "🟡";
    private const string Dropped = "❌";
    private const string Todo = "⏳";

    /// <summary>One parsed row of a parity table, with the section (<c>## Rules</c>, <c>## Variants</c>, <c>## Theme scales</c>) it belongs to.</summary>
    private sealed record ParityRow(
        string Section,
        string Upstream,
        string Status,
        IReadOnlyList<string> Examples,
        int LineNumber
    )
    {
        public bool IsPorted =>
            Status.StartsWith(Ported, StringComparison.Ordinal) || Status.StartsWith(Partial, StringComparison.Ordinal);

        public string Label => $"{Section}: {Upstream} (PARITY.md:{LineNumber})";
    }

    /// <summary>Parsed once per test run — the markdown is small and the theory data is built from it.</summary>
    private static readonly Lazy<IReadOnlyList<ParityRow>> ParsedRows = new(ParseMatrix);

    /// <summary>One theory case per (row, token), so a broken token fails alone with its row in the test name.</summary>
    public static TheoryData<string, string> PortedExampleTokens
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var row in ParsedRows.Value.Where(row => row.IsPorted))
            foreach (var token in row.Examples)
                data.Add(row.Label, token);
            return data;
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(PortedExampleTokens))]
    public void PortedRow_ExampleTokenResolves(string row, string token)
    {
        var (_, generator) = TestHelpers.CreateMiniGenerator();

        var utils = generator.Generate(
            $"Classes=\"{token}\"",
            new AtomicGenerator<MiniTheme>.Options { Id = "PARITY.md" }
        );

        Assert.True(utils.Length > 0, $"PARITY.md row '{row}' lists `{token}`, but it resolves to no style.");
    }

    [AvaloniaFact]
    public void EveryPortedRow_HasAtLeastOneExample()
    {
        var missing = ParsedRows
            .Value.Where(row => row.IsPorted && row.Examples.Count == 0)
            .Select(row => row.Label)
            .ToArray();

        Assert.True(
            missing.Length == 0,
            "Ported/partial rows need at least one example token:\n" + string.Join('\n', missing)
        );
    }

    [AvaloniaFact]
    public void ExampleTokens_AreSingleTokens()
    {
        // The extractor splits on whitespace, so an example with a space would silently test two tokens and
        // pass for the wrong reason.
        var invalid = ParsedRows
            .Value.SelectMany(row => row.Examples.Select(token => (row, token)))
            .Where(x => x.token.Length == 0 || x.token.Any(char.IsWhiteSpace))
            .Select(x => $"{x.row.Label} -> `{x.token}`")
            .ToArray();

        Assert.True(
            invalid.Length == 0,
            "Example cells must hold single, whitespace-free tokens:\n" + string.Join('\n', invalid)
        );
    }

    [AvaloniaFact]
    public void TodoAndDroppedRows_HaveNoExamples()
    {
        // Guards against a row advertising working tokens while claiming the family is unimplemented.
        var offenders = ParsedRows
            .Value.Where(row =>
                (
                    row.Status.StartsWith(Todo, StringComparison.Ordinal)
                    || row.Status.StartsWith(Dropped, StringComparison.Ordinal)
                )
                && row.Examples.Count > 0
            )
            .Select(row => $"{row.Label} -> {string.Join(", ", row.Examples.Select(token => $"`{token}`"))}")
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Todo/dropped rows must leave Examples empty:\n" + string.Join('\n', offenders)
        );
    }

    [AvaloniaFact]
    public void Counts_MatchSummary()
    {
        var actual = ParsedRows
            .Value.GroupBy(row => (row.Section, Status: StatusKey(row.Status)))
            .ToDictionary(group => group.Key, group => group.Count());

        var summary = ParseSummary();

        foreach (var section in new[] { "Rules", "Variants", "Theme scales" })
        {
            foreach (var status in new[] { Ported, Partial, Dropped, Todo })
            {
                actual.TryGetValue((section, status), out var count);
                Assert.True(
                    summary.TryGetValue((section, status), out var expected),
                    $"PARITY.md's summary table has no '{section}' / '{status}' cell."
                );
                Assert.True(
                    count == expected,
                    $"PARITY.md summary says {section}/{status} = {expected}, but {count} rows are actually marked so."
                );
            }

            var sectionTotal = actual.Where(pair => pair.Key.Section == section).Sum(pair => pair.Value);
            Assert.True(
                summary.TryGetValue((section, "Total"), out var expectedSectionTotal),
                $"PARITY.md's summary table has no '{section}' total."
            );
            Assert.True(
                sectionTotal == expectedSectionTotal,
                $"PARITY.md summary says {section} has {expectedSectionTotal} rows, but {sectionTotal} were parsed."
            );
        }

        var total = ParsedRows.Value.Count;
        foreach (var status in new[] { Ported, Partial, Dropped, Todo })
        {
            var count = actual.Where(pair => pair.Key.Status == status).Sum(pair => pair.Value);
            Assert.True(
                summary.TryGetValue(("Total", status), out var expected),
                $"PARITY.md's total row has no '{status}' cell."
            );
            Assert.True(
                count == expected,
                $"PARITY.md summary says {status} = {expected} in total, but {count} rows are actually marked so."
            );
        }

        Assert.True(
            summary.TryGetValue(("Total", "Total"), out var expectedTotal),
            "PARITY.md's summary has no total row."
        );
        Assert.True(
            total == expectedTotal,
            $"PARITY.md summary says {expectedTotal} rows in total, but {total} were parsed."
        );

        // Every row must belong to exactly one of the counted status buckets.
        Assert.Equal(total, actual.Values.Sum());
    }

    /// <summary>Parses the <c>| Section | ✅ ported | 🟡 partial | ❌ dropped | ⏳ todo | Total |</c> table at the end of the file.</summary>
    private static Dictionary<(string Section, string Status), int> ParseSummary()
    {
        var result = new Dictionary<(string, string), int>();
        string[]? header = null;

        foreach (var line in File.ReadLines(FindParityFile()))
        {
            if (!line.TrimStart().StartsWith('|'))
            {
                header = null;
                continue;
            }

            var cells = SplitCells(line);
            if (header is null)
            {
                if (cells.Count == 6 && cells[0] == "Section")
                    header = cells.ToArray();
                continue;
            }

            if (cells.All(cell => cell.Length == 0 || cell.All(c => c is '-' or ':')))
                continue;

            var section = StripEmphasis(cells[0]);
            for (var column = 1; column < header.Length && column < cells.Count; column++)
            {
                if (!int.TryParse(StripEmphasis(cells[column]), out var value))
                    continue;

                if (column == header.Length - 1)
                {
                    result[(section, "Total")] = value;
                    continue;
                }

                result[(section, StatusKey(header[column]))] = value;
            }
        }

        return result;
    }

    private static string StripEmphasis(string cell) =>
        cell.Replace("*", string.Empty, StringComparison.Ordinal).Trim();

    private static string StatusKey(string status) =>
        status.StartsWith(Ported, StringComparison.Ordinal) ? Ported
        : status.StartsWith(Partial, StringComparison.Ordinal) ? Partial
        : status.StartsWith(Dropped, StringComparison.Ordinal) ? Dropped
        : status.StartsWith(Todo, StringComparison.Ordinal) ? Todo
        : throw new InvalidOperationException($"Unknown status '{status}' in PARITY.md.");

    private static IReadOnlyList<ParityRow> ParseMatrix()
    {
        var rows = new List<ParityRow>();
        var section = string.Empty;
        var inParityTable = false;
        var lineNumber = 0;

        foreach (var line in File.ReadLines(FindParityFile()))
        {
            lineNumber++;

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                section = line[3..].Trim();
                inParityTable = false;
                continue;
            }

            if (!line.TrimStart().StartsWith('|'))
            {
                inParityTable = false;
                continue;
            }

            var cells = SplitCells(line);
            if (!inParityTable)
            {
                if (cells.Count == 5 && cells[0] == "Upstream" && cells[1] == "Status")
                    inParityTable = true;
                continue;
            }

            // Header separator (`|---|---|...`) and anything with the wrong shape.
            if (cells.Count != 5 || cells.All(cell => cell.Length == 0 || cell.All(c => c is '-' or ':')))
                continue;

            var examples = ExampleTokenRegex().Matches(cells[3]).Select(match => match.Groups[1].Value).ToArray();

            rows.Add(new ParityRow(section, cells[0], cells[1], examples, lineNumber));
        }

        Assert.True(rows.Count > 0, $"No parity rows found in {FindParityFile()}.");
        return rows;
    }

    /// <summary>Splits a markdown table row on unescaped <c>|</c> and unescapes <c>\|</c> inside cells.</summary>
    private static List<string> SplitCells(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
            trimmed = trimmed[1..];
        if (trimmed.EndsWith('|'))
            trimmed = trimmed[..^1];

        var cells = new List<string>();
        var current = new StringBuilder();
        for (var i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] == '\\' && i + 1 < trimmed.Length && trimmed[i + 1] == '|')
            {
                current.Append('|');
                i++;
            }
            else if (trimmed[i] == '|')
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(trimmed[i]);
            }
        }

        cells.Add(current.ToString().Trim());
        return cells;
    }

    /// <summary>Walks up from the test binary until the repository root is found — see the class remarks for why.</summary>
    private static string FindParityFile()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            var candidate = Path.Combine(directory.FullName, "Sources", "Enx.Atomic.Avalonia.Preset.Mini", "PARITY.md");
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            $"Could not find Sources/Enx.Atomic.Avalonia.Preset.Mini/PARITY.md above '{AppContext.BaseDirectory}'."
        );
    }

    [GeneratedRegex("`([^`\\r\\n]+)`")]
    private static partial Regex ExampleTokenRegex();
}
