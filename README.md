# Minecraft 自動更新工具<br />
專為Minecraft的伺服器主設計，讓你的使用者不再需要手動更新，可以自定義的全自動更新<br />
<br />
特色：<br />
1. 採用SHA256較驗檔案，SHA256不同則視為版本不同<br />
2. 支援模糊搜索檔名並刪除，例如Botania*<br />
3. 開源

# Minecraft Automatic Update Tool<br />
Designed specifically for Minecraft's server mainframe, so your users no longer need to manually update, you can customize the automatic update <br />
<br />
Features：<br />
1. Use SHA256 to verify the file, SHA256 is different as the version is different<br />
2. Support fuzzy search file names and delete them, for example Botania*<br />
3. Open source


## Installation  <br/>
https://github.com/flier268/Minecraft_updater/wiki

## Download Authentication
Starting from v1.2 you can secure update pack downloads with common authentication schemes.

- Open the **Pack Maker** (`UpdatepackMakerWindow`) and use the new “下載來源身份驗證” panel to pick one mode at a time (Basic, Bearer Token, API Key header, or API Key query). Only the fields for the selected mode stay active; switching modes clears credentials from the others automatically.
- The app writes the chosen settings back to `Minecraft_updater.ini` (or the path specified with `--config`), so runtime downloads (pack list + file sync) reuse the same credentials.
- Manual configuration is also possible by editing the following keys inside the `[Minecraft_updater]` block of `Minecraft_updater.ini`:

  ```ini
  DownloadAuthType=None|Basic|BearerToken|ApiKeyHeader|ApiKeyQuery
  DownloadAuthUsername=
  DownloadAuthPassword=
  DownloadAuthBearerToken=
  DownloadAuthHeaderName=
  DownloadAuthHeaderValue=
  DownloadAuthQueryName=
  DownloadAuthQueryValue=
  ```

### Custom Configuration File

- Default config file name is now `Minecraft_updater.ini`. If the new file is missing but an older `config.ini` with a valid `scUrl` is present, the app copies it forward automatically.
- You can override the config path via `--config <path>` (or `-c <path>`) appended after the command, e.g. `Minecraft_updater.exe CheckUpdate --config /path/to/custom.ini`.
