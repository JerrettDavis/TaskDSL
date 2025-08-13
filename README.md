# TaskDSL

**TaskDSL** is a compact, human-friendly, and machine-parseable textual syntax for defining highly extensible to-do lists.

It is designed to be:

- **Easy to write by hand** – quick, mnemonic symbols
- **Easy to parse by computer** – deterministic grammar, simple token rules
- **Feature-rich** – recurrence, dependencies, tags, assignments, metadata
- **Scalable** – works for small personal lists and large team workflows

---

## Features

- **Tasks** defined in a single line: `<status> <[id]> <attributes/flags> -- <title>`
- **Statuses**: `O` (open) and `X` (done)
- **Flags**: `!` (priority), `?` (explicitly blocked)
- **Assignees**, **tags (`#tag`)**, **dependencies**, **contexts**
- **Recurrence rules** with a compact syntax (daily, weekly, nth weekday, intervals, start/end dates)
- **Due dates**, **estimates**, **numeric priorities (`p:n`)**
- **Custom metadata** (`meta:key=value`)
- **Ad-hoc bullets** for lightweight note-taking
- **Full C# parser** with xUnit test suite
- **RRULE** generation (iCalendar) and **cron** export for scheduling integrations

---

## Quick Start

```text
O [cleanup] ^jd #work #bgis -- Clean up go-live checklist
O [deploy] ^sam +[cleanup] #work *1mon+2p -- Deploy to production
O [hotfix] ! ^jd #work p:1 =2h >2025-09-01T14:30:00-05:00 meta:ticket=BG-42 -- Ship hotfix

- quick note or to-do without full syntax
~~ crossed-off ad-hoc bullet
````

---

## Syntax Reference

### Status (required)

| Code | Meaning |
| ---- | ------- |
| `O`  | Open    |
| `X`  | Done    |

> Back-compat: `O!` is accepted and interpreted as `O` plus the `!` priority flag.

### IDs

* Format: `[slug]`
* Allowed characters: `A-Z a-z 0-9 _ -`
* Must be unique in a list
* Examples: `[deploy]`, `[task-01]`

### Flags & Attributes

Attributes and flags appear between the **ID** and the `--` title separator (any order).

| Syntax     | Meaning                   | Example                    |
| ---------- | ------------------------- | -------------------------- |
| `!`        | Priority flag             | `O [t] ! -- …`             |
| `?`        | Explicitly marked blocked | `O [t] ? -- …`             |
| `^name`    | Assignee                  | `^jd`, `^"sam j"`          |
| `#tag`     | Tag (canonical)           | `#work`, `#"support team"` |
| `+[id]`    | Depends on another task   | `+[cleanup]`               |
| `*rule`    | Recurrence                | `*mon+2p`, `*day/2+8a+8p`  |
| `>when`    | Due date/time             | `>2025-08-20`, `>fri+5p`   |
| `=dur`     | Estimate                  | `=45m`, `=2h`, `=3d`       |
| `p:n`      | Numeric priority          | `p:1`, `p:3`               |
| `@context` | Context/location          | `@home`, `@"HQ North"`     |
| `meta:k=v` | Arbitrary metadata        | `meta:ticket=BG-42`        |

> **Blocked semantics:** A task is considered blocked if `?` is present **or** any dependency is not `X`/done. The explicit `?` is a manual override; otherwise blocked is computed from dependencies.

### Ad-hoc Bullets

For lightweight notes without full DSL:

| Syntax | Meaning               |
| ------ | --------------------- |
| `- `   | Open bullet item      |
| `~~ `  | Completed bullet item |

Bullets can include inline `#tags`, `@assignees`, and `+[id]` dependencies inside the text. IDs are auto-generated.

```text
- buy milk #errand @jd
~~ set DNS record #infra +[ticket123]
```

### Recurrence Rules

Format:

```
*<freq>[/<interval>][+<time>][@<start>][~<end>|~count:N]
```

**Frequency (`<freq>`):**

* Units: `min`, `hour`, `day`, `week`, `month`, `year`
* Weekdays: `mon`..`sun`
* Nth weekday in month: `1mon`..`5fri`, `lastsat`

