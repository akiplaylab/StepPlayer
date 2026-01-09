namespace DDR.Domain
{
    public static class ScoreCalculator
    {
        private const int MaxScore = 1_000_000;

        public static int Calculate(int totalNotes, int marvelous, int perfect, int great, int good, int bad, int miss)
        {
            if (totalNotes <= 0)
            {
                return 0;
            }

            double basePoint = (double)MaxScore / totalNotes;

            double weighted =
                marvelous * 1.00 +
                perfect * 0.98 +
                great * 0.60 +
                good * 0.20 +
                bad * 0.00 +
                miss * 0.00;

            return (int)(basePoint * weighted);
        }

        public static string GetDanceLevel(int score, bool failed = false)
        {
            if (failed)
            {
                return "E";
            }

            if (score >= 990_000) return "AAA";
            if (score >= 950_000) return "AA+";
            if (score >= 900_000) return "AA";
            if (score >= 890_000) return "AA-";
            if (score >= 850_000) return "A+";
            if (score >= 800_000) return "A";
            if (score >= 790_000) return "A-";
            if (score >= 750_000) return "B+";
            if (score >= 700_000) return "B";
            if (score >= 690_000) return "B-";
            if (score >= 650_000) return "C+";
            if (score >= 600_000) return "C";
            if (score >= 590_000) return "C-";
            if (score >= 550_000) return "D+";

            return "D";
        }
    }
}
