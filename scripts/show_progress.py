#!/usr/bin/env python3

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

BAR_WIDTH = 24
TRACKER_PATH = Path(__file__).resolve().parent.parent / "progress-tracker.md"


def load_progress_data() -> dict:
    markdown = TRACKER_PATH.read_text(encoding="utf-8")
    match = re.search(r"```progress-data\s*([\s\S]*?)```", markdown)

    if not match:
        raise ValueError("Could not find a ```progress-data block in progress-tracker.md")

    return json.loads(match.group(1).strip())


def render_bar(progress: int) -> str:
    filled = round((progress / 100) * BAR_WIDTH)
    return f"{'█' * filled}{'░' * (BAR_WIDTH - filled)}"


def print_chart(data: dict) -> None:
    latest_progress = data["days"][-1]["progress"]

    print()
    print(f"{data['project']} — Build Progress")
    print("=" * 72)
    print(data["repository"])
    print(data["summary"])
    print()

    for day in data["days"]:
        bar = render_bar(day["progress"])
        progress_label = f"{day['progress']:>3}%"
        day_label = f"Day {day['day']:>2}"
        print(
            f"{day_label}  {bar}  {progress_label}  {day['title']}"
        )

    print()
    print(f"Overall  {render_bar(latest_progress)}  {latest_progress:>3}%")
    print("=" * 72)
    print()


def print_verbose(data: dict) -> None:
    for day in data["days"]:
        print(f"Day {day['day']} ({day['progress']}%)")
        print(f"Focus: {day['focus']}")
        print("Completed:")
        for item in day["completed"]:
            print(f"  • {item}")
        if day.get("blockers"):
            print("Blockers:")
            for item in day["blockers"]:
                print(f"  • {item}")
        print()


def main() -> int:
    verbose = "--verbose" in sys.argv or "-v" in sys.argv

    try:
        data = load_progress_data()
        print_chart(data)

        if verbose:
            print_verbose(data)
        else:
            print("Tip: run with --verbose to print daily details.")
            print()

        return 0
    except Exception as error:
        print(f"Failed to render progress chart: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
