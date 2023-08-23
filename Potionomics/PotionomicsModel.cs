using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Potionomics
{
    public class PotionomicsModel
    {
        public Ingredient NoneIngredient { get; }

        private List<Ingredient> _ingredients = new List<Ingredient>();
        public IEnumerable<Ingredient> Ingredients => _ingredients;
        
        private List<string> _locations = new List<string>();
        public IEnumerable<string> Locations => _locations;

        private List<PotionRecipe> _potionRecipes = new List<PotionRecipe>();
        public IEnumerable<PotionRecipe> PotionRecipes => _potionRecipes;
        
        private List<Cauldron> _cauldrons = new List<Cauldron>();
        public IEnumerable<Cauldron> Cauldrons => _cauldrons;

        public PotionomicsModel()
        {
            ReadIngredients();
            NoneIngredient = Ingredients.First();
            ReadPotionRecipes();
            ReadCauldrons();
        }

        private void ReadIngredients()
        {
            string fileName = @"Models\Ingredients.json";
            string jsonString = File.ReadAllText(fileName);
            _ingredients.AddRange(JsonSerializer.Deserialize<Ingredient[]>(jsonString)!);
            _locations.AddRange(_ingredients.Select(i => i.Location).Distinct());
        }

        private void ReadPotionRecipes()
        {
            string fileName = @"Models\PotionRecipes.json";
            string jsonString = File.ReadAllText(fileName);
            _potionRecipes.AddRange(JsonSerializer.Deserialize<PotionRecipe[]>(jsonString)!);
        }

        private void ReadCauldrons()
        {
            string fileName = @"Models\Cauldrons.json";
            string jsonString = File.ReadAllText(fileName);
            _cauldrons.AddRange(JsonSerializer.Deserialize<Cauldron[]>(jsonString)!);
        }
    }
}
