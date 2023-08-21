using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Potionomics
{
    public record Cauldron(string Name, int MaxIngredients, int MaxMagimins);

    public record Ingredient(
        string Name,
        int Rarity,
        string Type,
        int MagiminsA,
        int MagiminsB,
        int MagiminsC,
        int MagiminsD,
        int MagiminsE,
        int TotalMagimins,
        bool? Taste,
        bool? Sensation,
        bool? Aroma,
        bool? Visual,
        bool? Sound,
        int BasePrice,
        string Location);

    public class PotionScoreComparer : IEqualityComparer<Potion>, IComparer<Potion>
    {
        public int Compare(Potion? x, Potion? y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return 1;
            if (y == null)
                return -1;

            if (y.Score != x.Score)
                return y.Score.CompareTo(x.Score);

            return y.TotalMagimins.CompareTo(x.TotalMagimins);
        }

        public bool Equals(Potion? x, Potion? y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.Score == y.Score;
        }

        public int GetHashCode([DisallowNull] Potion obj)
        {
            return obj.Score.GetHashCode();
        }
    }

    public class Potion
    {
        private static int[] Tiers = new int[]
        {
            0, 10, 20, 30, 40, 50,
            60, 75, 90, 105, 115, 130,
            150, 170, 195, 215, 235, 260,
            290, 315, 345, 370, 400, 430,
            470, 505, 545, 580, 620, 660,
            720, 800, 875, 960, 1040, 1125,
        };

        public Ingredient[] Ingredients { get; }

        public int TotalMagimins { get; }

        public int Tier { get; }

        public bool? Taste { get; }
        public bool? Sensation { get; }
        public bool? Aroma { get; }
        public bool? Visual { get; }
        public bool? Sound { get; }

        public int TotalTraits { get; }

        public double Score { get; }

        private bool? CalculateTrait(IEnumerable<bool?> values)
        {
            bool? feeling = null;
            foreach (var value in values)
            {
                if (value == false)
                    return false;
                if (value == true)
                    feeling = true;
            }
            return feeling;
        }

        public Potion(Ingredient[] ingredients, PotionRecipe potionRecipe)
        {
            Ingredients = ingredients;
            TotalMagimins = ingredients.Sum(i => i.TotalMagimins);
            var magiminsA = ingredients.Sum(i => i.MagiminsA);
            var magiminsB = ingredients.Sum(i => i.MagiminsB);
            var magiminsC = ingredients.Sum(i => i.MagiminsC);
            var magiminsD = ingredients.Sum(i => i.MagiminsD);
            var magiminsE = ingredients.Sum(i => i.MagiminsE);

            double ratioA = (double)magiminsA / TotalMagimins;
            double ratioB = (double)magiminsB / TotalMagimins;
            double ratioC = (double)magiminsC / TotalMagimins;
            double ratioD = (double)magiminsD / TotalMagimins;
            double ratioE = (double)magiminsE / TotalMagimins;

            double ratiosOffBy = 0;
            ratiosOffBy += Math.Abs(ratioA - potionRecipe.RatioA);
            ratiosOffBy += Math.Abs(ratioB - potionRecipe.RatioB);
            ratiosOffBy += Math.Abs(ratioC - potionRecipe.RatioC);
            ratiosOffBy += Math.Abs(ratioD - potionRecipe.RatioD);
            ratiosOffBy += Math.Abs(ratioE - potionRecipe.RatioE);

            double normalizedRatiosOffBy = (5.0 - ratiosOffBy) / 5.0;

            int tier = Array.BinarySearch(Tiers, TotalMagimins);
            if (tier < 0)
                tier = -tier - 1;

            if (normalizedRatiosOffBy == 0)
                tier += 3;
            else if (normalizedRatiosOffBy >= 1)
                tier += 2;
            else if (normalizedRatiosOffBy >= 1)
                tier += 1;
            else
                tier = 0;

            Tier = tier;

            Taste = CalculateTrait(ingredients.Select(i => i.Taste));
            Sensation = CalculateTrait(ingredients.Select(i => i.Sensation));
            Aroma = CalculateTrait(ingredients.Select(i => i.Aroma));
            Visual = CalculateTrait(ingredients.Select(i => i.Visual));
            Sound = CalculateTrait(ingredients.Select(i => i.Sound));

            int totalTraits = 0;
            foreach (var trait in new[] { Taste, Sensation, Aroma, Visual, Sound }) 
            {
                if (trait == true)
                {
                    totalTraits++;
                }
                else if (trait == false)
                {
                    totalTraits--;
                }
            }
            TotalTraits = totalTraits;

            if (TotalMagimins == 0)
            {
                Score = 0;
            }
            else
            {
                double score = Tier;
                for (int i = 0; i < TotalTraits; i++)
                    score *= 1.25;
                Score = score;
            }
        }
    }

    public record PotionRecipe(string Name, int MagiminsA, int MagiminsB, int MagiminsC, int MagiminsD, int MagiminsE)
    {
        public int TotalMagimins => MagiminsA + MagiminsB + MagiminsC + MagiminsD + MagiminsE;
        public double RatioA => (double)MagiminsA / TotalMagimins;
        public double RatioB => (double)MagiminsB / TotalMagimins;
        public double RatioC => (double)MagiminsC / TotalMagimins;
        public double RatioD => (double)MagiminsD / TotalMagimins;
        public double RatioE => (double)MagiminsE / TotalMagimins;
    }
}
