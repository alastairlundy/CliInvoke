#!/usr/bin/env bash
# CI guard: fails if any .cs file in src/ contains a bare 'await' that is
# missing ConfigureAwait(false).  Whitelists 'await foreach' and 'await using'
# (which cannot accept ConfigureAwait).  No NuGet packages required.
#
# Handles multi-line await expressions by extracting each await statement
# (from 'await' to the next semicolon) and checking for ConfigureAwait(false).
set -euo pipefail

SRC_DIR="${1:-src}"

violations=0

while IFS= read -r -d '' file; do
  # Strip block comments and line comments, then extract await expressions.
  # Each await statement spans from 'await' to the next ';'. We use awk to
  # concatenate continuation lines and output one logical statement per line.
  statements=$(awk '
    /^[[:space:]]*\/\// { next }
    /^[[:space:]]*\*/   { next }
    {
      # Strip leading/trailing whitespace
      gsub(/^[[:space:]]+/, "")
      gsub(/[[:space:]]+$/, "")
    }
    /await[[:space:]]/ {
      # Accumulate until semicolon
      buf = $0
      while (buf !~ /;[[:space:]]*$/ && getline > 0) {
        gsub(/^[[:space:]]+/, "")
        buf = buf " " $0
      }
      print buf
      next
    }
  ' "$file")

  while IFS= read -r stmt; do
    [[ -z "$stmt" ]] && continue

    # Whitelist: await foreach / await using
    [[ "$stmt" =~ await[[:space:]]+foreach ]] && continue
    [[ "$stmt" =~ await[[:space:]]+using ]] && continue

    # If it has 'await' but no ConfigureAwait(false), it is a violation.
    if [[ "$stmt" =~ await[[:space:]] ]] && ! [[ "$stmt" =~ ConfigureAwait\(false\) ]]; then
      echo "::error file=$file::Bare await missing ConfigureAwait(false)"
      echo "  $stmt"
      ((violations++)) || true
    fi
  done <<< "$statements"
done < <(find "$SRC_DIR" -name '*.cs' -print0)

if [[ $violations -gt 0 ]]; then
  echo ""
  echo "::error::$violations bare await(s) found in $SRC_DIR without ConfigureAwait(false)."
  echo "Add .ConfigureAwait(false) to every await in library code."
  exit 1
fi

echo "ConfigureAwait(false) guard passed: no bare awaits in $SRC_DIR."
exit 0
