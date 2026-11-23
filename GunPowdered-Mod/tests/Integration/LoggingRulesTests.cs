using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GunPowdered.Tests.Integration;

/// <summary>
/// PROJECT_RULES 8 ("with debug on, absolutely everything is tracked", user 2026-11-20) and the user's 2026-11-24 correction ("check that every method
/// has logging for every possible way in which it can go well or bad" + csharp-best-practices Error Handling + debug-first-coding-standards):
/// <list type="bullet">
/// <item>every block-bodied method under src/ (src/Testing included) writes a debug line;</item>
/// <item>EVERY exit - return, continue, break, yield break, throw - logs on its line or on the line right above it in the same block;</item>
/// <item>an exit taken because of an <c>if (...)</c> on its line says why: its log names the condition (generated lines: "because ..."), not only
/// "returns X (line N)";</item>
/// <item>every catch writes a line that is always written (ModLog.Warn / ModLog.Error - errors are never debug-only);</item>
/// <item>a catch-all <c>catch (Exception)</c> is the last resort: a specific catch comes first in the same try (unless the try runs another mod's code,
/// marked "foreign code"), and the catch-all writes the stack trace.</item>
/// </list>
/// Origin: visual-dynamite-kneel (a18g16-1) failed and its log said only "no free ground within reach": FindSpot's three failure reasons were
/// `continue` statements the old check never looked at (it only checked "has a log line" and `return`). Exits inside a lambda written on one line
/// (a sort comparer, an event handler) belong to the lambda, not to the method's flow, and are not checked; expression-bodied members (=>) neither -
/// give them a block body when they decide something.
/// </summary>
[TestFixture]
public class LoggingRulesTests
{
    static readonly Regex Signature = new Regex(@"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|internal|private|protected|static|override|virtual|sealed|unsafe|new|extern)\s+)*([\w<>\[\],\.\?]+(?:\s*<[^>]*>)?)\s+(\w+)\s*(<[^>]*>)?\s*\(.*\)\s*(?:where\s+[^{]*)?$");
    static readonly Regex Exit = new Regex(@"(?<![\w.])(?:yield\s+break;|continue;|break;|throw(?:\s+[^;]*)?;|return(?:\s+[^;]*)?;)");
    static readonly Regex Condition = new Regex(@"\bif\s*\(");
    // the guards in front of log lines ("if (RuleLog.IsOn())", "if (RuleLog.Due(...))") are not conditions of the exit
    static readonly Regex LogGuard = new Regex(@"if \((?:RuleLog|ModLog|CoreLog)\.\w+\((?:""[^""]*"",\s*[\d.]+d?)?\)\)");
    static readonly Regex ReasonlessLog = new Regex(@":\s*(?:returns|skips|leaves|stops)[^""]*\(line \d+\)""");
    static readonly Regex CatchLine = new Regex(@"\bcatch\b\s*(\(\s*([\w.]+)[^)]*\))?");
    static readonly Regex CatchAll = new Regex(@"^\s*(?:System\.)?Exception$");
    static readonly HashSet<string> NotMethods = new HashSet<string> { "if", "for", "foreach", "while", "switch", "using", "lock", "catch", "return", "nameof", "typeof", "new", "else", "get", "set" };

    sealed class Method
    {
        public string File = "";
        public string Name = "";
        public int Open;
        public int Close;
    }

    static IEnumerable<string> SourceFiles() => Directory.GetFiles(TestPaths.Src, "*.cs", SearchOption.AllDirectories)
        .Where(f => !IsLogListener(f) && Path.GetFileName(f) != "RuleLog.cs");

    // code that receives every log line (a BepInEx ILogListener, Unity's logMessageReceived) must never log: each of its own lines would come back to it
    // and loop forever (KydlCore FlightListener, 2026-11-20: the log filled up and Valheim did not start)
    static bool IsLogListener(string file)
    {
        string text = File.ReadAllText(file);
        return text.Contains("ILogListener") || text.Contains("logMessageReceived");
    }

    [Test]
    public void LogListeners_NeverLog()
    {
        var looping = Directory.GetFiles(TestPaths.Src, "*.cs", SearchOption.AllDirectories).Where(IsLogListener)
            .Where(f => File.ReadAllLines(f).Any(l => Logs(Code(l)))).Select(Path.GetFileName).ToList();
        Assert.That(looping, Is.Empty, "a log listener that logs calls itself again for every line it writes (endless loop)");
    }

    static List<Method> Methods(string file, string[] lines)
    {
        var found = new List<Method>();
        for (int i = 0; i + 1 < lines.Length; i++)
        {
            Match m = Signature.Match(lines[i]);
            if (!m.Success || lines[i + 1].Trim() != "{" || NotMethods.Contains(m.Groups[2].Value) || NotMethods.Contains(m.Groups[1].Value) || m.Groups[1].Value.Contains("=")) continue;
            int depth = 0, j = i + 1;
            for (; j < lines.Length; j++)
            {
                string code = Code(lines[j]);
                depth += code.Count(c => c == '{') - code.Count(c => c == '}');
                if (depth == 0) break;
            }
            found.Add(new Method { File = file, Name = m.Groups[2].Value, Open = i + 1, Close = j });
        }
        return found;
    }

    static string Code(string line)
    {
        int c = line.IndexOf("//", System.StringComparison.Ordinal);
        return c >= 0 ? line.Substring(0, c) : line;
    }

    static bool Logs(string text) => text.Contains("ModLog.") || text.Contains("RuleLog.");

