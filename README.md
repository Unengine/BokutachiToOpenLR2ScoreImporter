# Bokutachi To OpenLR2 Score Importer

Imports your personal best scores from Bokutachi IR into your local OpenLR2 player.db.

## Releases (Downloads)
Download the latest release here, then unzip the file to get started.

https://github.com/Unengine/BokutachiToOpenLR2ScoreImporter/releases

## Features

- Inserts new chart records with your Bokutachi IR PB to the new table `imported_score`.
- Preserves existing records since the table is separated.

## How to use

1. Make a backup of your `player.db` file.
    - Typically located at : `LR2Path/LR2Files/Database/Score/<playerName>.db`
2. Download your all.json file (Personal Best scores from Bokutachi IR)
    - Go to `https://boku.tachi.ac/api/v1/users/<your-userid>/games/bms-7k/pbs/all`. (SP pbs)
    - `https://boku.tachi.ac/api/v1/users/<your-userid>/games/bms-14k/pbs/all` (DP pbs)
    - Download the raw JSON file (usually right-click in your browser and select "Save as...").
3. Launch BokutachiToOpenLR2ScoreImporter.exe
4. Select the path to your all.json file.
5. Select the path to your player.db file.
6. The import process will start automatically.
7. Press any key to close the application once finished.
8. You should launch it twice if you need to import both SP and DP scores. (Optional)

## Disclaimer

- **Importing directly into `player.db` is a risky operation and might result in data corruption.** Always keep a backup of your database.

- Please use your own userid. Note that there is no validation or authentication process at this time.

- **Use this tool at your own risk.**