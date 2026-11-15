#!/usr/bin/env bash
# tools/sync_unity.sh [--plugins]   copies src/GoomeSharp into the unity package and writes the .meta files unity needs
# --plugins also copies runtimes/linux-x64, win-x64 and osx-universal libs into Runtime/Plugins
set -euo pipefail
cd "$(dirname "$0")/.."
pkg=unity/com.goome-ui.goome
dst=$pkg/Runtime/GoomeSharp
mkdir -p "$dst"
rm -f "$dst"/*.cs
for f in src/GoomeSharp/*.cs; do
  { echo "#nullable enable"; cat "$f"; } > "$dst/$(basename "$f")"
done

if [ "${1:-}" = --plugins ]; then
  for pair in linux-x64/native/libgoome.so:x86_64/libgoome.so win-x64/native/goome.dll:x86_64/goome.dll osx-universal/native/libgoome.dylib:macOS/libgoome.dylib; do
    from=runtimes/${pair%%:*}
    to=$pkg/Runtime/Plugins/${pair#*:}
    if [ -f "$from" ]; then
      cp "$from" "$to"
      echo "plugin $to"
    else
      echo "no $from, left $to alone"
    fi
  done
fi

hash() {
  if command -v md5sum > /dev/null; then printf '%s' "$1" | md5sum | cut -c1-32; else printf '%s' "$1" | md5 -q; fi
}

plugin() {
  local editor_os=$1 editor_cpu=$2 linux=$3 mac=$4 win=$5
  cat <<META
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      : Any
    second:
      enabled: 0
      settings:
        Exclude Editor: 0
        Exclude Linux64: $((1 - linux))
        Exclude OSXUniversal: $((1 - mac))
        Exclude Win: 1
        Exclude Win64: $((1 - win))
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: $editor_cpu
        DefaultValueInitialized: true
        OS: $editor_os
  - first:
      Standalone: Linux64
    second:
      enabled: $linux
      settings:
        CPU: $([ "$linux" = 1 ] && echo x86_64 || echo None)
  - first:
      Standalone: OSXUniversal
    second:
      enabled: $mac
      settings:
        CPU: $([ "$mac" = 1 ] && echo AnyCPU || echo None)
  - first:
      Standalone: Win
    second:
      enabled: 0
      settings:
        CPU: None
  - first:
      Standalone: Win64
    second:
      enabled: $win
      settings:
        CPU: $([ "$win" = 1 ] && echo x86_64 || echo None)
  userData: 
  assetBundleName: 
  assetBundleVariant: 
META
}

simple() {
  printf '%s:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' "$1"
}

meta() {
  local path=$1 rel=${1#$pkg/}
  echo "fileFormatVersion: 2"
  echo "guid: $(hash "com.goome-ui.goome/$rel")"
  if [ -d "$path" ]; then
    echo "folderAsset: yes"
    simple DefaultImporter
    return
  fi
  case "$rel" in
  *.cs)
    printf 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    ;;
  *.asmdef) simple AssemblyDefinitionImporter ;;
  package.json) simple PackageManifestImporter ;;
  *.shader)
    printf 'ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    ;;
  *.so) plugin Linux x86_64 1 0 0 ;;
  *.dll) plugin Windows x86_64 0 0 1 ;;
  *.dylib) plugin OSX AnyCPU 0 1 0 ;;
  *) simple TextScriptImporter ;;
  esac
}

find "$pkg" -name '*.meta' | while read -r m; do
  [ -e "${m%.meta}" ] || rm "$m"
done
find "$pkg" -mindepth 1 -not -name '*.meta' -not -path '*/Samples~*' | sort | while read -r f; do
  meta "$f" > "$f.meta"
done
echo "synced $dst and the .meta files"
