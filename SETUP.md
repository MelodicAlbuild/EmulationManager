# EmulationManager Setup Guide

## Prerequisites

- **.NET 9 SDK** installed on your server and on any machine used for development
  - Download: https://dotnet.microsoft.com/download/dotnet/9.0
  - Verify: `dotnet --version` should show `9.0.x`

---

## Part 1: Server Setup

The server hosts your game library, serves files to the desktop client, and provides the web UI for browsing games.

### 1.1 Organize Your Game Files

Create a directory structure on your storage server for your game files. Example:

```
/mnt/storage/emulation/
├── games/
│   ├── switch/
│   │   ├── Zelda Breath of the Wild.nsp
│   │   ├── Super Mario Odyssey.nsp
│   │   └── Metroid Dread.nsp
│   ├── ds/
│   │   ├── Pokemon Black.nds
│   │   └── Mario Kart DS.nds
│   └── 3ds/
│       ├── Pokemon Ultra Sun.3ds
│       └── Fire Emblem Awakening.3ds
├── dlc/
│   └── switch/
│       ├── zelda_botw_dlc1.nsp
│       └── zelda_botw_dlc2.nsp
├── updates/
│   └── switch/
│       └── zelda_botw_v1.6.0.nsp
├── firmware/
│   └── switch/
│       └── Firmware 19.0.1.zip
└── bios/
    └── ds/
        ├── bios7.bin
        ├── bios9.bin
        └── firmware.bin
```

The paths can be on a local disk, a network share, or anywhere accessible from your server (including Tailscale-networked paths).

### 1.2 Configure the Server

Edit `src/EmulationManager.Server/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=emulationmanager.db"
  },
  "Storage": {
    "GamesBasePath": "/mnt/storage/emulation/games",
    "DlcBasePath": "/mnt/storage/emulation/dlc",
    "UpdatesBasePath": "/mnt/storage/emulation/updates",
    "EmulatorsBasePath": "/mnt/storage/emulation/emulators",
    "FirmwareBasePath": "/mnt/storage/emulation/firmware",
    "BiosBasePath": "/mnt/storage/emulation/bios",
    "CoverImagesBasePath": "/mnt/storage/emulation/covers"
  }
}
```

On Windows, use forward slashes or escaped backslashes:
```json
"GamesBasePath": "D:/Emulation/games"
```

**Important:** The `GamesBasePath` is also used by the **LibraryScanService** to auto-discover games on startup. It scans recursively for files with these extensions and imports them into the database:

| Extension | Platform |
|-----------|----------|
| `.nsp`, `.xci`, `.nca` | Nintendo Switch |
| `.nds`, `.dsi` | Nintendo DS |
| `.3ds`, `.cci`, `.cxi`, `.cia` | Nintendo 3DS |

File paths stored in the database for DLCs, updates, etc. are **relative to their respective base path**. So a DLC with `FilePath = "switch/zelda_botw_dlc1.nsp"` resolves to `{DlcBasePath}/switch/zelda_botw_dlc1.nsp`.

### 1.3 Configure the Listen URL

By default the server listens on `http://localhost:5038`. To make it accessible on your network, edit `src/EmulationManager.Server/Properties/launchSettings.json` or set the URL at runtime:

```bash
# Listen on all interfaces, port 5038
dotnet run --project src/EmulationManager.Server --urls "http://0.0.0.0:5038"
```

For your cloud proxy setup, point the proxy to this port on the Tailscale IP of the machine running the server.

### 1.4 Start the Server

```bash
cd EmulationManager
dotnet run --project src/EmulationManager.Server
```

