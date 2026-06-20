
namespace BokutachiToOpenLR2ScoreImporter
{
    public class BokutachiScoreRecord
    {
        public required string MD5 { get; set; }
        public required BokutachiClearType ClearType { get; set; }
        public required BokutachiRankType GradeType { get; set; }
        public required int NoteCount { get; set; }
        public required int Rate { get; set; }
        public required int PGreat { get; set; }
        public required int Great { get; set; }
        public required int Good { get; set; }
        public required int Bad { get; set; }
        public required int Poor { get; set; }
        public required int MaxCombo { get; set; }
        public required int MinBP { get; set; }
    }

    public enum BokutachiClearType
    {
        NOPLAY = 0,
        FAILED = 1,
        ASSISTED = 2,
        EASY = 3,
        NORMAL = 4,
        HARD = 5,
        EXHARD = 6,
        FULLCOMBO = 7,
    }

    public enum BokutachiRankType
    {
        F = 0,
        E = 1,
        D = 2,
        C = 3,
        B = 4,
        A = 5,
        AA = 6,
        AAA = 7,
        MAXMinus = 8,
        MAX = 9,
    }
}
