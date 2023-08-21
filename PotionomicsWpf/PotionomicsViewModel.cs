using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Potionomics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xaml.Schema;

namespace PotionomicsWpf
{
    internal partial class PotionomicsViewModel : ObservableObject
    {
        public Ingredient[] AllIngredients { get; }
        public Cauldron[] AllCauldrons { get; }
        public PotionRecipe[] AllPotionRecipes { get; }
        public string[] AllLocations { get; }

        private ObservableCollection<Potion> _potions = new ();
        public ReadOnlyObservableCollection<Potion> Potions { get; }

        public Ingredient NoneIngredient { get; }

        [ObservableProperty]
        public int _displaySize;

        [ObservableProperty]
        public int _generationSize;

        [ObservableProperty]
        public double _mutationChance;

        [ObservableProperty]
        public int _automaticPromotionCount;

        [ObservableProperty]
        public int _singleIngredientPromotionCount;

        [ObservableProperty]
        public Cauldron _selectedCauldron;

        [ObservableProperty]
        public PotionRecipe _selectedPotionRecipe;

        [ObservableProperty]
        public string _selectedLocation;

        [ObservableProperty]
        public double _averageFitness;

        [ObservableProperty]
        public int _generationCount;

        public AsyncRelayCommand CalculateBestPotions { get; }
        public RelayCommand CancelCalculateBestPotions { get; }

        private readonly Random _random = new Random();

        public PotionomicsViewModel()
        {
            var model = new PotionomicsModel();

            AllIngredients = model.Ingredients.ToArray();
            NoneIngredient = model.NoneIngredient;
            AllCauldrons = model.Cauldrons.ToArray();
            AllPotionRecipes = model.PotionRecipes.ToArray();
            AllLocations = model.Locations.ToArray();

            _selectedCauldron = AllCauldrons.Single(c => c.Name == "Ocean ++ Cauldron");
            _selectedPotionRecipe = AllPotionRecipes.First();
            _selectedLocation = AllLocations.Single(l => l == "Shadow Steppe");

            _displaySize = 10;
            _generationSize = 50;
            _mutationChance = 0.3;
            _automaticPromotionCount = _generationSize / 10;
            _singleIngredientPromotionCount = _generationSize / 5;

            Potions = new ReadOnlyObservableCollection<Potion>(_potions);

            CalculateBestPotions = new AsyncRelayCommand(OnCalculateBestPotionsAsync);
            CancelCalculateBestPotions = new RelayCommand(OnCancelCalculateBestPotions);
        }

        private Potion SelectRandomParent(Potion[] currentGeneration, double[] probabilities) 
        {
            double current = _random.NextDouble();
            for (int i = 0; i < GenerationSize; i++)
            {
                if (current < probabilities[i])
                    return currentGeneration[i];
            }
            return currentGeneration[^1];
        }

        private void AddIngredientIfPossible(Ingredient[] ingredients, int index, Ingredient ingredient, ref int totalMagimins)
        {
            if ((totalMagimins + ingredient.TotalMagimins) <= SelectedCauldron.MaxMagimins)
            {
                totalMagimins += ingredient.TotalMagimins;
                ingredients[index] = ingredient;
            }
            else
            {
                ingredients[index] = NoneIngredient;
            }
        }

        private Potion GetSingleIngredientPotion(Ingredient ingredient)
        {
            var ingredients = new Ingredient[SelectedCauldron.MaxIngredients];
            int totalMagimins = 0;
            for (int ingredientsIndex = 0; ingredientsIndex < ingredients.Length; ingredientsIndex++)
                AddIngredientIfPossible(ingredients, ingredientsIndex, ingredient, ref totalMagimins);

            return new Potion(ingredients, SelectedPotionRecipe);
        }

        private Potion GetRandomPotion(Ingredient[] availableIngredients)
        {
            var ingredients = new Ingredient[SelectedCauldron.MaxIngredients];
            int totalMagimins = 0;
            for (int ingredientsIndex = 0; ingredientsIndex < ingredients.Length; ingredientsIndex++)
            {
                int allIngredientIndex = _random.Next(availableIngredients.Length);
                AddIngredientIfPossible(ingredients, ingredientsIndex, availableIngredients[allIngredientIndex], ref totalMagimins);
            }
            return new Potion(ingredients, SelectedPotionRecipe);
        }

