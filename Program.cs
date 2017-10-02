using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace snesclassicalert
{
    class Program
    {
        static void Main(string[] args)
        {
            string usage = "Usage: SNESHunter.exe [-cypher]";

            switch (args.Length)
            {
                case 0:
                    startMonitor();
                    break;
                case 1:
                    if (args[0].ToUpper().Trim() == "-CYPHER")
                        getEncryptedPassword();
                    else
                        Console.WriteLine("Invalid argument: " + args[0] + "\r\n" + usage);
                    break;
                default:
                    Console.WriteLine("Wrong number of arguments\r\n" + usage);
                    break;
            }
        }

        static void getEncryptedPassword()
        {
            string plainText = string.Empty;
            bool passwordEntered = false;

            do
            {
                Console.Write("Enter your Email account password: ");
                plainText = getPasswordMasked();
                if (plainText == string.Empty)
                    Console.WriteLine("\r\nPassword cannot be empty\r\n");
                else
                    passwordEntered = true;
            }
            while (!passwordEntered);

            string cypherText = Crypto.EncryptStringAES(plainText);
            Console.WriteLine("\r\n\r\nYour encrypted Email password is: " + cypherText + "\r\n\r\nPress any key to exit.");
            Console.ReadKey();
        }

        static string getPasswordMasked()
        {
            ConsoleKeyInfo key;
            string pass = string.Empty;

            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            while (key.Key != ConsoleKey.Enter);

            return pass;
        }

        static void startMonitor()
        {
            Merchant.emailHandler = new EmailHandler();
            if (!Merchant.emailHandler.usable)
                return;

            List<Merchant> merchants = new List<Merchant>();
            Merchant m;
            foreach (string record in Properties.Settings.Default.Merchants)
            {
                m = new Merchant(record);
                if (m.usable)
                    merchants.Add(m);
            }

            while (true)
            {
                foreach (Merchant merchant in merchants)
                {
                    Console.Write(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " - Checking merchant " + merchant.name + "... ");
                    merchant.checkAvailability();
                    Console.Write((merchant.inStock ? "In Stock!" : "Sold out...") + "\r\n");
                }
                Console.WriteLine("");
                System.Threading.Thread.Sleep(60000);
            }
        }
    }
}