On first start, the server will:
1. Create the SQLite database (`emulationmanager.db`) and apply migrations
2. Seed sample game data (only on a fresh DB -- you'll want to clear this later)
3. Run the library scanner to auto-import games from `GamesBasePath`
4. Start listening for connections

Verify it's working:
- Open `http://localhost:5038` in a browser -- you should see the home page with game counts
- `http://localhost:5038/games` -- game library grid
- `http://localhost:5038/health` -- health check endpoint
- `http://localhost:5038/api/games` -- raw JSON API

### 1.5 Manage Your Library

**Auto-import:** Drop game files into your `GamesBasePath` directory structure. The scanner runs on startup and imports anything new. Restart the server to pick up new files (or trigger a scan from admin).

**Manual management:** Navigate to `http://localhost:5038/admin/games` to add, edit, or delete games. Use this to fix titles, add descriptions, or set file paths for DLC/updates.

**Removing seed data:** The seed data only loads on an empty database. To start fresh, stop the server, delete `src/EmulationManager.Server/emulationmanager.db`, and restart. If your `GamesBasePath` is configured, the scanner will import your real games instead.

### 1.6 Running in Production

For a persistent deployment (e.g., on your cloud proxy):

```bash
# Build a self-contained release
dotnet publish src/EmulationManager.Server -c Release -o publish/server

# Run it
cd publish/server
./EmulationManager.Server --urls "http://0.0.0.0:5038"
```

Or use a systemd service (Linux):

```ini
[Unit]
Description=EmulationManager Server
After=network.target

[Service]
WorkingDirectory=/opt/emulationmanager
ExecStart=/opt/emulationmanager/EmulationManager.Server --urls http://0.0.0.0:5038
Restart=always
User=emumgr

[Install]
WantedBy=multi-user.target
```

Logs are written to the `logs/` directory relative to where the server runs.

---

## Part 2: Desktop Client Setup

The desktop client runs on your friends' PCs (and yours). It connects to the server, downloads games, manages emulators, and launches everything.

### 2.1 Running from Source (Development)

```bash
cd EmulationManager
dotnet run --project src/EmulationManager.Desktop
```

The client will:
1. Create a local SQLite database at `%LocalAppData%/EmulationManager/local.db` (Windows) or `~/.local/share/EmulationManager/local.db` (Linux)
2. Register the `emumgr://` protocol handler on first run
3. Show the main window with a game library (empty until connected to server)

### 2.2 Configure the Server Connection

1. Open the desktop client
2. Click **Settings** in the sidebar
3. Set **Server URL** to your server's address (e.g., `http://your-server:5038`)
4. Set **Install Directory** to where you want emulators and games stored locally (defaults to `%LocalAppData%/EmulationManager`)
5. Click **Save Settings**
6. Go back to **Game Library** -- it should now load games from the server

**Note:** The server URL is currently also hardcoded as a default in the HttpClient registration (`http://localhost:5038`). For friends connecting to a remote server, they need to update this in Settings on first run.

### 2.3 Building Releases for Friends

Build platform-specific executables:

```bash
# Windows
dotnet publish src/EmulationManager.Desktop -c Release -r win-x64 --self-contained -o publish/win-x64

# Linux
dotnet publish src/EmulationManager.Desktop -c Release -r linux-x64 --self-contained -o publish/linux-x64

# macOS (Apple Silicon)
dotnet publish src/EmulationManager.Desktop -c Release -r osx-arm64 --self-contained -o publish/osx-arm64
```

These produce self-contained executables that don't need .NET installed. Zip and share with friends.

### 2.4 GitHub Releases (Automated)

If you push to a GitHub repo, the included CI/CD workflows handle this:

1. Push your code to GitHub
2. Update `githubRepo` in `src/EmulationManager.Server/Components/Pages/ClientDownload.razor` (line 62) to your actual repo (e.g., `myuser/EmulationManager`)
3. Tag a release: `git tag v1.0.0 && git push --tags`
4. GitHub Actions builds for all 3 platforms and creates a release
5. Friends can download from `http://your-server:5038/download-client`

---

## Part 3: The Play Flow (How It All Works)

### From the Web UI:

1. Friend opens `http://your-server:5038/games` in their browser
2. Clicks on a game to see its details
3. Clicks the **Play** button
4. Browser opens an `emumgr://launch/{gameId}` link
5. The desktop client catches this (registered as protocol handler)
6. Client checks if the emulator is installed -- if not, prompts to install
7. Client checks if the game is downloaded locally -- if not, downloads from server
8. Client validates requirements (prod.keys for Switch, BIOS for DS, etc.)
9. Client launches the game with the correct emulator

### From the Desktop Client directly:

1. Open the client, browse the Game Library
2. Click on a game card to view details
3. (Launch flow will trigger when the Play button is wired to LaunchService)

---

## Part 4: Emulator Requirements

Each platform has specific requirements that must be satisfied before games will run:

### Nintendo Switch (Ryubing)

- **prod.keys**: Must exist at `{emulator_dir}/system/prod.keys`. These are encryption keys required to decrypt Switch game files. The client checks for this during launch validation.
- **Firmware**: Optional but recommended. Can be installed via the firmware install flow.
- **DLC/Updates**: NSP files. The handler manages placing them in the correct Ryubing directories.

### Nintendo DS (melonDS)

- **BIOS files**: `bios7.bin`, `bios9.bin`, and `firmware.bin` must be in melonDS's directory. The client checks for all three during validation.
- No DLC or update management needed.

### Nintendo 3DS (Citra)

- No mandatory BIOS or firmware files.
- DLC/Updates are CIA files placed in Citra's install directory.

---

## Part 5: Directory Layout Reference

### Server-side (your storage)
```
{GamesBasePath}/
├── switch/game.nsp          <- game FilePath is relative to GamesBasePath
├── ds/game.nds
└── 3ds/game.3ds

{DlcBasePath}/
└── switch/dlc/game_dlc.nsp  <- DLC FilePath is relative to DlcBasePath

{UpdatesBasePath}/
└── switch/updates/game_update.nsp
```

### Client-side (friend's PC)
```
%LocalAppData%/EmulationManager/
├── local.db                  <- installed emulators, downloaded games, settings
├── logs/
│   └── client-2026-03-24.log
└── games/                    <- default install directory
    ├── nintendoswitch/
    │   └── Zelda Breath of the Wild.nsp
    ├── nintendods/
    │   └── Pokemon Black.nds
    └── nintendo3ds/
        └── Fire Emblem Awakening.3ds
```

---

## Part 6: API Reference (Quick)

All endpoints return JSON with string enum values.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/games?platform=NintendoSwitch&search=zelda` | List games (filterable) |
| GET | `/api/games/{id}` | Game detail with DLC/update lists |
| GET | `/api/emulators` | All registered emulators |
| GET | `/api/emulators/{platform}` | Emulator for a specific platform |
| GET | `/api/platforms` | Platform stats (game counts) |
| GET | `/api/downloads/{type}/{id}` | Stream file (supports Range headers) |
| HEAD | `/api/downloads/{type}/{id}` | Get file size without downloading |
| GET | `/api/client/latest` | Latest client version info |
| GET | `/health` | Health check |

Download `type` values: `Game`, `Dlc`, `Update`, `Emulator`, `Firmware`, `Bios`

---

## Troubleshooting

**Server won't start / migration error**: Delete `emulationmanager.db` and restart. The DB will be recreated.

**Client can't connect**: Check the Server URL in Settings. Make sure the server is running and accessible (try `curl http://your-server:5038/api/games`).

**Games not showing up after adding files**: The library scanner only runs on startup. Restart the server to pick up new files.

**Protocol handler not working (emumgr:// links)**: The handler registers on first run. If it didn't register, run the client once normally, then try the link again. On Linux, verify `~/.local/share/applications/emumgr.desktop` exists.

**Emulator not found**: The handlers search common installation paths. If your emulator is installed elsewhere, you'll need to add its path to the handler's search paths (in `src/EmulationManager.Emulators/Handlers/`).

**Rate limited**: Download endpoints allow 5 requests per 10 seconds. If you're hitting limits during testing, adjust in `Program.cs`.

**Logs**: Server logs are in `logs/server-*.log` relative to the server working directory. Client logs are in `%LocalAppData%/EmulationManager/logs/`.