        private async Task OnCalculateBestPotionsAsync(CancellationToken cancellationToken)
        {
            var scoreComparer = new PotionScoreComparer();

            var currentGeneration = new Potion[GenerationSize];
            var nextGeneration = new Potion[GenerationSize];
            var probabilities = new double[GenerationSize];

            var locationIndices = AllLocations.Select((l, i) => (l, i)).ToDictionary(t => t.l, t => t.i);

            var availableIngredients = AllIngredients.Where(i => locationIndices[i.Location] <= locationIndices[SelectedLocation]).ToArray();

            // Create random potions for first generation
            await Task.Run(() =>
            {
                for (int potionsIndex = 0; potionsIndex < availableIngredients.Length && potionsIndex < GenerationSize; potionsIndex++)
                {
                    var potion = GetSingleIngredientPotion(availableIngredients[potionsIndex]);
                    currentGeneration[potionsIndex] = potion;
                }
                for (int potionsIndex = availableIngredients.Length; potionsIndex < GenerationSize; potionsIndex++)
                {
                    var potion = GetRandomPotion(availableIngredients);
                    currentGeneration[potionsIndex] = potion;
                }
                Array.Sort(currentGeneration, scoreComparer);
            });

            GenerationCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                GenerationCount++;
                AverageFitness = currentGeneration.Take(DisplaySize).Average(p => p.Score);

                if (GenerationCount % 50 == 0)
                {
                    // Update the UI
                    UpdatePotionsInUi(currentGeneration);
                }

                // Calculate all values
                await Task.Run(() =>
                {
                    double totalScore = currentGeneration.Sum(c => c.Score);
                    probabilities[0] = currentGeneration[0].Score / totalScore;
                    for (int i = 1; i < GenerationSize; i++)
                    {
                        probabilities[i] = probabilities[i - 1] + currentGeneration[i].Score / totalScore;
                    }

                    int index = 0;
                    for (int i = 0; i < AutomaticPromotionCount; i++)
                    {
                        nextGeneration[index++] = currentGeneration[i];
                    }
                    for (int i = 0; i < SingleIngredientPromotionCount && i < availableIngredients.Length; i++)
                    {
                        int randomIngredientIndex = _random.Next(availableIngredients.Length);
                        var potion = GetSingleIngredientPotion(availableIngredients[randomIngredientIndex]);
                        nextGeneration[index++] = potion;
                    }
                    for (int i = index; i < GenerationSize; i++)
                    {
                        var parent1 = SelectRandomParent(currentGeneration, probabilities);
                        var parent2 = SelectRandomParent(currentGeneration, probabilities);

                        var ingredients = new Ingredient[SelectedCauldron.MaxIngredients];
                        int totalMagimins = 0;
                        for (int ingredientsIndex = 0; ingredientsIndex < ingredients.Length; ingredientsIndex++)
                        {
                            double mutation = _random.NextDouble();
                            if (mutation < MutationChance)
                            {
                                int allIngredientIndex = _random.Next(availableIngredients.Length);
                                AddIngredientIfPossible(ingredients, ingredientsIndex, availableIngredients[allIngredientIndex], ref totalMagimins);
                            }
                            else
                            {
                                double parent = _random.NextDouble();
                                if (parent < 0.5)
                                {
                                    AddIngredientIfPossible(ingredients, ingredientsIndex, parent1.Ingredients[ingredientsIndex], ref totalMagimins);
                                }
                                else
                                {
                                    AddIngredientIfPossible(ingredients, ingredientsIndex, parent2.Ingredients[ingredientsIndex], ref totalMagimins);
                                }
                            }
                        }
                        nextGeneration[index++] = new Potion(ingredients, SelectedPotionRecipe);
                    }
                    Array.Sort(nextGeneration, scoreComparer);
                    Array.Copy(nextGeneration, currentGeneration, GenerationSize);
                });
            }

            UpdatePotionsInUi(currentGeneration);
        }

        private void UpdatePotionsInUi(Potion[] currentGeneration)
        {
            _potions.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int i = 0; i < DisplaySize; i++)
            {
                _potions.Add(currentGeneration[i]);
            }
        }

        private void OnCancelCalculateBestPotions()
        {
            CalculateBestPotions.Cancel();
        }
    }
}
