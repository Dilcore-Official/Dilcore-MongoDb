#!/usr/bin/env bash
# Verify every roadmap issue (#2–#46) has milestone, labels, acceptance criteria,
# and is linked from ROADMAP.md.
set -euo pipefail

REPO="${REPO:-Dilcore-Official/Dilcore-MongoDb}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ROADMAP="${ROOT}/ROADMAP.md"

if [[ ! -f "${ROADMAP}" ]]; then
  echo "ERROR: ROADMAP.md not found at ${ROADMAP}" >&2
  exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "ERROR: gh CLI is required" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "ERROR: jq is required" >&2
  exit 1
fi

echo "Fetching issues #2–#46 from ${REPO}..."
ISSUES_JSON="$(gh issue list --repo "${REPO}" --state all --limit 200 --json number,title,url,milestone,labels,body)"

fail=0
missing_in_roadmap=0
checked=0

for n in $(seq 2 46); do
  issue="$(jq -c --argjson n "$n" '.[] | select(.number == $n)' <<<"${ISSUES_JSON}")"
  if [[ -z "${issue}" ]]; then
    echo "FAIL #${n}: issue not found"
    fail=$((fail + 1))
    continue
  fi
  checked=$((checked + 1))

  title="$(jq -r '.title' <<<"${issue}")"
  milestone="$(jq -r '.milestone.title // empty' <<<"${issue}")"
  labels="$(jq -r '[.labels[].name] | join(",")' <<<"${issue}")"
  body="$(jq -r '.body // ""' <<<"${issue}")"

  problems=()

  if [[ -z "${milestone}" ]]; then
    problems+=("missing milestone")
  fi
  if [[ ",${labels}," != *",roadmap,"* ]]; then
    problems+=("missing label roadmap")
  fi
  if ! grep -Eq '(^|,)area:' <<<"${labels}"; then
    problems+=("missing area:* label")
  fi
  if ! grep -Eq '(^|,)type:' <<<"${labels}"; then
    problems+=("missing type:* label")
  fi
  if ! grep -Eq '(^|,)priority:' <<<"${labels}"; then
    problems+=("missing priority:* label")
  fi
  if ! grep -Eqi '^##[[:space:]]*Acceptance criteria[[:space:]]*$' <<<"${body}"; then
    problems+=("missing Acceptance criteria section")
  else
    acceptance_section="$(BODY="${body}" python3 - <<'PY'
import os
body = os.environ["BODY"]
lines = body.splitlines()
start = None
for i, line in enumerate(lines):
    if line.strip().lower() == "## acceptance criteria":
        start = i + 1
        break
if start is None:
    raise SystemExit(0)
chunk = []
for line in lines[start:]:
    if line.startswith("## "):
        break
    chunk.append(line)
print("\n".join(chunk))
PY
)"
    if ! grep -Eq '^[[:space:]]*-[[:space:]]*\[[ xX]\][[:space:]]*[^[:space:]].+' <<<"${acceptance_section}"; then
      problems+=("Acceptance criteria section has no non-empty checklist items")
    fi
  fi
  if ! grep -Eqi '^##[[:space:]]*Dependencies[[:space:]]*$' <<<"${body}"; then
    problems+=("missing Dependencies section")
  fi
  if ! grep -Eq "#${n}([^0-9]|$)|/issues/${n}([^0-9]|$)" "${ROADMAP}"; then
    problems+=("not linked from ROADMAP.md")
    missing_in_roadmap=$((missing_in_roadmap + 1))
  fi

  if ((${#problems[@]} > 0)); then
    echo "FAIL #${n} ${title}"
    for p in "${problems[@]}"; do
      echo "  - ${p}"
    done
    fail=$((fail + 1))
  else
    echo "OK   #${n} [${milestone}] ${title}"
  fi
done

echo
echo "Checked ${checked} issues; failures=${fail}; missing ROADMAP links=${missing_in_roadmap}"

if ((fail > 0)); then
  exit 1
fi

echo "Roadmap coverage verification passed."
