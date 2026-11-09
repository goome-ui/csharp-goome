#!/usr/bin/env bash
# tools/update_native.sh [path to c-goome]   copies its goome.h into native/ and writes the commit to native/VERSION
set -euo pipefail
cd "$(dirname "$0")/.."
src="${1:-../c-goome}"
if [ ! -f "$src/goome.h" ]; then
  echo "no goome.h in $src, pass the c-goome folder"
  exit 1
fi
if [ -n "$(git -C "$src" status --porcelain goome.h)" ]; then
  echo "goome.h in $src has changes that arent committed, commit them first"
  exit 1
fi
cp "$src/goome.h" native/goome.h
git -C "$src" rev-parse HEAD > native/VERSION
echo "goome.h from $(cat native/VERSION)"
