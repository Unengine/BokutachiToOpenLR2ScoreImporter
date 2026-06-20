using Microsoft.Data.Sqlite;

namespace BokutachiToOpenLR2ScoreImporter
{
    public class PlayerDBBuilder
    {
        private const string IR_DERIVED_SCOREHASH = "IR_DERIVED_RECORD";

        public void BuildIRScoreDB(string playerDBPath, IEnumerable<BokutachiScoreRecord> records)
        {
            Dictionary<string, OpenLR2ScoreRecord> existingRecords;
            using (var playerdbConn = new SqliteConnection($"Data Source={playerDBPath}"))
            {
                playerdbConn.Open();
                existingRecords = LoadExistingRecords(playerdbConn);
            }

            Console.WriteLine($"Loaded {existingRecords.Count} existing records from the database.");

            var parent = Directory.GetParent(playerDBPath).FullName;
            var dbDir = Path.Combine(parent, "IRScore");
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            var fileName = Path.GetFileName(playerDBPath);
            var dbPath = Path.Combine(dbDir, fileName);
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                CreateTable(conn, tx);

                var upsertedCount = 0;
                foreach (var record in records)
                {
                    if (existingRecords.TryGetValue(record.MD5, out var lr2Record))
                    {
                        if (HasBetterStats(record, lr2Record) &&
                            UpsertScore(conn, tx, record))
                        {
                            upsertedCount++;
                        }
                    }
                    else
                    {
                        if (UpsertScore(conn, tx, record))
                        {
                            upsertedCount++;
                        }
                    }
                }

                Console.WriteLine($"Upserted {upsertedCount} new scores.");
                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Console.WriteLine($"DB error: {ex.Message}");
            }
        }

        private bool HasBetterStats(BokutachiScoreRecord tachi, OpenLR2ScoreRecord lr2)
        {
            return
                (tachi.PGreat * 2 + tachi.Great > lr2.PGreat * 2 + lr2.Great) ||
                (BokutachiClearTypeToLR2(tachi.ClearType) > lr2.Clear) ||
                (tachi.MaxCombo > lr2.MaxCombo) ||
                (tachi.MinBP < lr2.MinBP);
        }

