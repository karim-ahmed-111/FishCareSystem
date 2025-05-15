#!/usr/bin/env bash

# -----------------------------
# startup.sh for FishCare FastAPI
# -----------------------------

# Exit on error
set -e

# Move to the directory where this script resides (so relative imports work)
cd "$(dirname "$0")"

# (Optional) Activate a virtual environment if you have one:
# source venv/bin/activate

# Export PORT if not set (many PaaS platforms use $PORT)
PORT=${PORT:-8000}

# Launch the app with Uvicorn
exec uvicorn main:app \
     --host 0.0.0.0 \
     --port "$PORT" \
     --workers 1
