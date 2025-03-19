using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

class DiceGame
{
    static void Main(string[] args)
    {
        
        if (args.Length < 3)
        {
            Console.WriteLine("Error: Provide at least 3 dice configurations as command-line arguments.");
            Console.WriteLine("Example: game.exe 2,2,4,4,9,9 6,8,1,1,8,6 7,5,3,7,5,3");
            return;
        }

        int[][] dice;
        try
        {
            dice = args.Select(arg => arg.Split(',').Select(int.Parse).ToArray()).ToArray();
            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i].Length != 6)
                    throw new Exception($"Dice at argument {i + 1} must have exactly 6 integers.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Input Error: {ex.Message}");
            return;
        }

     
        Console.WriteLine("Let's determine who makes the first move.");
        var (randomValue, hmacKey, hmac) = GenerateFairRandom(0, 1);
        Console.WriteLine($"I selected a random value in the range 0..1 (HMAC-{hmac}).");
        Console.Write("Choose a number between 0 and 1: ");
        if (!int.TryParse(Console.ReadLine(), out var userGuessValue) || userGuessValue < 0 || userGuessValue > 1)
        {
            Console.WriteLine("Input Error: You must enter 0 or 1.");
            return;
        }
        Console.WriteLine($"My selection: {randomValue} (KEY-{hmacKey}).");
        Console.WriteLine(userGuessValue == randomValue ? "You guessed correctly. You make the first move." : "You guessed wrong. I make the first move.");

        while (true)
        {
            
            Console.WriteLine("Choose your dice:");
            for (int i = 0; i < dice.Length; i++)
                Console.WriteLine($"{i}: [{string.Join(",", dice[i])}]");
            Console.WriteLine("X: exit, ?: help");

            
            Console.Write("Your selection: ");
            var input = Console.ReadLine();
            if (input?.ToUpper() == "X") return;
            if (input?.ToUpper() == "?")
            {
                DisplayProbabilityTable(dice);
                continue;
            }
            if (!int.TryParse(input, out var userChoice) || userChoice < 0 || userChoice >= dice.Length)
            {
                Console.WriteLine($"Input Error: Please select a valid dice index between 0 and {dice.Length - 1}.");
                continue;
            }
            Console.WriteLine($"You choose the dice: [{string.Join(",", dice[userChoice])}]");

           
            var remainingChoices = Enumerable.Range(0, dice.Length).Where(i => i != userChoice).ToArray();
            var computerChoice = remainingChoices[GenerateSecureRandomInt(0, remainingChoices.Length - 1)];
            Console.WriteLine($"I choose the dice: [{string.Join(",", dice[computerChoice])}]");

            
            Console.WriteLine("It's time for my throw.");
            var (compValue, compKey, compHmac) = GenerateFairRandom(0, 5);
            Console.WriteLine($"I selected a random value in the range 0..5 (HMAC-{compHmac}).");
            Console.Write("Choose a number between 0 and 5: ");
            if (!int.TryParse(Console.ReadLine(), out var userNum) || userNum < 0 || userNum > 5)
            {
                Console.WriteLine("Input Error: You must enter a number between 0 and 5.");
                return;
            }
            Console.WriteLine($"My number is {compValue} (KEY-{compKey}).");
            var compThrow = dice[computerChoice][(userNum + compValue) % 6]; 
            Console.WriteLine($"The result is {userNum}+{compValue}={(userNum + compValue) % 6} (mod 6).");
            Console.WriteLine($"My throw: {compThrow}.");

            Console.WriteLine("It's time for your throw.");
            var (userValue, userKey, userHmac) = GenerateFairRandom(0, 5);
            Console.WriteLine($"I selected a random value in the range 0..5 (HMAC-{userHmac}).");
            Console.Write("Choose a number between 0 and 5: ");
            if (!int.TryParse(Console.ReadLine(), out var compNum) || compNum < 0 || compNum > 5)
            {
                Console.WriteLine("Input Error: You must enter a number between 0 and 5.");
                return;
            }
            Console.WriteLine($"My number is {userValue} (KEY-{userKey}).");
            var userThrow = dice[userChoice][(compNum + userValue) % 6];
            Console.WriteLine($"The result is {compNum}+{userValue}={(compNum + userValue) % 6} (mod 6).");
            Console.WriteLine($"Your throw: {userThrow}.");

            Console.WriteLine(userThrow > compThrow ? "You win!" : userThrow < compThrow ? "I win!" : "It's a tie!");
            break;
        }
    }

    static (int, string, string) GenerateFairRandom(int min, int max)
    {
        var key = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        var randomValue = GenerateSecureRandomInt(min, max);
        using var hmac = new HMACSHA256(key);
        var hmacValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(randomValue.ToString()));
        return (randomValue, Convert.ToBase64String(key), Convert.ToHexString(hmacValue));
    }

    static int GenerateSecureRandomInt(int min, int max)
    {
        var buffer = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        while (true)
        {
            rng.GetBytes(buffer);
            var rand = BitConverter.ToInt32(buffer, 0) & int.MaxValue;
            if (rand < int.MaxValue - (int.MaxValue % (max - min + 1)))
                return min + (rand % (max - min + 1));
        }
    }

    static void DisplayProbabilityTable(int[][] dice)
    {
        Console.WriteLine("\nProbability of the win for the user:");
        Console.Write("User dice v".PadRight(15));
        foreach (var d in dice)
            Console.Write($"[{string.Join(",", d)}]".PadRight(20));
        Console.WriteLine();
        Console.WriteLine(new string('-', dice.Length * 20 + 15));

        for (int i = 0; i < dice.Length; i++)

        
        {
           Console.Write($"[{string.Join(",", dice[i])}]".PadRight(15));
for (int j = 0; j < dice.Length; j++)
{
    if (i == j)
    {
        Console.Write($"--({CalculateProbability(dice[i], dice[j]):0.0000})".PadRight(20));
    }
    else
    {
        Console.Write($"{CalculateProbability(dice[i], dice[j]):0.0000}".PadRight(20));
    }
}
Console.WriteLine();

        }
    }

    static double CalculateProbability(int[] userDice, int[] computerDice)
    {
        int wins = 0, total = userDice.Length * computerDice.Length;
        foreach (var uf in userDice)
            foreach (var cf in computerDice)
                if (uf > cf) wins++;
        return (double)wins / total;
    }
}
