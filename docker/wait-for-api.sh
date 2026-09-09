#!/bin/bash
# Wait for an ASP.NET Core API to be ready (HTTP 200 on /health/ready).
# Usage: ./wait-for-api.sh <host> <port> [timeout_seconds]
# Example: ./wait-for-api.sh localhost 5080 60

set -e

host=${1:-localhost}
port=${2:-8080}
timeout=${3:-60}
start_time=$(date +%s)

while true; do
  current_time=$(date +%s)
  elapsed=$((current_time - start_time))

  if [ $elapsed -gt $timeout ]; then
    echo "Timeout waiting for $host:$port to be ready after ${timeout}s"
    exit 1
  fi

  if curl -sf "http://$host:$port/health/ready" >/dev/null 2>&1; then
    echo "$host:$port is ready"
    exit 0
  fi

  echo "Waiting for $host:$port to be ready... ($elapsed/${timeout}s)"
  sleep 2
done
