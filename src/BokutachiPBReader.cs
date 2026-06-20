using Newtonsoft.Json.Linq;

namespace BokutachiToOpenLR2ScoreImporter
{
    public class BokutachiPBReader
    {
        private Dictionary<string, (string md5, int noteCount)> chartMap;

        public IEnumerable<BokutachiScoreRecord>? LoadAllPBJson(string json)
        {
            JObject root = JObject.Parse(json);

            try
            {
                chartMap = new();
                var charts = root["body"]["charts"];
                foreach (var chart in charts)
                {
                    string chartId = chart["chartID"]?.ToString();

                    var data = chart["data"];
                    string md5 = data["hashMD5"]?.ToString();
                    int noteCount = data["notecount"]?.Value<int>() ?? 0;

                    if (!string.IsNullOrEmpty(chartId) && !string.IsNullOrEmpty(md5))
                    {
                        if (!chartMap.ContainsKey(chartId))
                        {
                            chartMap.Add(chartId, (md5, noteCount));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing charts: {ex.Message}");
                return null;
            }

            return ExtractPbsScores(root);
        }

        private List<BokutachiScoreRecord>? ExtractPbsScores(JObject root)
        {
            try
            {
                var scores = new List<BokutachiScoreRecord>();
                var pbsArray = root["body"]?["pbs"] as JArray;

                if (pbsArray == null) return scores;

                foreach (var item in pbsArray)
                {
                    var chartId = item["chartID"]?.ToString() ?? "";
                    if (chartMap.TryGetValue(chartId, out var tuple))
                    {
                        var md5 = tuple.md5;
                        var noteCount = tuple.noteCount;

                        var scoreData = item["scoreData"];
                        var enumIndexes = scoreData?["enumIndexes"];
                        var optional = scoreData?["optional"];
                        var judgements = scoreData?["judgements"];

                        var clearType = (BokutachiClearType)(int)enumIndexes?["lamp"];
                        var gradeTypeIndex = (BokutachiRankType)(int)enumIndexes?["grade"];


                        var pgr = judgements?["pgreat"]?.Value<int>() ?? 0;
                        var gr = judgements?["great"]?.Value<int>() ?? 0;
                        var gd = judgements?["good"]?.Value<int>() ?? 0;
                        var bd = judgements?["bad"]?.Value<int>() ?? 0;
                        var pr = judgements?["poor"]?.Value<int>() ?? 0;

                        var bpToken = optional?["bp"];
                        var bp = (bpToken != null && bpToken.Type == JTokenType.Integer)
                            ? bpToken.Value<int>()
                            : (bd + pr);

                        var maxCombo = optional?["maxCombo"]?.Value<int>() ??
                            (clearType == BokutachiClearType.FULLCOMBO ? noteCount : 0);
                        var rate = (int)(scoreData?["percent"]?.Value<double>() ?? ((pgr * 2 + gr) / noteCount * 2) * 100);

                        var record = new BokutachiScoreRecord
                        {
                            MD5 = md5,
                            ClearType = clearType,
                            GradeType = gradeTypeIndex,

                            NoteCount = noteCount,
                            Rate = rate,
                            PGreat = pgr,
                            Great = gr,
                            Good = gd,
                            Bad = bd,
                            Poor = pr,
                            MaxCombo = maxCombo,
                            MinBP = bp
                        };
                        scores.Add(record);
                    }
                    else
                    {
                        Console.WriteLine($"Error parsing pb, chartId : {chartId}");
                    }
                }
                return scores;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing pbs: {ex.Message}");
                return null;
            }
        }
    }
}
