using Microsoft.Data.Sqlite;

namespace BokutachiToOpenLR2ScoreImporter
{
    public class PlayerDBAggregator
    {
        private const string IR_DERIVED_SCOREHASH = "IR_DERIVED_RECORD";

        public void AggregatePlayerDB(string dbPath, IEnumerable<BokutachiScoreRecord> records)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var existingRecords = LoadExistingRecords(conn, tx);
                var updatedCount = 0;
                var insertedCount = 0;
                foreach (var record in records)
                {
                    if (existingRecords.TryGetValue(record.MD5, out var lr2Record))
                    {
                        if(UpdateExistingScore(conn, tx, record, lr2Record))
                        {
                            updatedCount++;
                        }
                    }
                    else
                    {
                        if (InsertNewScore(conn, tx, record))
                        {
                            insertedCount++;
                        }
                    }
                }

                Console.WriteLine($"Updated {updatedCount} new scores.\nInserted {insertedCount} new scores.");
                tx.Commit();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Console.WriteLine($"DB error: {ex.Message}");
            }
        }

        private bool UpdateExistingScore(SqliteConnection conn, SqliteTransaction tx, BokutachiScoreRecord tachi, OpenLR2ScoreRecord lr2)
        {
            var tachiClear = BokutachiClearTypeToLR2(tachi.ClearType);
            bool needsUpdate = (tachi.PGreat * 2 + tachi.Great > lr2.PGreat * 2 + lr2.Great) ||
                               (tachiClear > lr2.Clear) ||
                               (tachi.MaxCombo > lr2.MaxCombo) ||
                               (tachi.MinBP < lr2.MinBP);

            if (!needsUpdate) return false;

            string query = @"
            UPDATE score 
            SET
            -- default updates
            scorehash = 'IR_DERIVED_RECORD',
            op_best = 0,
            rseed = 0,

            -- Score updates
            perfect = CASE WHEN @tachiEx > @lr2Ex THEN @perfect ELSE perfect END,
            great = CASE WHEN @tachiEx > @lr2Ex THEN @great ELSE great END,
            good = CASE WHEN @tachiEx > @lr2Ex THEN @good ELSE good END,
            bad = CASE WHEN @tachiEx > @lr2Ex THEN @bad ELSE bad END,
            poor = CASE WHEN @tachiEx > @lr2Ex THEN @poor ELSE poor END,
            rank = CASE WHEN @tachiEx > @lr2Ex THEN @rank ELSE rank END,
            rate = CASE WHEN @tachiEx > @lr2Ex THEN @rate ELSE rate END,
            ghost = CASE WHEN @tachiEx > @lr2Ex THEN NULL ELSE ghost END,
            
            -- Clear updates
            clear = CASE WHEN @tachiClear > clear THEN @tachiClear ELSE clear END,
            maxcombo = CASE WHEN @tachiClearType == 7 THEN @notecount
                ELSE CASE WHEN @tachiMaxCombo > maxcombo THEN @tachiMaxCombo
                ELSE maxcombo END
            END,
            clearcount = clearcount + CASE WHEN @tachiClearType >= 3 THEN 1 ELSE 0 END,
            failcount = failcount + CASE WHEN @tachiClearType < 3 THEN 1 ELSE 0 END,
            complete = CASE WHEN @tachiClearType == 0 THEN complete ELSE 1 END,
            
            -- BP updates
            minbp = CASE WHEN @minbp < minbp THEN @minbp ELSE minbp END,

            -- Play count update
            playcount = playcount + 1
            WHERE hash = @hash";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Transaction = tx;

            cmd.Parameters.AddWithValue("@hash", tachi.MD5);
            cmd.Parameters.AddWithValue("@tachiEx", tachi.PGreat * 2 + tachi.Great);
            cmd.Parameters.AddWithValue("@lr2Ex", lr2.PGreat * 2 + lr2.Great);

            cmd.Parameters.AddWithValue("@perfect", tachi.PGreat);
            cmd.Parameters.AddWithValue("@great", tachi.Great);
            cmd.Parameters.AddWithValue("@good", tachi.Good);
            cmd.Parameters.AddWithValue("@bad", tachi.Bad);
            cmd.Parameters.AddWithValue("@poor", tachi.Poor);
            cmd.Parameters.AddWithValue("@rank", (int)BokutachiRankTypeToLR2(tachi.GradeType));
            cmd.Parameters.AddWithValue("@rate", tachi.Rate);

            cmd.Parameters.AddWithValue("@notecount", tachi.NoteCount);
            cmd.Parameters.AddWithValue("@tachiMaxCombo", tachi.MaxCombo);
            cmd.Parameters.AddWithValue("@tachiClear", (int)tachiClear);
            cmd.Parameters.AddWithValue("@tachiClearType", (int)tachi.ClearType);

            cmd.Parameters.AddWithValue("@minbp", tachi.MinBP);

            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                Console.WriteLine($"Warning : {tachi.MD5} record not updated.");
                return false;
            }

            Console.WriteLine($"UPDATE : {tachi.MD5} record is updated.");
            return true;
        }

        private bool InsertNewScore(SqliteConnection conn, SqliteTransaction tx, BokutachiScoreRecord record)
        {
            string insertQuery = @"
                INSERT INTO score (hash, clear, perfect, great, good, bad, poor, totalnotes, maxcombo, minbp, playcount, clearcount, failcount, rank, rate, clear_db, op_history, scorehash, ghost, clear_sd, clear_ex, op_best, rseed, complete)
                VALUES (@hash, @clear, @perfect, @great, @good, @bad, @poor, @totalnotes, @maxcombo, @minbp, @playcount, @clearcount, @failcount, @rank, @rate, @clear_db, @op_history, @scorehash, @ghost, @clear_sd, @clear_ex, @op_best, @rseed, @complete)";
            
            using var cmd = new SqliteCommand(insertQuery, conn);
            cmd.Transaction = tx;

            cmd.Parameters.AddWithValue("@hash", record.MD5);

            var clear = (int)BokutachiClearTypeToLR2(record.ClearType);
            cmd.Parameters.AddWithValue("@clear", clear);

            cmd.Parameters.AddWithValue("@perfect", record.PGreat);
            cmd.Parameters.AddWithValue("@great", record.Great);
            cmd.Parameters.AddWithValue("@good", record.Good);
            cmd.Parameters.AddWithValue("@bad", record.Bad);
            cmd.Parameters.AddWithValue("@poor", record.Poor);
            cmd.Parameters.AddWithValue("@totalnotes", record.NoteCount);
            cmd.Parameters.AddWithValue("@maxcombo", record.MaxCombo);
            cmd.Parameters.AddWithValue("@minbp", record.MinBP);
            cmd.Parameters.AddWithValue("@playcount", 1);
            cmd.Parameters.AddWithValue("@clearcount", record.ClearType >= BokutachiClearType.EASY ? 0 : 1);
            cmd.Parameters.AddWithValue("@failcount", record.ClearType < BokutachiClearType.EASY ? 1 : 0);

            var rank = (int)BokutachiRankTypeToLR2(record.GradeType);
            cmd.Parameters.AddWithValue("@rank", rank);

            cmd.Parameters.AddWithValue("@rate", record.Rate);
            cmd.Parameters.AddWithValue("@clear_db", 0);
            cmd.Parameters.AddWithValue("@op_history", 0);
            cmd.Parameters.AddWithValue("@scorehash", IR_DERIVED_SCOREHASH);
            cmd.Parameters.AddWithValue("@ghost", DBNull.Value);
            cmd.Parameters.AddWithValue("@clear_sd", 0);
            cmd.Parameters.AddWithValue("@clear_ex", 0);
            cmd.Parameters.AddWithValue("@op_best", 0);
            cmd.Parameters.AddWithValue("@rseed", 0);
            cmd.Parameters.AddWithValue("@complete", record.ClearType == BokutachiClearType.NOPLAY ? 0 : 1);

            int rowsAffected = cmd.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                Console.WriteLine($"Warning : {record.MD5} record not inserted.");
                return false;
            }

            Console.WriteLine($"INSERT : {record.MD5} record is inserted.");
            return true;
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

        private Dictionary<string, OpenLR2ScoreRecord> LoadExistingRecords(SqliteConnection conn, SqliteTransaction tx)
        {
            var cache = new Dictionary<string, OpenLR2ScoreRecord>();

            string query = "SELECT * FROM score";

            using var cmd = new SqliteCommand(query, conn);
            cmd.Transaction = tx;

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
