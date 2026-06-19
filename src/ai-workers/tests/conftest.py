import sys
from pathlib import Path

# Allow `from workers.main import app` when running pytest from src/ai-workers
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
