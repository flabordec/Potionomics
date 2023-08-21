using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Potionomics
{
    public class PotionomicsModel
    {
        public Ingredient NoneIngredient { get; }

        private List<Ingredient> _ingredients = new List<Ingredient>();
        public IEnumerable<Ingredient> Ingredients => _ingredients;
        
        private HashSet<string> _locations = new HashSet<string>();
        private List<string> _locationsInOrder = new List<string>();
        public IEnumerable<string> Locations => _locationsInOrder;

        private List<PotionRecipe> _potionRecipes = new List<PotionRecipe>();
        public IEnumerable<PotionRecipe> PotionRecipes => _potionRecipes;
        
        private List<Cauldron> _cauldrons = new List<Cauldron>();
        public IEnumerable<Cauldron> Cauldrons => _cauldrons;

        public PotionomicsModel()
        {
            NoneIngredient = new Ingredient("None", 0, "None", 0, 0, 0, 0, 0, 0, null, null, null, null, null, 0, "None");
            ReadIngredients();
            ReadPotionRecipes();
            ReadCauldrons();
        }

        private bool? ReadTrait(HtmlNode traitNode)
        {
            var img = traitNode.SelectSingleNode("div/figure/a/img");
            if (img != null)
            {
                string value = img.Attributes["alt"].Value;
                if (value.EndsWith("positive"))
                {
                    return true;
                }
                else if (value.EndsWith("negative"))
                {
                    return false;
                }
                else
                {
                    throw new Exception($"Unexpected trait: {value}");
                }
            }
            return null;
        }

        private string ReadLocation(HtmlNode locationNode)
        {
            var a = locationNode.SelectSingleNode("a");
            return a.InnerText;
        }

        private void AddIngredient(Ingredient ingredient)
        {
            _ingredients.Add(ingredient);
            if (_locations.Add(ingredient.Location))
            {
                _locationsInOrder.Add(ingredient.Location);
            }
        }

        private void ReadIngredients()
        {
            AddIngredient(NoneIngredient);

            var doc = new HtmlDocument();
            doc.Load(@"Models\Ingredients.html");

            var rows = doc.DocumentNode.SelectNodes("/table/tbody/tr");

            foreach (var row in rows)
            {
                var columns = row.SelectNodes("td").ToArray();
                string name = columns[1].InnerText.Trim();
                int rarity = int.Parse(columns[2].InnerText.Trim());
                string type = columns[3].InnerText.Trim();
                int magiminsA = int.Parse(columns[4].InnerText.Trim());
                int magiminsB = int.Parse(columns[5].InnerText.Trim());
                int magiminsC = int.Parse(columns[6].InnerText.Trim());
                int magiminsD = int.Parse(columns[7].InnerText.Trim());
                int magiminsE = int.Parse(columns[8].InnerText.Trim());
                int magiminsTotal = int.Parse(columns[9].InnerText.Trim());
                bool? taste = ReadTrait(columns[10]);
                bool? sensation = ReadTrait(columns[11]);
                bool? aroma = ReadTrait(columns[12]);
                bool? visual = ReadTrait(columns[13]);
                bool? sound = ReadTrait(columns[14]);
                int basePrice = int.Parse(columns[15].InnerText.Trim());
                string description = columns[16].InnerText.Trim();
                string location = ReadLocation(columns[17]);

                var ingredient = new Ingredient(name, rarity, type, magiminsA, magiminsB, magiminsC, magiminsD, magiminsE, magiminsTotal, taste, sensation, aroma, visual, sound, basePrice, location);
                AddIngredient(ingredient);
            }
        }

        private void ReadPotionRecipes()
        {
            var potionRecipes = new PotionRecipe[]
            {
                new PotionRecipe("Health Potion",      1, 1, 0, 0, 0),
                new PotionRecipe("Mana Potion",        0, 1, 1, 0, 0),
                new PotionRecipe("Stamina Potion",     1, 0, 0, 0, 1),
                new PotionRecipe("Speed Potion",       0, 0, 1, 1, 0),
                new PotionRecipe("Tolerance Potion",   0, 0, 0, 1, 1),
                new PotionRecipe("Fire Tonic",         1, 0, 1, 0, 0),
                new PotionRecipe("Ice Tonic",          1, 0, 0, 1, 0),
                new PotionRecipe("Thunder Tonic",      0, 1, 0, 1, 0),
                new PotionRecipe("Shadow Tonic",       0, 1, 0, 0, 1),
                new PotionRecipe("Radiation Tonic",    0, 0, 1, 0, 1),
                new PotionRecipe("Sight Enhancer",     3, 4, 3, 0, 0),
                new PotionRecipe("Alertness Enhancer", 0, 3, 4, 3, 0),
                new PotionRecipe("Insight Enhancer",   4, 3, 0, 0, 3),
                new PotionRecipe("Dowsing Enhancer",   3, 0, 0, 3, 4),
                new PotionRecipe("Seeking Enhancer",   0, 0, 3, 4, 3),
                new PotionRecipe("Poison Cure",        2, 0, 1, 1, 0),
                new PotionRecipe("Drowsiness Cure",    1, 1, 0, 2, 0),
                new PotionRecipe("Petrification Cure", 1, 0, 2, 0, 1),
                new PotionRecipe("Silence Cure",       0, 2, 1, 0, 1),
                new PotionRecipe("Curse Cure",         0, 1, 1, 0, 2),
            };
            _potionRecipes.AddRange(potionRecipes);
        }

        private void ReadCauldrons()
        {
            var doc = new HtmlDocument();
            doc.Load(@"Models\Cauldrons.html");

            var rows = doc.DocumentNode.SelectNodes("/table/tbody/tr");

            foreach (var row in rows)
            {
                var columns = row.SelectNodes("td").ToArray();
                string name = columns[0].InnerText.Trim();
                string goldCost = columns[1].InnerText.Trim();
                string ingredientCost = columns[2].InnerText.Trim();
                int maxIngredients = int.Parse(columns[3].InnerText.Trim());
                int maxMagimins = int.Parse(columns[4].InnerText.Trim());

                var cauldron = new Cauldron(name, maxIngredients, maxMagimins);
                _cauldrons.Add(cauldron);
            }
        }
    }
}
