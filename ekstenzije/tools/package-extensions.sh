#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
extensions_root="$repo_root/ekstenzije"
output_root="${1:-$repo_root/artifacts/extensions}"

rm -rf "$output_root"
mkdir -p "$output_root"
output_root="$(cd "$output_root" && pwd)"
work_root="$(mktemp -d)"
trap 'rm -rf "$work_root"' EXIT

package_browser() {
  local browser="$1"
  local label="$2"
  local source_dir="$extensions_root/$browser"
  local stage_dir="$work_root/$browser"
  local output_file="$output_root/SNAPVERE-$label.zip"

  mkdir -p "$stage_dir"
  cp -R "$source_dir/." "$stage_dir/"

  # ZIP stores DOS timestamps. Normalize every staged file/directory to the
  # earliest portable ZIP epoch so two clean builds are byte-for-byte stable.
  find "$stage_dir" -exec touch -t 198001010000 {} +

  (
    cd "$stage_dir"
    find . -type f -print | LC_ALL=C sort | zip -X -q "$output_file" -@
  )

  test -s "$output_file"
}

package_browser chrome Chrome
package_browser edge Edge
package_browser opera Opera
package_browser firefox Firefox

(
  cd "$output_root"
  sha256sum SNAPVERE-Chrome.zip SNAPVERE-Edge.zip SNAPVERE-Opera.zip SNAPVERE-Firefox.zip > SHA256SUMS.txt
)

printf 'Created reproducible browser packages in %s\n' "$output_root"
