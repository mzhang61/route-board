from pathlib import Path
import shutil
import subprocess
subprocess.run(["npm", "run", "build:mcp"], cwd="frontend", check=True)
assets = Path("backend/RouteBoard.Api/Assets")
assets.mkdir(exist_ok=True)
shutil.copyfile("frontend/dist-mcp/mcp-app.html", assets / "mcp-app.html")
