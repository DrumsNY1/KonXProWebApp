#!/usr/bin/env python3
"""Render a GitHub Actions step summary from VSTest .trx files.

Used by .github/workflows/build-and-test.yml so test failures are readable in the
run summary without downloading the raw logs. Writes Markdown to stdout.

Usage: trx-summary.py <results-directory>
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def main() -> int:
    results_dir = sys.argv[1] if len(sys.argv) > 1 else "TestResults"
    files = sorted(glob.glob(os.path.join(results_dir, "*.trx")))
    if not files:
        print("### Test results\n\nNo .trx files were produced.")
        return 0

    print("### Test results\n")
    print("| Suite | Total | Passed | Failed | Skipped |")
    print("|---|---:|---:|---:|---:|")

    failures = []
    for path in files:
        name = os.path.splitext(os.path.basename(path))[0]
        try:
            counters = ET.parse(path).getroot().find(".//t:Counters", NS)
        except ET.ParseError as exc:
            print(f"| {name} | - | - | - | - |")
            failures.append((name, "unparseable .trx", str(exc)))
            continue
        if counters is None:
            print(f"| {name} | - | - | - | - |")
            continue
        get = lambda k: counters.get(k, "0")  # noqa: E731
        print(
            f"| {name} | {get('total')} | {get('passed')} "
            f"| {get('failed')} | {get('notExecuted')} |"
        )

        root = ET.parse(path).getroot()
        for result in root.findall(".//t:UnitTestResult", NS):
            if result.get("outcome") != "Failed":
                continue
            message = result.find(".//t:Message", NS)
            trace = result.find(".//t:StackTrace", NS)
            failures.append(
                (
                    result.get("testName", "(unnamed)"),
                    (message.text or "").strip() if message is not None else "",
                    (trace.text or "").strip() if trace is not None else "",
                )
            )

    if failures:
        print("\n### Failures\n")
        for test_name, message, trace in failures:
            print(f"<details><summary><code>{test_name}</code></summary>\n")
            print("```")
            print(message)
            if trace:
                print(trace)
            print("```")
            print("\n</details>\n")

    return 0


if __name__ == "__main__":
    sys.exit(main())
