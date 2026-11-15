#!/usr/bin/env bash
# ./build.sh        native lib for this machine into runtimes/<rid>/native/, plus build/layout, build/frame and build/kit
# ./build.sh win    goome.dll with mingw into runtimes/win-x64/native/
# ./build.sh universal   on a mac, one libgoome.dylib for arm64 + x64 into runtimes/osx-universal/native/ (the unity package uses it)
set -euo pipefail
cd "$(dirname "$0")"
what="${1:-}"
os=$(uname -s)
arch=$(uname -m)
case "$arch" in
x86_64 | amd64) arch=x64 ;;
aarch64 | arm64) arch=arm64 ;;
esac
flags=(-O2 -std=c99 -Wall -Wextra -Wno-unused-parameter -Wno-missing-field-initializers ${WERROR:+-Werror} -Inative)
if [ "$what" = win ] || [[ "$os" == MINGW* || "$os" == MSYS* || "$os" == CYGWIN* ]]; then
  cc="${CC:-x86_64-w64-mingw32-gcc}"
  [ "$what" = win ] || cc="${CC:-gcc}"
  rid=win-x64
  lib=goome.dll
  shared=(-shared -Wl,--export-all-symbols -static-libgcc)
  exe=.exe
elif [ "$os" = Darwin ]; then
  cc="${CC:-cc}"
  rid=osx-$arch
  if [ "$what" = universal ]; then
    rid=osx-universal
    flags+=(-arch arm64 -arch x86_64)
  fi
  lib=libgoome.dylib
  shared=(-dynamiclib -install_name @rpath/libgoome.dylib)
  exe=
else
  cc="${CC:-cc}"
  rid=linux-$arch
  lib=libgoome.so
  shared=(-shared -fPIC)
  exe=
fi
out=runtimes/$rid/native
mkdir -p "$out" build
"$cc" "${flags[@]}" "${shared[@]}" -o "$out/$lib" native/goome_lib.c
"$cc" "${flags[@]}" -o "build/layout$exe" tests/layout.c
"$cc" "${flags[@]}" -o "build/frame$exe" tests/frame.c native/goome_lib.c
"$cc" "${flags[@]}" -o "build/kit$exe" tests/kit.c native/goome_lib.c
echo "built $out/$lib, build/layout$exe, build/frame$exe and build/kit$exe"
