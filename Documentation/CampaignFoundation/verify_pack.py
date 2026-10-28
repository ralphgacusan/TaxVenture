"""Internal documentation checks only; never opens Unity or changes game files."""

from pathlib import Path
import re
import sys

PACK = Path(__file__).resolve().parent
EXPECTED = {
    "T01": [], "T02": ["T01"], "T03": ["T02"], "T04": ["T03"],
    "T05": ["T04"], "T06": ["T04"], "T07": ["T05", "T06"],
    "T08": ["T07", "T22"], "T09": ["T07"], "T10": ["T05"],
    "T11": ["T09", "T10"], "T12": ["T09", "T10", "T11"],
    "T13": ["T11", "T12"], "T14": ["T05", "T11", "T12"],
    "T15": ["T06", "T10"],
    "T16": ["T08", "T09", "T11", "T13", "T14", "T15", "T23"],
    "T17": ["T16", "T29"], "T18": ["T17", "T27"], "T19": ["T18"],
    "T20": ["T18"], "T21": ["T19", "T20"],
    "T22": [], "T23": ["T08", "T22"],
    "T24": ["T16"], "T25": ["T31"], "T26": ["T25"],
    "T27": ["T17"], "T28": ["T26"], "T29": ["T28"],
    "T30": ["T24"], "T31": ["T30"],
}
errors = []
documents = sorted(PACK.rglob("*.md"))
ticket_paths = sorted((PACK / "tickets").glob("T*.md"))
texts = {}
for document in documents:
    try:
        texts[document] = document.read_text(encoding="utf-8-sig")
    except (OSError, UnicodeError) as error:
        errors.append(f"Cannot read {document.name}: {error}")

links = 0
for document, body in texts.items():
    for target in re.findall(r"\[[^\]\n]+\]\(([^)\n]+)\)", body):
        target = target.strip().strip("<>")
        if re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*:", target) or target.startswith("#"):
            continue
        target = target.split("#", 1)[0]
        links += 1
        if not (document.parent / target).resolve().exists():
            errors.append(f"Broken link in {document.relative_to(PACK)}: {target}")

actual = {}
word_total = 0
for ticket in ticket_paths:
    match = re.match(r"(T\d{2})-", ticket.name)
    if not match:
        errors.append(f"Invalid ticket filename: {ticket.name}")
        continue
    ticket_id = match.group(1)
    if ticket_id in actual:
        errors.append(f"Duplicate ticket ID: {ticket_id}")
    body = texts.get(ticket, "")
    actual[ticket_id] = ticket
    if not body.startswith(f"# {ticket_id}"):
        errors.append(f"{ticket_id}: heading must identify ticket")
    if not re.search(r"Status:\s*(?:\*\*)?Planned", body, re.I):
        errors.append(f"{ticket_id}: authoring status must remain Planned")
    blocker_line = re.search(r"Blocked by:\s*([^\n]+)", body, re.I)
    blockers = set(re.findall(r"T\d{2}", blocker_line.group(1))) if blocker_line else None
    if blockers != set(EXPECTED.get(ticket_id, [])):
        errors.append(f"{ticket_id}: blockers {blockers} != expected {EXPECTED.get(ticket_id)}")
    if not re.search(r"Internal (?:checks|verification|acceptance)", body, re.I):
        errors.append(f"{ticket_id}: missing internal verification section")
    if not re.search(r"USER(?:-only)? manual", body, re.I):
        errors.append(f"{ticket_id}: missing USER manual acceptance section")
    if "- [ ]" not in body or "- [x]" in body.lower():
        errors.append(f"{ticket_id}: acceptance must be unchecked")
    if not re.search(r"Non-goals|Out of scope|Not included", body, re.I):
        errors.append(f"{ticket_id}: missing explicit non-goals")
    if not re.search(r"Handoff", body, re.I):
        errors.append(f"{ticket_id}: missing handoff")
    if re.search(r"Unity(?:\.exe)?\s+-(?:batchmode|runTests)|capture_screen|Start-Process.*Unity", body, re.I):
        errors.append(f"{ticket_id}: Unity execution belongs to USER, not internal checks")
    words = len(body.split())
    word_total += words
    if words < 400:
        errors.append(f"{ticket_id}: only {words} words; expected a detailed vertical slice")

