# Bokutachi To OpenLR2 Score Importer

Imports your personal best scores from Bokutachi IR into your local OpenLR2 player.db.

## Features

- Updates existing chart records with your Bokutachi IR PB.
- Inserts new chart records if they don't exist in your local database.
- Preserves existing records if they are already better than your Bokutachi IR PBs.

[How is it imported?](https://app.notion.com/p/IR-Record-to-LR2-player-db-38452d65f39a801fbf78e059a816f9df?source=copy_link)

## How to use

1. Make a backup of your `player.db` file.
    - Typically located at : `LR2Path/LR2Files/Database/Score/<playerName>.db`
2. Download your all.json file (Personal Best scores from Bokutachi IR)
    - Go to `https://boku.tachi.ac/api/v1/users/<your-userid>/games/bms-7k/pbs/all`.
    - Download the raw JSON file (usually right-click in your browser and select "Save as...").
3. Launch BokutachiToOpenLR2ScoreImporter.exe
4. Select the path to your all.json file.
5. Select the path to your player.db file.
6. The import process will start automatically.
7. Press any key to close the application once finished.

## Disclaimer

- **Importing directly into `player.db` is a risky operation and might result in data corruption.** Always keep a backup of your database.

- Please use your own userid. Note that there is no validation or authentication process at this time.

- **Use this tool at your own risk.**