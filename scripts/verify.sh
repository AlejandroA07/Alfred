#!/bin/sh
set -eu

root="$(cd "$(dirname "$0")/.." && pwd)"
exec node "$root/scripts/verify.mjs"