if set(actual) != set(EXPECTED):
    errors.append(f"Ticket set mismatch: missing={set(EXPECTED)-set(actual)}, extra={set(actual)-set(EXPECTED)}")

fresh_ticket = PACK / "tickets" / "T02-fresh-campaign-and-save.md"
obsolete_ticket = PACK / "tickets" / "T02-campaign-migration-and-archive.md"
if not fresh_ticket.is_file() or obsolete_ticket.exists():
    errors.append("T02 must use the fresh-campaign ticket; remove the superseded archive ticket")
if "Fresh-start decision" not in texts.get(PACK / "README.md", ""):
    errors.append("README must state the fresh-start decision and retained new campaign systems")
if "START FRESH" not in texts.get(PACK / "AGENTS.md", ""):
    errors.append("Agent instructions must make the fresh-start scope explicit")
stale_archive_requirements = (
    r"one-time archive", r"read-only Legacy Archive", r"Legacy Archive sheet",
    r"retains? the separate legacy archive", r"preserved settings/archive",
    r"migration/archive compatibility", r"preserve the legacy mastery archive",
    r"T02-campaign-migration-and-archive\.md",
)
for document, body in texts.items():
    for pattern in stale_archive_requirements:
        if re.search(pattern, body, re.I):
            errors.append(f"Superseded archive requirement in {document.relative_to(PACK)}: {pattern}")

visited, visiting = set(), set()
def visit(ticket_id):
    if ticket_id in visiting:
        errors.append(f"Dependency cycle at {ticket_id}")
        return
    if ticket_id in visited:
        return
    visiting.add(ticket_id)
    for blocker in EXPECTED[ticket_id]:
        visit(blocker)
    visiting.remove(ticket_id)
    visited.add(ticket_id)

for ticket_id in EXPECTED:
    visit(ticket_id)

def ancestors(ticket_id):
    result = set()
    for blocker in EXPECTED[ticket_id]:
        result.add(blocker)
        result.update(ancestors(blocker))
    return result

AMENDMENTS = {f"T{number:02}" for number in range(22, 32)}  # 2026-10-24 / 2026-10-26
# User revision 2026-10-28: T27 follows T17 and gates T18 adoption; T21 still includes all amendments.
DELIVERY_A_AMENDMENTS = AMENDMENTS - {"T27"}
if ancestors("T17") != {f"T{number:02}" for number in range(1, 17)} | DELIVERY_A_AMENDMENTS:
    errors.append("Delivery A gate does not cover its implemented route and amendments")
if ancestors("T21") != {f"T{number:02}" for number in range(1, 21)} | AMENDMENTS:
    errors.append("Delivery B gate does not cover all earlier tickets")

# Approved-pack starting fixtures: these are arithmetic checks, not playtesting.
assert 5 * 60 + 7 * 100 == 1000
assert 3 * 60 + 4 * 100 - 40 - 120 - 2 * 30 == 360
assert 40 + 2 + 4 == 46
assert 40 + 5 * 2 + 7 * 4 + 3 * 2 == 84
assert 40 + 5 * 2 + 7 * 4 - 3 * 8 + 6 == 60 < 65
assert 40 + 5 * 2 + 7 * 4 - 2 * 8 + 2 + 6 == 70 >= 65

if errors:
    for error in errors:
        print(f"FAIL: {error}")
    sys.exit(1)
print(f"PASS: {len(documents)} Markdown documents, {len(actual)} detailed tickets ({word_total:,} words), {links} local links, dependency DAG and delivery gates, fresh-start scope, unchecked acceptance, and balance arithmetic.")
print("Documentation only. No gameplay build, Unity/manual, visual, touch, device or listening verification was performed.")
