using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Potionomics
{
    public interface IHasMagimins
    {
        int MagiminsA { get; }
        int MagiminsB { get; }
        int MagiminsC { get; }
        int MagiminsD { get; }
        int MagiminsE { get; }
    }

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
        string Location) : IHasMagimins;

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

            return y.Score.CompareTo(x.Score);
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

    public class Potion : IHasMagimins
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

        public PotionRecipe Recipe { get; }

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

        public int MagiminsA { get; }
        public int MagiminsB { get; }
        public int MagiminsC { get; }
        public int MagiminsD { get; }
        public int MagiminsE { get; }

        public double NormalizedRatiosOffBy { get; }

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

        public Potion(Ingredient[] ingredients, PotionRecipe potionRecipe, Cauldron cauldron)
        {
            Recipe = potionRecipe;
            Ingredients = ingredients.OrderByDescending(i => i.TotalMagimins).ToArray();
            TotalMagimins = ingredients.Sum(i => i.TotalMagimins);
            MagiminsA = ingredients.Sum(i => i.MagiminsA);
            MagiminsB = ingredients.Sum(i => i.MagiminsB);
            MagiminsC = ingredients.Sum(i => i.MagiminsC);
            MagiminsD = ingredients.Sum(i => i.MagiminsD);
            MagiminsE = ingredients.Sum(i => i.MagiminsE);

            double ratioA = (double)MagiminsA / TotalMagimins;
            double ratioB = (double)MagiminsB / TotalMagimins;
            double ratioC = (double)MagiminsC / TotalMagimins;
            double ratioD = (double)MagiminsD / TotalMagimins;
            double ratioE = (double)MagiminsE / TotalMagimins;

            double ratiosOffBy = 0;
            ratiosOffBy += Math.Abs(ratioA - potionRecipe.RatioA);
            ratiosOffBy += Math.Abs(ratioB - potionRecipe.RatioB);
            ratiosOffBy += Math.Abs(ratioC - potionRecipe.RatioC);
            ratiosOffBy += Math.Abs(ratioD - potionRecipe.RatioD);
            ratiosOffBy += Math.Abs(ratioE - potionRecipe.RatioE);

            NormalizedRatiosOffBy = ratiosOffBy / 5.0;

            int tier = Array.BinarySearch(Tiers, TotalMagimins);
            if (tier < 0)
                tier = -tier - 1;

            if (NormalizedRatiosOffBy == 0)
                tier += 3;
            else if (NormalizedRatiosOffBy <= 0.02)
                tier += 2;
            else if (NormalizedRatiosOffBy <= 0.06)
                tier += 1;
            else if (NormalizedRatiosOffBy < 0.1)
                tier -= 1;
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
                const double traitValue = 0.25;

                double score = Tier;
                foreach (var trait in new[] { Taste, Sensation, Aroma, Visual, Sound })
                {
                    if (trait == true)
                    {
                        score += traitValue;
                    }
                    else if (trait == false)
                    {
                        score -= traitValue;
                    }
                }
                score += (1.0 - NormalizedRatiosOffBy);
                Score = score;
            }
        }
    }

    public record PotionRecipe(string Name, int MagiminsA, int MagiminsB, int MagiminsC, int MagiminsD, int MagiminsE) : IHasMagimins
    {
        public int TotalMagimins => MagiminsA + MagiminsB + MagiminsC + MagiminsD + MagiminsE;
        public double RatioA => (double)MagiminsA / TotalMagimins;
        public double RatioB => (double)MagiminsB / TotalMagimins;
        public double RatioC => (double)MagiminsC / TotalMagimins;
        public double RatioD => (double)MagiminsD / TotalMagimins;
        public double RatioE => (double)MagiminsE / TotalMagimins;
    }
}
