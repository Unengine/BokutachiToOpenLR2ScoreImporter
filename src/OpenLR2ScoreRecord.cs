
using System.Security.Policy;

namespace BokutachiToOpenLR2ScoreImporter
{
    public class OpenLR2ScoreRecord
    {
        public required string Hash { get; set; }
        public required OpenLR2ClearType Clear { get; set; }
        public required int PGreat { get; set; }
        public required int Great { get; set; }
        public required int Good { get; set; }
        public required int Bad { get; set; }
        public required int Poor { get; set; }
        public required int Totalnotes { get; set; }
        public required int MaxCombo { get; set; }
        public required int MinBP { get; set; }
        public required int PlayCount { get; set; }
        public required int ClearCount { get; set; }
        public required int FailCount { get; set; }
        public required OpenLR2RankType Rank { get; set; }
        public required int Rate { get; set; }
        public required int ClearDB { get; set; }
        public required int OpHistory { get; set; }
        public required string ScoreHash { get; set; }
        public required string? Ghost { get; set; }
        public required int ClearSD { get; set; }
        public required int ClearEX { get; set; }
        public required int OpBest { get; set; }
        public required int RSeed { get; set; }
        public required int Complete { get; set; }
    }

    public enum OpenLR2ClearType
    {
        NOPLAY = 0,
        FAILED = 1,
        EASY = 2,
        NORMAL = 3,
        HARD = 4,
        FULLCOMBO = 5
    }

    public enum OpenLR2RankType
    {
        F = 1,
        E = 2,
        D = 3,
        C = 4,
        B = 5,
        A = 6,
        AA = 7,
        AAA = 8,
    }
}