**Interval:** Optional `/N` after frequency (`*day/2` → twice a day)

**Times:** `+8a`, `+2p`, `+14:30` (repeat `+` for multiple times)

**Range/Count:** `@YYYY-MM-DD`, `~YYYY-MM-DD`, or `~count:N`

**Examples:**

| Rule                             | Meaning                       |
| -------------------------------- | ----------------------------- |
| `*day/2+8a+8p`                   | Twice daily, 8 AM & 8 PM      |
| `*mon+2p`                        | Every Monday at 2 PM          |
| `*1mon+2p`                       | First Monday each month, 2 PM |
| `*lastfri+17:00`                 | Last Friday monthly, 5 PM     |
| `*month/3@2025-01-01~2025-12-31` | Quarterly in 2025             |

### Due Dates

* Absolute: `>2025-08-20`
* Weekday + time: `>fri+5p`
* Time only: `>14:30` (today/tomorrow based on now)

### Estimates

* `=30m`, `=2h`, `=3d`

### Metadata

* `meta:key=value` (case-insensitive keys)

### Escaping

* Use quotes for multi-word tokens:
  `^"sam j"`, `#"support team"`, `@"HQ North"`
* Escape `--` inside titles with `\--`:
  `-- Fix heating \-- basement and attic`

---

## Parsing in C\#

```csharp
var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
var now = DateTimeOffset.UtcNow;

var task = Parser.ParseLine(
    "O [deploy] +[cleanup] #work *1mon+2p -- Deploy to production",
    tz,
    now
);

Console.WriteLine(task.Id);              // deploy
Console.WriteLine(task.Recurrence.Freq); // 1mon
```

---

## RRULE & Cron

Generate iCalendar RRULE fragments or standard 5-field cron strings:

```csharp
var rrule = Parser.ToRRule(task.Recurrence);
var cron  = Parser.ToCronString(task.Recurrence);
```

> Note: Plain cron can’t express `1mon`/`lastfri` directly; `ToCronString` throws `NotSupportedException` for those.

---

## Testing

* **Unit tests** for all tokens & flags
* **Integration tests** (good/bad payloads)
* **Round-trip tests** (`ParseLine` → `ToString` → `ParseLine`)
* **Pretty-print tests** (console output via `ToPrettyString()`)
* **Performance smoke tests**
* **Property-based fuzzing** (optional via FsCheck)

Run:

```sh
dotnet test
```

---

## Example Full Payload

```text
O [cleanup] ^jd #work #bgis -- Clean up go-live checklist
O [deploy] ^sam +[cleanup] #work *1mon+2p -- Deploy to production
O [hotfix] ! ^jd #work p:1 =2h >2025-09-01T14:30:00-05:00 meta:ticket=BG-42 -- Ship hotfix
O [blocked1] ? +[deploy] #work meta:reason=waiting-approval -- Wait for CAB approval
O [water] #health *day/2+8a+8p -- Drink water
O [finance-report] #finance *lastfri+17:00 -- Run monthly finance report
O [standup] #team *wed+9a+1p -- Team standup sessions
O [quarterly-plan] #planning *month/3@2025-01-01~2025-12-31 -- Quarterly planning
O [today-reminder] >14:30 #reminders -- Ping vendor
O [facilities] #"support team" @"HQ North" -- Replace air filters \-- lobby and east wing

- Call the plumber #home
~~ Take out trash #home
```

---

## Compatibility Notes

* `#tag` is the **canonical** tag syntax.
  Legacy `-tag` input is still accepted but will be rendered as `#tag`.
* `O!` is accepted as **back-compat** and treated as `O` + priority flag `!`.

---

## Contributing

1. Fork the repository
2. Add features or fixes in a branch
3. Add or update tests for all changes
4. Open a PR with a clear description

---

## Roadmap

* [x] Canonicalizer (stable attribute ordering)
* [ ] Serialization back to DSL (`ToDsl`) — if not using `ToString()`
* [ ] Dependency cycle detection
* [ ] Optional natural language date/time parsing
* [ ] Command-line tooling

---

## License

MIT License. See [LICENSE](LICENSE) for details.


