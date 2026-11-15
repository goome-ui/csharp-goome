#!/usr/bin/env bash
# tools/update_native.sh [c-goome folder or a goome.h] [commit]   copies goome.h into native/ and writes its commit to native/VERSION
set -euo pipefail
cd "$(dirname "$0")/.."
src="${1:-../c-goome}"
ref="${2:-}"
[ -d "$src" ] && src="$src/goome.h"
dir=$(cd "$(dirname "$src")" && pwd)
file=$(basename "$src")
if ! git -C "$dir" rev-parse HEAD > /dev/null 2>&1; then
  echo "$src isnt in a git checkout, cant tell which commit it is"
  exit 1
fi
if [ -n "$ref" ]; then
  if ! commit=$(git -C "$dir" rev-parse --verify --quiet "$ref^{commit}"); then
    echo "cant find commit $ref in $dir"
    exit 1
  fi
  path="$(git -C "$dir" rev-parse --show-prefix)$file"
  if ! git -C "$dir" cat-file -e "$commit:$path" 2> /dev/null; then
    echo "$commit has no $path"
    exit 1
  fi
  git -C "$dir" show "$commit:$path" > native/goome.h
  echo "$commit" > native/VERSION
else
  if [ ! -f "$src" ]; then
    echo "cant find $src, pass the c-goome folder or a goome.h"
    exit 1
  fi
  if [ -n "$(git -C "$dir" status --porcelain -- "$file")" ]; then
    echo "$src has changes that arent committed, commit them first"
    exit 1
  fi
  cp "$src" native/goome.h
  git -C "$dir" rev-parse HEAD > native/VERSION
fi
echo "goome.h from $(cat native/VERSION)"
