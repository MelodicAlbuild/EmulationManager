# Server Deployment Guide

This guide covers deploying Grimoire to local server hardware behind a Tailscale network, with a cloud proxy serving it to the public internet. Kestrel binds directly to the Tailscale interface — no local nginx needed.

## Traffic Flow

```
Friend's browser
    │
    ▼
emu.yourdomain.com:443 (Cloud nginx, SSL termination)
    │
    │  Tailscale tunnel
    ▼
100.64.x.x:5038 (Kestrel / Grimoire.Server)
    │
    ▼
/mnt/storage/emulation/games/* (local disk)
```

---

## Prerequisites

- .NET 9 SDK on the local server (`dotnet --version` should show `9.0.x`)
- nginx on the cloud proxy
- Tailscale running on both machines, with connectivity confirmed (`tailscale ping <other-node>`)
- A domain name pointed at the cloud proxy's public IP

---

## Part 1: Deploy to Local Server

### 1.1 Build and Publish

```bash
git clone <your-repo-url> /opt/grimoire
cd /opt/grimoire
dotnet publish src/Grimoire.Server -c Release -o /opt/grimoire/publish
```

### 1.2 Create a Service User

```bash
sudo useradd -r -s /bin/false grimoire
sudo chown -R grimoire:grimoire /opt/grimoire/publish
```

### 1.3 Configure the Application

Edit `/opt/grimoire/publish/appsettings.json`:

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
    "DefaultConnection": "Data Source=grimoire.db"
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

Set each path to wherever your files live on the local server. The library scanner runs on startup and auto-imports games from `GamesBasePath` by file extension:

| Extension | Platform |
|-----------|----------|
| `.nsp`, `.xci`, `.nca` | Nintendo Switch |
| `.nds`, `.dsi` | Nintendo DS |
| `.3ds`, `.cci`, `.cxi`, `.cia` | Nintendo 3DS |

Make sure the `grimoire` user has read access to these paths:

```bash
sudo usermod -aG storage grimoire  # or whatever group owns the files
```

### 1.4 Find Your Tailscale IP

```bash
tailscale ip -4
# Example output: 100.64.1.10
```

### 1.5 Create the systemd Service

Create `/etc/systemd/system/grimoire.service`:

```ini
[Unit]
Description=Grimoire Server
After=network.target tailscaled.service

[Service]
WorkingDirectory=/opt/grimoire/publish
ExecStart=/opt/grimoire/publish/Grimoire.Server --urls http://100.64.1.10:5038
Restart=always
RestartSec=5
User=grimoire
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1

[Install]
WantedBy=multi-user.target
```

Replace `100.64.1.10` with your actual Tailscale IP.

Kestrel binds directly to the Tailscale interface — only machines on your Tailscale network can reach it. No local nginx required.

### 1.6 Start the Service

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now grimoire
```

Verify:

```bash
sudo systemctl status grimoire
curl http://100.64.1.10:5038/health
```

Logs are written to `/opt/grimoire/publish/logs/server-*.log` and can also be viewed via journalctl:

```bash
journalctl -u grimoire -f
```

---

## Part 2: Cloud Proxy nginx (Public-Facing)

This nginx runs on your cloud server, receives traffic from the internet on your domain, and forwards it through Tailscale directly to Kestrel on the local server.

### 2.1 Add the WebSocket Upgrade Map

Add to `/etc/nginx/nginx.conf` inside the `http { }` block if not already present:

```nginx
map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
}
```

This is required for Blazor Server (SignalR uses WebSockets).

### 2.2 Create the Site Config

Create `/etc/nginx/sites-available/grimoire`:

```nginx
server {
    listen 80;
    server_name emu.yourdomain.com;

    # Redirect all HTTP to HTTPS
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name emu.yourdomain.com;

    # SSL certificates (certbot will populate these)
    ssl_certificate /etc/letsencrypt/live/emu.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/emu.yourdomain.com/privkey.pem;

    # No upload size limit
    client_max_body_size 0;

    # Generous timeouts for game downloads over Tailscale
    proxy_read_timeout 600s;
    proxy_send_timeout 600s;
    proxy_connect_timeout 10s;

    # WebSocket support (Blazor Server / SignalR)
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection $connection_upgrade;

    # Preserve original host and protocol for Blazor
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    # Stream files directly
    proxy_buffering off;

    location / {
        # Points directly to Kestrel on the local server's Tailscale IP
        proxy_pass http://100.64.1.10:5038;
    }
}
```

Replace:
- `emu.yourdomain.com` with your actual domain
- `100.64.1.10` with your local server's Tailscale IP

### 2.3 SSL Certificate

```bash
sudo certbot --nginx -d emu.yourdomain.com
```

Or if you prefer to get the cert first and configure manually:

```bash
sudo certbot certonly --standalone -d emu.yourdomain.com
```

### 2.4 Enable and Test

```bash
sudo ln -s /etc/nginx/sites-available/grimoire /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