    static bool LogsAlways(string text) => text.Contains("ModLog.Warn(") || text.Contains("ModLog.Error(");

    [Test]
    public void EveryMethod_WritesADebugLine()
    {
        var silent = new List<string>();
        foreach (string f in SourceFiles())
        {
            string[] lines = File.ReadAllLines(f);
            foreach (Method m in Methods(f, lines))
                if (!Logs(string.Join("\n", lines.Skip(m.Open).Take(m.Close - m.Open + 1)))) silent.Add(Path.GetFileName(f) + ": " + m.Name);
        }
        Assert.That(silent, Is.Empty, "methods without any ModLog / RuleLog line (PROJECT_RULES 8; _agent/tools/add_method_logging.py adds them)");
    }

    /// <summary>Every exit of the method's own flow, with the code before it on its line (lambdas written on one line are left out).</summary>
    static IEnumerable<(string where, string code, string before, string prev)> Exits()
    {
        foreach (string f in SourceFiles())
        {
            string[] lines = File.ReadAllLines(f);
            foreach (Method m in Methods(f, lines))
                for (int k = m.Open + 1; k < m.Close; k++)
                {
                    string code = Code(lines[k]);
                    if (code.Contains("yield return")) continue;
                    Match e = Exit.Match(code);
                    if (!e.Success) continue;
                    string before = code.Substring(0, e.Index);
                    if (before.Contains("=>")) continue;      // the exit of a lambda on this line
                    yield return (Path.GetFileName(f) + ":" + (k + 1) + " in " + m.Name, code, before, Code(lines[k - 1]).Trim());
                }
        }
    }

    [Test]
    public void EveryExit_IsLogged()
    {
        var silent = Exits().Where(x => !Logs(x.code) && !(Logs(x.prev) && !x.prev.StartsWith("}"))).Select(x => x.where + ": " + x.code.Trim()).ToList();
        Assert.That(silent, Is.Empty, "exits (return / continue / break / yield break / throw) without a log line on them or right above in the same block - no method may stop or skip silently");
    }

    [Test]
    public void ConditionalExit_LogSaysWhy()
    {
        var reasonless = Exits().Where(x => Condition.IsMatch(LogGuard.Replace(x.before, "")) && Logs(x.code) && ReasonlessLog.IsMatch(x.code) && !x.code.Contains(" because "))
            .Select(x => x.where + ": " + x.code.Trim()).ToList();
        Assert.That(reasonless, Is.Empty, "an exit taken because of an if (...) must log that condition, not only what it returns (user 2026-11-24)");
    }

    /// <summary>Each catch: where it is, its type (empty for a bare catch), its body text, whether a specific catch came first in the same try.</summary>
    static IEnumerable<(string where, string type, string line, string body, bool specificBefore)> Catches()
    {
        foreach (string f in SourceFiles())
        {
            string[] lines = File.ReadAllLines(f);
            for (int k = 0; k < lines.Length; k++)
            {
                string code = Code(lines[k]);
                Match c = CatchLine.Match(code);
                if (!c.Success || !Regex.IsMatch(code, @"(^|[\s}])catch\b")) continue;
                int depth = 0, j = k;
                bool opened = false;
                var body = new List<string>();
                for (; j < lines.Length; j++)
                {
                    string t = Code(lines[j]);
                    body.Add(t);
                    depth += t.Count(ch => ch == '{') - t.Count(ch => ch == '}');
                    if (t.Contains("{")) opened = true;
                    if (opened && depth <= 0) break;
                }
                bool specific = false;
                for (int i = k - 1; i >= 0 && i > k - 80; i--)
                {
                    string t = Code(lines[i]).Trim();
                    Match up = CatchLine.Match(t);
                    if (up.Success && Regex.IsMatch(t, @"(^|[\s}])catch\b") && up.Groups[2].Success && !CatchAll.IsMatch(up.Groups[2].Value)) { specific = true; break; }
                    if (Regex.IsMatch(t, @"(^|\s)try\b")) break;
                }
                yield return (Path.GetFileName(f) + ":" + (k + 1), c.Groups[2].Success ? c.Groups[2].Value : "", lines[k], string.Join("\n", body), specific);
            }
        }
    }

    [Test]
    public void EveryCatch_WritesAnAlwaysOnLine()
    {
        var quiet = Catches().Where(c => !LogsAlways(c.body)).Select(c => c.where + ": " + c.line.Trim()).ToList();
        Assert.That(quiet, Is.Empty, "a catch must write ModLog.Warn or ModLog.Error - errors are always written, never only with debug on (debug-first-coding-standards 2)");
    }

    [Test]
    public void CatchAll_IsTheLastResort()
    {
        var bad = new List<string>();
        foreach (var c in Catches())
        {
            bool all = c.type.Length == 0 || CatchAll.IsMatch(c.type);
            if (!all) continue;
            if (!c.specificBefore && !c.line.Contains("foreign code")) bad.Add(c.where + ": no specific catch before the catch-all: " + c.line.Trim());
            if (!c.body.Contains(".StackTrace") && !c.body.Contains(".ToString()")) bad.Add(c.where + ": the catch-all does not write the stack trace: " + c.line.Trim());
        }
        Assert.That(bad, Is.Empty, "catch (Exception) is the last resort: the exception that can happen gets its own catch first, and an unexpected one shows where it came from (csharp-best-practices Error Handling; debug-first-coding-standards 4)");
    }
}
