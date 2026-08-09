#!/usr/bin/env bash
# PreToolUse Bash guard. Reads the hook payload on stdin, inspects the command,
# and blocks known-dangerous operations by emitting a permissionDecision=deny.
# Anything not matched is left to normal permission handling (no output = allow).
set -euo pipefail

payload="$(cat)"
cmd="$(printf '%s' "$payload" | jq -r '.tool_input.command // empty')"

[ -z "$cmd" ] && exit 0

deny() {
  # Emit a PreToolUse deny decision. jq builds valid JSON regardless of message content.
  jq -n --arg reason "$1" '{
    hookSpecificOutput: {
      hookEventName: "PreToolUse",
      permissionDecision: "deny",
      permissionDecisionReason: $reason
    }
  }'
  exit 0
}

# Each entry: "regex|||human-readable reason". Matched case-insensitively against the command.
patterns=(
  'rm[[:space:]]+(-[a-zA-Z]*[rR][a-zA-Z]*[[:space:]]+)?-?[a-zA-Z]*[fF][a-zA-Z]*[[:space:]]+(/|/\*|~|\$HOME|\.)([[:space:]]|$)|||Recursive force-delete of a root/home/cwd path'
  'rm[[:space:]]+-[a-zA-Z]*[rR][a-zA-Z]*[fF]|rm[[:space:]]+-[a-zA-Z]*[fF][a-zA-Z]*[rR]|||rm -rf style recursive force delete'
  ':\(\)\s*\{.*\|.*&\s*\}|||Fork bomb'
  '\bmkfs\b|||Filesystem format (mkfs)'
  '\bdd\b[^|]*of=/dev/|||Raw write to a block device with dd'
  '>[[:space:]]*/dev/(sd|nvme|disk)|||Redirect over a raw disk device'
  'git[[:space:]]+push[[:space:]].*(--force([[:space:]]|=|$)|[[:space:]]-f([[:space:]]|$))|||git push --force (use --force-with-lease and confirm)'
  'git[[:space:]]+reset[[:space:]]+--hard|||git reset --hard discards local changes'
  'git[[:space:]]+clean[[:space:]]+-[a-zA-Z]*[fdx]|||git clean -fd wipes untracked files'
  'DROP[[:space:]]+(DATABASE|SCHEMA|TABLE)|||SQL DROP DATABASE/SCHEMA/TABLE'
  'TRUNCATE[[:space:]]+TABLE|||SQL TRUNCATE TABLE'
  'dropdb\b|||Postgres dropdb'
  'DELETE[[:space:]]+FROM[[:space:]]+[^;]*;?[[:space:]]*$|||Unqualified DELETE FROM (no WHERE clause) — verify intent'
  'chmod[[:space:]]+-R[[:space:]]+777|||Recursive chmod 777'
  'aws[[:space:]]+s3[[:space:]]+rb\b|aws[[:space:]]+s3[[:space:]]+rm\b.*--recursive|||AWS S3 bucket/recursive delete'
  'kubectl[[:space:]]+delete[[:space:]]+.*--all|||kubectl delete --all'
  'curl[[:space:]].*\|[[:space:]]*(sudo[[:space:]]+)?(ba)?sh|wget[[:space:]].*\|[[:space:]]*(sudo[[:space:]]+)?(ba)?sh|||Piping a downloaded script straight into a shell'
)

shopt -s nocasematch
for entry in "${patterns[@]}"; do
  regex="${entry%%|||*}"
  reason="${entry##*|||}"
  if [[ "$cmd" =~ $regex ]]; then
    deny "Blocked by dangerous-command guard: $reason. Command: $cmd"
  fi
done

exit 0
