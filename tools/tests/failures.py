#!/usr/bin/env python3
"""The totals of a Unity test-results XML and every failed test with the first line of its message.

  python tools/tests/failures.py <results.xml>
"""
import sys
import xml.etree.ElementTree as ET


def main():
    try:
        root = ET.parse(sys.argv[1]).getroot()
    except (OSError, ET.ParseError) as error:
        print(f"no results ({error})")
        return
    print(f"{root.get('passed')}/{root.get('total')} passed, {root.get('failed')} failed")
    for case in root.iter("test-case"):
        if case.get("result") == "Failed":
            message = case.findtext("failure/message") or ""
            first = next((l.strip() for l in message.splitlines() if l.strip()), "")
            print(f"  FAIL {case.get('fullname')}: {first[:220]}")


if __name__ == "__main__":
    main()
