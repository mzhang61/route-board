"""Measure a public staging route from an independent, instrumented client."""
import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.request

from opentelemetry import propagate, trace
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import ConsoleSpanExporter, SimpleSpanProcessor

SITE = "https://staging.mzcampuspulse.com"
DEADLINE_SECONDS = 5
SAMPLES = 3


def measure(tracer, route_id):
    with tracer.start_as_current_span("GET route estimate", kind=trace.SpanKind.CLIENT) as span:
        headers = {"User-Agent": "RouteBoard-SLO/1.0 (+https://github.com/mzhang61/route-board)",
                   "Accept": "application/json"}
        propagate.inject(headers)
        request = urllib.request.Request(f"{SITE}/api/routes/{route_id}/estimate", headers=headers)
        start = time.monotonic()
        status = 0
        outcome = "request_failed"
        try:
            with urllib.request.urlopen(request, timeout=8) as response:
                status = response.status
                body = json.load(response)
            if body.get("status") in ("fresh", "cached", "stale") and isinstance(body.get("seconds"), (float, int)) and body["seconds"] > 0:
                outcome = body["status"]
        except urllib.error.HTTPError as error:
            status = error.code
        except (OSError, ValueError):
            pass
        elapsed = time.monotonic() - start
        good = status == 200 and outcome != "request_failed" and elapsed <= DEADLINE_SECONDS
        span.set_attribute("http.response.status_code", status)
        span.set_attribute("routing.outcome", outcome)
        span.set_status(trace.StatusCode.OK if good else trace.StatusCode.ERROR)
        return {"good": good, "status": status, "outcome": outcome, "seconds": round(elapsed, 3),
                "trace_id": format(span.get_span_context().trace_id, "032x"),
                "client_span_id": format(span.get_span_context().span_id, "016x")}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--drill", action="store_true", help="Send the first sample to a missing route")
    args = parser.parse_args()
    provider = TracerProvider(resource=Resource.create({"service.name": "routeboard-slo-probe"}))
    provider.add_span_processor(SimpleSpanProcessor(ConsoleSpanExporter()))
    tracer = provider.get_tracer("routeboard.routing.probe")
    # This fixed route ID belongs to the seeded staging catalog.
    samples = [measure(tracer, -1 if args.drill and index == 0 else 2) for index in range(SAMPLES)]
    provider.shutdown()
    good = sum(sample["good"] for sample in samples)
    summary = f"Staging route SLI: {good}/{SAMPLES} valid estimates within {DEADLINE_SECONDS}s. Target: {SAMPLES}/{SAMPLES} per check."
    print(json.dumps(samples))
    print(summary)
    if path := os.getenv("GITHUB_STEP_SUMMARY"):
        with open(path, "a") as output:
            print(summary, file=output)
            print("\n| HTTP | Outcome | Seconds | Trace ID |\n|---|---|---:|---|", file=output)
            for row in samples:
                print(f"| {row['status']} | {row['outcome']} | {row['seconds']} | {row['trace_id']} |", file=output)
    if good != SAMPLES:
        print("::error::" + summary)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
