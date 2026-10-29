using System;

namespace TastePassport.Gameplay
{
    public enum IngredientResult { Correct, Wrong, Complete }

    public sealed class RecipeRun
    {
        public RecipeDefinition Recipe { get; }
        public string[] Choices { get; }
        public int StepIndex { get; private set; }
        public int Mistakes { get; private set; }
        public bool Complete => StepIndex == Recipe.Steps.Length;
        public int Score => Math.Max(100, 300 - Mistakes * 35);
        public int Stars => Mistakes == 0 ? 3 : Mistakes <= 2 ? 2 : 1;

        public RecipeRun(RecipeDefinition recipe, int seed)
        {
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            if (recipe.Steps == null || recipe.Steps.Length == 0 || recipe.Decoys == null)
                throw new ArgumentException("Recipe needs steps and decoys.", nameof(recipe));
            Choices = new string[recipe.Steps.Length + recipe.Decoys.Length];
            recipe.Steps.CopyTo(Choices, 0);
            recipe.Decoys.CopyTo(Choices, recipe.Steps.Length);
            var random = new Random(seed);
            for (int i = Choices.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string swap = Choices[i]; Choices[i] = Choices[j]; Choices[j] = swap;
            }
        }

        public IngredientResult Choose(int index)
        {
            if (index < 0 || index >= Choices.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (Complete) return IngredientResult.Complete;
            if (Choices[index] != Recipe.Steps[StepIndex]) { Mistakes++; return IngredientResult.Wrong; }
            StepIndex++;
            return Complete ? IngredientResult.Complete : IngredientResult.Correct;
        }
    }
}
