using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Potionomics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
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

        private ObservableCollection<Potion> _potions = new();
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

        [ObservableProperty]
        public int _maximumGeneration;

        [ObservableProperty]
        public int _maximumTimeInSeconds;

        public AsyncRelayCommand CalculateBestPotions { get; }
        public RelayCommand CancelCalculateBestPotions { get; }

        public AsyncRelayCommand CalculateAllBestPotions { get; }
        public RelayCommand CancelCalculateAllBestPotions { get; }

        public AsyncRelayCommand SaveResults { get; }
        public RelayCommand CancelSaveResults { get; }

        private readonly Random _random = new Random();

        public PotionomicsViewModel()
        {
            var model = new PotionomicsModel();

            AllIngredients = model.Ingredients.ToArray();
            NoneIngredient = model.NoneIngredient;
            AllCauldrons = model.Cauldrons.ToArray();
            AllPotionRecipes = model.PotionRecipes.ToArray();
            AllLocations = model.Locations.ToArray();

            _selectedCauldron = AllCauldrons.Single(c => c.Name == "Dragon \u002B\u002B Cauldron");
            _selectedPotionRecipe = AllPotionRecipes.First();
            _selectedLocation = AllLocations.Single(l => l == "Crater");

            _displaySize = 10;
            _generationSize = 50;
            _mutationChance = 0.3;
            _automaticPromotionCount = _generationSize / 10;
            _singleIngredientPromotionCount = _generationSize / 5;
            _maximumGeneration = 10000;
            _maximumTimeInSeconds = 0;

            Potions = new ReadOnlyObservableCollection<Potion>(_potions);

            CalculateBestPotions = new AsyncRelayCommand(OnCalculateBestPotionsAsync);
            CancelCalculateBestPotions = new RelayCommand(OnCancelCalculateBestPotions);

            CalculateAllBestPotions = new AsyncRelayCommand(OnCalculateAllBestPotionsAsync);
            CancelCalculateAllBestPotions = new RelayCommand(OnCancelCalculateAllBestPotionsAsync);

            SaveResults = new AsyncRelayCommand(OnSaveResultsAsync);
            CancelSaveResults = new RelayCommand(OnCancelSaveResultsAsync);
        }

        private bool AddIngredientIfPossible(Ingredient[] ingredients, int index, Ingredient ingredient, ref int totalMagimins)
        {
            if ((totalMagimins + ingredient.TotalMagimins) <= SelectedCauldron.MaxMagimins)
            {
                totalMagimins += ingredient.TotalMagimins;
                ingredients[index] = ingredient;
                return true;
            }
            else
            {
                ingredients[index] = NoneIngredient;
                return false;
            }
        }

        private Potion GetGreedyPotion(Ingredient[] ingredients)
        {
            bool FilterByMagimins(Ingredient i, Func<IHasMagimins, int> ExtractMagimins)
            {
                var recipeMagimins = ExtractMagimins(SelectedPotionRecipe);
                var ingredientMagimins = ExtractMagimins(i);
                if (recipeMagimins == 0 && ingredientMagimins > 0)
                    return false;

                return true;
            }

            var filteredIngredients = ingredients
                .Where(i => FilterByMagimins(i, hm => hm.MagiminsA))
                .Where(i => FilterByMagimins(i, hm => hm.MagiminsB))
                .Where(i => FilterByMagimins(i, hm => hm.MagiminsC))
                .Where(i => FilterByMagimins(i, hm => hm.MagiminsD))
                .Where(i => FilterByMagimins(i, hm => hm.MagiminsE))
                .OrderByDescending(i => i.TotalMagimins)
                .ToArray();

            double[] probabilities = CalculateProbabilities(filteredIngredients, i => i.TotalMagimins);

            return GetPotion(() => SelectBasedOnProbabilities(filteredIngredients, probabilities));
        }

        private Potion GetSingleIngredientPotion(Ingredient ingredient)
        {
            var ingredients = new Ingredient[SelectedCauldron.MaxIngredients];
            int totalMagimins = 0;
            for (int ingredientsIndex = 0; ingredientsIndex < ingredients.Length; ingredientsIndex++)
                AddIngredientIfPossible(ingredients, ingredientsIndex, ingredient, ref totalMagimins);

            return new Potion(ingredients, SelectedPotionRecipe, SelectedCauldron);
        }

        private Potion GetRandomPotion(Ingredient[] availableIngredients) => GetPotion(() => availableIngredients[_random.Next(availableIngredients.Length)]);

        private Potion GetPotion(Func<Ingredient> selector)
        {
            var ingredients = new Ingredient[SelectedCauldron.MaxIngredients];
            int totalMagimins = 0;
            for (int ingredientsIndex = 0; ingredientsIndex < ingredients.Length; ingredientsIndex++)
            {
                var ingredient = selector();
                AddIngredientIfPossible(ingredients, ingredientsIndex, ingredient, ref totalMagimins);
            }
            return new Potion(ingredients, SelectedPotionRecipe, SelectedCauldron);
        }

        private delegate void UpdatedGeneration(Potion[] currentGeneration, int generationCount, bool finalUpdate);

        private double[] CalculateProbabilities<T>(T[] currentGeneration, Func<T, double> getScore) {

            double[] probabilities = new double[currentGeneration.Length];
            double totalScore = currentGeneration.Sum(c => getScore(c));
            probabilities[0] = getScore(currentGeneration[0]) / totalScore;
            for (int i = 1; i < currentGeneration.Length; i++)
            {
                probabilities[i] = probabilities[i - 1] + getScore(currentGeneration[i]) / totalScore;
            }
            return probabilities;
        }

        private T SelectBasedOnProbabilities<T>(T[] currentGeneration, double[] probabilities)
        {
            double current = _random.NextDouble();
            for (int i = 0; i < currentGeneration.Length; i++)
            {
                if (current < probabilities[i])
                    return currentGeneration[i];
            }
            return currentGeneration[^1];
        }

        private async Task GeneticCalculateBestPotionsAsync(CancellationToken cancellationToken, UpdatedGeneration updatedGeneration)
        {
            var scoreComparer = new PotionScoreComparer();

            var currentGeneration = new Potion[GenerationSize];
            var nextGeneration = new Potion[GenerationSize];

            var locationIndices = AllLocations.Select((l, i) => (l, i)).ToDictionary(t => t.l, t => t.i);

            var availableIngredients = AllIngredients.Where(i => locationIndices[i.Location] <= locationIndices[SelectedLocation]).ToArray();

            // Create random potions for first generation
            await Task.Run(() =>
            {
                int potionsIndex = 0;
                //for (; potionsIndex < availableIngredients.Length && potionsIndex < GenerationSize; potionsIndex++)
                //{
                //    var potion = GetSingleIngredientPotion(availableIngredients[potionsIndex]);
                //    currentGeneration[potionsIndex] = potion;
                //}
                for (; potionsIndex < GenerationSize; potionsIndex++)
                {
                    var potion = GetGreedyPotion(availableIngredients);
                    currentGeneration[potionsIndex] = potion;
                }
                Array.Sort(currentGeneration, scoreComparer);
            });

            TimeSpan maximumTime = new TimeSpan(0, 0, MaximumTimeInSeconds);
            Stopwatch stopwatch = Stopwatch.StartNew();

            GenerationCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                AverageFitness = currentGeneration.Take(DisplaySize).Average(p => p.Score);

                if (MaximumGeneration >= 0 && GenerationCount == MaximumGeneration)
                {
                    break;
                }

                if (maximumTime != TimeSpan.Zero && stopwatch.Elapsed >= maximumTime)
                {
                    break;
                }

                GenerationCount++;

                updatedGeneration?.Invoke(currentGeneration, GenerationCount, false);

                // Calculate all values
                await Task.Run(() =>
                {
                    double[] probabilities = CalculateProbabilities(currentGeneration, c => c.Score);

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
                        var parent1 = SelectBasedOnProbabilities(currentGeneration, probabilities);
                        var parent2 = SelectBasedOnProbabilities(currentGeneration, probabilities);

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
                        nextGeneration[index++] = new Potion(ingredients, SelectedPotionRecipe, SelectedCauldron);
                    }
                    Array.Sort(nextGeneration, scoreComparer);
                    Array.Copy(nextGeneration, currentGeneration, GenerationSize);
                });
            }

            updatedGeneration?.Invoke(currentGeneration, GenerationCount, true);
        }

        private async Task OnCalculateAllBestPotionsAsync(CancellationToken cancellationToken)
        {
            Dictionary<PotionRecipe, Potion> bestPotions = new ();

            foreach (var potionRecipe in AllPotionRecipes)
            {
                SelectedPotionRecipe = potionRecipe;
                await GeneticCalculateBestPotionsAsync(
                    cancellationToken,
                    (currentGeneration, generationCount, finalUpdate) =>
                    {
                        if (finalUpdate)
                        {
                            bestPotions.Add(potionRecipe, currentGeneration.First());
                            UpdatePotionsInUi(bestPotions.Values);
                        }
                    });
            }
        }

        private void OnCancelCalculateAllBestPotionsAsync()
        {
            CalculateAllBestPotions.Cancel();
        }

        private async Task OnCalculateBestPotionsAsync(CancellationToken cancellationToken)
        {
            await GeneticCalculateBestPotionsAsync(
                cancellationToken,
                (currentGeneration, generationCount, finalUpdate) =>
                {
                    if (generationCount % 50 == 0 || finalUpdate)
                        UpdatePotionsInUi(currentGeneration.Take(DisplaySize));
                });
        }

        private void UpdatePotionsInUi(IEnumerable<Potion> potions)
        {
            _potions.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            foreach (var potion in potions)
            {
                _potions.Add(potion);
            }
        }

        private void OnCancelCalculateBestPotions()
        {
            CalculateBestPotions.Cancel();
        }

        private async Task OnSaveResultsAsync(CancellationToken cancellationToken)
        {
            using (FileStream stream = new FileStream("results.json", FileMode.Create, FileAccess.Write))
            {
                JsonSerializerOptions options = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                };
                await JsonSerializer.SerializeAsync(stream, Potions, options, cancellationToken);
            }
        }

        private void OnCancelSaveResultsAsync()
        {
            SaveResults.Cancel();
        }
    }
}