---

## Part 3: Forwarded Headers

Since the app sits behind a reverse proxy, ASP.NET Core needs to trust the forwarded headers so that URL generation, HTTPS detection, and client IP logging work correctly.

Add this to `/opt/grimoire/publish/appsettings.json` on the local server:

```json
{
  "ForwardedHeaders": {
    "ForwardedHeaders": "XForwardedFor,XForwardedProto",
    "KnownNetworks": ["100.64.0.0/10"],
    "ForwardLimit": 1
  }
}
```

The `100.64.0.0/10` range covers Tailscale's CGNAT address space.

> **Note:** If you get HTTPS redirect loops in production, it's because `UseHttpsRedirection()` fires before the forwarded proto header is processed. SSL terminates at the cloud proxy, so traffic between the proxy and Kestrel is plain HTTP over Tailscale. If this causes issues, you can disable `UseHttpsRedirection()` in `Program.cs` for production by wrapping it in the existing `IsDevelopment()` check — it's already partially there.

---

## Part 4: Verify the Full Chain

Run these in order to confirm each hop works:

```bash
# 1. Kestrel directly (on the local server)
curl http://100.64.1.10:5038/health

# 2. Through Tailscale (from the cloud proxy)
curl http://100.64.1.10:5038/health

# 3. Full public chain (from anywhere)
curl https://emu.yourdomain.com/health
curl https://emu.yourdomain.com/api/games
```

Open `https://emu.yourdomain.com` in a browser — you should see the Grimoire home page with game counts.

---

## Part 5: Updating the Server

When you want to deploy a new version:

```bash
cd /opt/grimoire
git pull
dotnet publish src/Grimoire.Server -c Release -o /opt/grimoire/publish
sudo systemctl restart grimoire
```

The database is preserved across restarts — migrations run automatically on startup.

---

## Part 6: Desktop Client Configuration

Once the server is deployed, friends configure their desktop client to point at the public URL:

1. Open the desktop client
2. Go to **Settings**
3. Set **Server URL** to `https://emu.yourdomain.com`
4. Save and return to Game Library

The `grimoire://` protocol links on the web UI will also work — clicking **Play** on `https://emu.yourdomain.com/games/1` triggers the desktop client via the protocol handler.

---

## Troubleshooting

### Server won't start
```bash
journalctl -u grimoire -n 50 --no-pager
```
Common causes: wrong file permissions on the publish directory, missing .NET runtime, port already in use, Tailscale not running yet (service depends on `tailscaled.service`).

### 502 Bad Gateway from cloud nginx
Kestrel isn't running or isn't reachable from the cloud proxy over Tailscale.
```bash
# On the cloud proxy
tailscale ping 100.64.1.10
curl http://100.64.1.10:5038/health
```

### Blazor pages load but are broken / no interactivity
WebSocket connection is failing. Verify the `map $http_upgrade` block is in the cloud nginx config and `proxy_http_version 1.1` is set.

### Game downloads hang or timeout
Increase `proxy_read_timeout` in the cloud nginx config. The default 60s is too short for multi-GB game files. The config above sets 600s (10 minutes).

### Tailscale connectivity
```bash
# On the cloud proxy
tailscale status
tailscale ping 100.64.1.10
```
If the ping fails, check that both machines are logged into the same Tailscale network and that ACLs allow traffic between them.

### HTTPS redirect loop
SSL terminates at the cloud proxy. If the app keeps redirecting to HTTPS, ensure `X-Forwarded-Proto` is being passed and the `ForwardedHeaders` config is set in `appsettings.json`. As a last resort, remove `app.UseHttpsRedirection()` from `Program.cs`.

### Rate limiting (429 errors)
Download endpoints are rate-limited to 5 requests per 10 seconds per client. Adjust in `Program.cs` if friends are downloading multiple files concurrently.

### Kestrel not binding to Tailscale IP
If Tailscale starts after the Grimoire service, the Tailscale IP may not exist yet. The systemd unit has `After=tailscaled.service` but you can also add a small startup delay:
```ini
ExecStartPre=/bin/sleep 3
```
