using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

class DiceGame
{
    static void Main(string[] args)
    {
        // Validate command-line arguments
        if (args.Length < 3)
        {
            Console.WriteLine("Error: Provide at least 3 dice configurations as command-line arguments.");
            Console.WriteLine("Example: game.exe 2,2,4,4,9,9 6,8,1,1,8,6 7,5,3,7,5,3");
            return;
        }

        int[][] dice;
        try
        {
            dice = args.Select((arg, index) =>
            {
                try
                {
                    var parsedDice = arg.Split(',').Select(int.Parse).ToArray();
                    if (parsedDice.Length != 6)
                        throw new Exception($"Dice at argument {index + 1} must have exactly 6 integers.");
                    return parsedDice;
                }
                catch (FormatException)
                {
                    throw new Exception($"Dice at argument {index + 1} contains invalid numbers.");
                }
            }).ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Input Error: {ex.Message}");
            return;
        }

        // Determine who makes the first move
        Console.WriteLine("Let's determine who makes the first move.");
        var (randomValue, hmacKey, hmac) = GenerateFairRandom(0, 1);
        Console.WriteLine($"I selected a random value in the range 0..1 (HMAC-{hmac}).");
        Console.Write("Choose a number between 0 and 1: ");
        var userGuessInput = Console.ReadLine();
        if (string.IsNullOrEmpty(userGuessInput) || !int.TryParse(userGuessInput, out var userGuessValue) || userGuessValue < 0 || userGuessValue > 1)
        {
            Console.WriteLine("Input Error: You must enter 0 or 1.");
            return;
        }
        Console.WriteLine($"My selection: {randomValue} (KEY-{hmacKey}).");
        if (userGuessValue == randomValue)
        {
            Console.WriteLine("You guessed correctly. You make the first move.");
        }
        else
        {
            Console.WriteLine("You guessed wrong. I make the first move.");
        }

        while (true)
        {
            // Display dice options
            Console.WriteLine("Choose your dice:");
            for (int i = 0; i < dice.Length; i++)
                Console.WriteLine($"{i}: [{string.Join(",", dice[i])}]");
            Console.WriteLine("X: exit, ?: help");

            // Handle user dice selection
            Console.Write("Your selection: ");
            var input = Console.ReadLine();
            if (input?.ToUpper() == "X") return;
            if (input?.ToUpper() == "?")
            {
                DisplayProbabilityTable(dice);
                continue;
            }

            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out var userChoice) || userChoice < 0 || userChoice >= dice.Length)
            {
                Console.WriteLine($"Input Error: Please select a valid dice index between 0 and {dice.Length - 1}.");
                continue;
            }
            Console.WriteLine($"You choose the dice: [{string.Join(",", dice[userChoice])}]");

            // Ensure the computer does not select the same dice as the user
            var remainingChoices = Enumerable.Range(0, dice.Length).Where(i => i != userChoice).ToArray();
            var computerChoice = remainingChoices[GenerateSecureRandomInt(0, remainingChoices.Length - 1)];
            Console.WriteLine($"I choose the dice: [{string.Join(",", dice[computerChoice])}]");

            // Simulate throws
            Console.WriteLine("It's time for my throw.");
            var (compValue, compKey, compHmac) = GenerateFairRandom(0, 5);
            Console.WriteLine($"I selected a random value in the range 0..5 (HMAC-{compHmac}).");
            Console.Write("Choose a number between 0 and 5: ");
            var userNumInput = Console.ReadLine();
            if (string.IsNullOrEmpty(userNumInput) || !int.TryParse(userNumInput, out var userNum) || userNum < 0 || userNum > 5)
            {
                Console.WriteLine("Input Error: You must enter a number between 0 and 5.");
                return;
            }
            Console.WriteLine($"My number is {compValue} (KEY-{compKey}).");
            var compThrow = (userNum + compValue) % 6;
            Console.WriteLine($"The result is {userNum}+{compValue}={compThrow} (mod 6).");
            Console.WriteLine($"My throw: {compThrow}.");

            Console.WriteLine("It's time for your throw.");
            var (userValue, userKey, userHmac) = GenerateFairRandom(0, 5);
            Console.WriteLine($"I selected a random value in the range 0..5 (HMAC-{userHmac}).");
            Console.Write("Choose a number between 0 and 5: ");
            var compNumInput = Console.ReadLine();
            if (string.IsNullOrEmpty(compNumInput) || !int.TryParse(compNumInput, out var compNum) || compNum < 0 || compNum > 5)
            {
                Console.WriteLine("Input Error: You must enter a number between 0 and 5.");
                return;
            }
            Console.WriteLine($"My number is {userValue} (KEY-{userKey}).");
            var userThrow = (compNum + userValue) % 6;
            Console.WriteLine($"The result is {compNum}+{userValue}={userThrow} (mod 6).");
            Console.WriteLine($"Your throw: {userThrow}.");

            if (userThrow > compThrow)
                Console.WriteLine("You win!");
            else if (userThrow < compThrow)
                Console.WriteLine("I win!");
            else
                Console.WriteLine("It's a tie!");

            break;
        }
    }

    static (int, string, string) GenerateFairRandom(int min, int max)
    {
        var key = GenerateSecureKey();
        var randomValue = GenerateSecureRandomInt(min, max);

        using var hmac = new HMACSHA256(key);
        var hmacValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(randomValue.ToString()));
        return (randomValue, Convert.ToBase64String(key), Convert.ToHexString(hmacValue));
    }

    static byte[] GenerateSecureKey()
    {
        var key = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return key;
    }

    static int GenerateSecureRandomInt(int min, int max)
    {
        if (min > max) throw new ArgumentOutOfRangeException(nameof(min), "min must be less than or equal to max.");
        var diff = max - min + 1;

        var buffer = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        do
        {
            rng.GetBytes(buffer);
            var randomValue = BitConverter.ToInt32(buffer, 0) & int.MaxValue;
            if (randomValue < (int.MaxValue - (int.MaxValue % diff)))
            {
                return min + (randomValue % diff);
            }
        } while (true);
    }

    static void DisplayProbabilityTable(int[][] dice)
    {
        Console.WriteLine("\nProbability of the win for the user:");
        Console.Write("User dice v".PadRight(15));
        foreach (var d in dice)
        {
            Console.Write($"[{string.Join(",", d)}]".PadRight(20));
        }
        Console.WriteLine();
        Console.WriteLine(new string('-', dice.Length * 20 + 15));

        for (int i = 0; i < dice.Length; i++)
        {
            Console.Write($"[{string.Join(",", dice[i])}]".PadRight(15));
            for (int j = 0; j < dice.Length; j++)
            {
                if (i == j)
                {
                    var selfProb = CalculateProbability(dice[i], dice[j]);
                    Console.Write($"--({selfProb:0.0000})".PadRight(20));
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
        foreach (var userFace in userDice)
        {
            foreach (var computerFace in computerDice)
            {
                if (userFace > computerFace) wins++;
            }
        }
        return (double)wins / total;
    }
}