        private bool UpsertScore(SqliteConnection conn, SqliteTransaction tx, BokutachiScoreRecord record)
        {
            var query = @"
                INSERT OR REPLACE INTO imported_score
                (hash, clear, perfect, great, good, bad, poor, total_notes, max_combo, minbp, rank, rate, complete)
                VALUES (@hash, @clear, @perfect, @great, @good, @bad, @poor, @total_notes, @max_combo, @minbp, @rank, @rate, @complete);";

            try
            {
                using var cmd = new SqliteCommand(query, conn);
                cmd.Transaction = tx;

                var clear = BokutachiClearTypeToLR2(record.ClearType);
                var rank = BokutachiRankTypeToLR2(record.RankType);

                cmd.Parameters.AddWithValue("@hash", record.MD5);
                cmd.Parameters.AddWithValue("@clear", (int)clear);
                cmd.Parameters.AddWithValue("@perfect", record.PGreat);
                cmd.Parameters.AddWithValue("@great", record.Great);
                cmd.Parameters.AddWithValue("@good", record.Good);
                cmd.Parameters.AddWithValue("@bad", record.Bad);
                cmd.Parameters.AddWithValue("@poor", record.Poor);
                cmd.Parameters.AddWithValue("@total_notes", record.NoteCount);
                cmd.Parameters.AddWithValue("@max_combo", record.MaxCombo);
                cmd.Parameters.AddWithValue("@minbp", record.MinBP);
                cmd.Parameters.AddWithValue("@rank", (int)rank);
                cmd.Parameters.AddWithValue("@rate", record.Rate);
                cmd.Parameters.AddWithValue("@complete", clear == 0 ? 0 : 1);

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"기록 저장 실패 (Hash: {record.MD5}): {ex.Message}");
                return false;
            }
        }

        private void CreateTable(SqliteConnection conn, SqliteTransaction tx)
        {
            var query = @"CREATE TABLE IF NOT EXISTS imported_score (
                hash TEXT PRIMARY KEY,
                clear INTEGER, 
                perfect INTEGER,
                great INTEGER,
                good INTEGER,
                bad INTEGER,
                poor INTEGER,
                total_notes INTEGER,
                max_combo INTEGER,
                minbp INTEGER,
                rank INTEGER,
                rate INTEGER,
                complete INTEGER
            );";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Transaction = tx;
            cmd.ExecuteNonQuery();
        }

        private OpenLR2ClearType BokutachiClearTypeToLR2(BokutachiClearType clearType)
        {
            return clearType switch
            {
                BokutachiClearType.NOPLAY => OpenLR2ClearType.NOPLAY,
                BokutachiClearType.FAILED => OpenLR2ClearType.FAILED,
                BokutachiClearType.ASSISTED => OpenLR2ClearType.FAILED,
                BokutachiClearType.EASY => OpenLR2ClearType.EASY,
                BokutachiClearType.NORMAL => OpenLR2ClearType.NORMAL,
                BokutachiClearType.HARD => OpenLR2ClearType.HARD,
                BokutachiClearType.EXHARD => OpenLR2ClearType.HARD,
                BokutachiClearType.FULLCOMBO => OpenLR2ClearType.FULLCOMBO,
                _ => OpenLR2ClearType.NOPLAY
            };
        }

        private OpenLR2RankType BokutachiRankTypeToLR2(BokutachiRankType gradeType)
        {
            return gradeType switch
            {
                BokutachiRankType.F => OpenLR2RankType.F,
                BokutachiRankType.E => OpenLR2RankType.E,
                BokutachiRankType.D => OpenLR2RankType.D,
                BokutachiRankType.C => OpenLR2RankType.C,
                BokutachiRankType.B => OpenLR2RankType.B,
                BokutachiRankType.A => OpenLR2RankType.A,
                BokutachiRankType.AA => OpenLR2RankType.AA,
                BokutachiRankType.AAA => OpenLR2RankType.AAA,
                BokutachiRankType.MAXMinus => OpenLR2RankType.AAA,
                BokutachiRankType.MAX => OpenLR2RankType.AAA,
                _ => OpenLR2RankType.F
            };
        }

        private Dictionary<string, OpenLR2ScoreRecord> LoadExistingRecords(SqliteConnection conn)
        {
            var cache = new Dictionary<string, OpenLR2ScoreRecord>();

            string query = "SELECT * FROM score";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Transaction = conn.BeginTransaction();

            using (var reader = cmd.ExecuteReader())    
            {
                while (reader.Read())
                {
                    var hash = reader.GetString(reader.GetOrdinal("hash"));
                    var clear = reader.GetInt32(reader.GetOrdinal("clear"));
                    var pGreat = reader.GetInt32(reader.GetOrdinal("perfect"));
                    var great = reader.GetInt32(reader.GetOrdinal("great"));
                    var good = reader.GetInt32(reader.GetOrdinal("good"));
                    var bad = reader.GetInt32(reader.GetOrdinal("bad"));
                    var poor = reader.GetInt32(reader.GetOrdinal("poor"));
                    var totalNotes = reader.GetInt32(reader.GetOrdinal("totalnotes"));
                    var maxCombo = reader.GetInt32(reader.GetOrdinal("maxcombo"));
                    var minBP = reader.GetInt32(reader.GetOrdinal("minbp"));
                    var playCount = reader.GetInt32(reader.GetOrdinal("playcount"));
                    var clearCount = reader.GetInt32(reader.GetOrdinal("clearcount"));
                    var failCount = reader.GetInt32(reader.GetOrdinal("failcount"));
                    var rank = reader.GetInt32(reader.GetOrdinal("rank"));
                    var rate = reader.GetInt32(reader.GetOrdinal("rate"));
                    var clearDB = reader.GetInt32(reader.GetOrdinal("clear_db"));
                    var opHistory = reader.GetInt32(reader.GetOrdinal("op_history"));
                    var scoreHash = reader.GetString(reader.GetOrdinal("scorehash"));

                    var ghostOrdinal = reader.GetOrdinal("ghost");
                    var ghost = reader.IsDBNull(ghostOrdinal) ? null : reader.GetString(ghostOrdinal);

                    var clearSD = reader.GetInt32(reader.GetOrdinal("clear_sd"));
                    var clearEX = reader.GetInt32(reader.GetOrdinal("clear_ex"));
                    var opBest = reader.GetInt32(reader.GetOrdinal("op_best"));
                    var rSeed = reader.GetInt32(reader.GetOrdinal("rseed"));
                    var complete = reader.GetInt32(reader.GetOrdinal("complete"));

                    var record = new OpenLR2ScoreRecord
                    {
                        Hash = hash,
                        Clear = (OpenLR2ClearType)clear,
                        PGreat = pGreat,
                        Great = great,
                        Good = good,
                        Bad = bad,
                        Poor = poor,
                        Totalnotes = totalNotes,
                        MaxCombo = maxCombo,
                        MinBP = minBP,
                        PlayCount = playCount,
                        ClearCount = clearCount,
                        FailCount = failCount,
                        Rank = (OpenLR2RankType)rank,
                        Rate = rate,
                        ClearDB = clearDB,
                        OpHistory = opHistory,
                        ScoreHash = scoreHash,
                        Ghost = ghost,
                        ClearSD = clearSD,
                        ClearEX = clearEX,
                        OpBest = opBest,
                        RSeed = rSeed,
                        Complete = complete
                    };

                    // MD5 hash is primary key
                    if (!cache.ContainsKey(record.Hash))
                    {
                        cache.Add(record.Hash, record);
                    }
                }
            }

            return cache;
        }
    }
}
