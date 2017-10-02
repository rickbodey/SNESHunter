using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Mail;

namespace snesclassicalert
{
    public class EmailHandler
    {
        private static string rfc_2822_pattern = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";

        private  string userName,
                        password;

        private SmtpClient smtpClient;
        private MailMessage message;

        public bool usable;

        public EmailHandler()
        {
            string sFromEmail,
                   sToEmail,
                   sToName,
                   smtp_server;

            int smtp_port = 0;

            bool enableSSL = false;

            MailAddress from_address,
                        to_address;

            //Get Email FROM address
            try
            {
                sFromEmail = Properties.Settings.Default.Email_From_Address;
                if (!Regex.IsMatch(sFromEmail, rfc_2822_pattern, RegexOptions.IgnoreCase))
                {
                    Console.WriteLine(sFromEmail + ": Invalid Email FROM address in .config.");
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                sFromEmail = getEmail("Enter Email FROM address");
            }

            //Get Email TO address
            try
            {
                sToEmail = Properties.Settings.Default.Email_To_Address;
                if (!Regex.IsMatch(sToEmail, rfc_2822_pattern, RegexOptions.IgnoreCase))
                {
                    Console.WriteLine(sFromEmail + ": Invalid Email TO address in .config.");
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                sToEmail = getEmail("Enter Email TO address");

            }

            //Get Email TO name
            try
            {
                sToName = Properties.Settings.Default.Email_To_Name;
            }
            catch (Exception)
            {
                Console.Write("Enter Email TO name: ");
                sToName = Console.ReadLine();
            }

            //Get SMTP server
            try
            {
                smtp_server = Properties.Settings.Default.Email_Server;
                if (!isValidFQDN(smtp_server))
                {
                    Console.WriteLine(smtp_server + ": Specified mail server is not a valid FQDN");
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                bool validSMTPServer = false;
                do
                {
                    Console.Write("Enter mail server: ");
                    smtp_server = Console.ReadLine();
                    if (!isValidFQDN(smtp_server))
                        Console.WriteLine(smtp_server + ": not a valid FQDN.");
                    else
                        validSMTPServer = true;
                }
                while (!validSMTPServer);
            }

            //Get SMTP port
            try
            {
                smtp_port = Properties.Settings.Default.Email_Port;
                if (!checkPort(smtp_port))
                    throw new Exception();
            }
            catch (Exception)
            {
                string sPort;
                bool isValidPort = false;

                do
                {
                    Console.Write("Enter mail server port number: ");
                    sPort = Console.ReadLine();
                    if (!Regex.IsMatch(sPort, "^[0-9]+$"))
                        Console.WriteLine(sPort + ": not a valid port number.");
                    else
                    {
                        smtp_port = Convert.ToInt16(sPort);
                        isValidPort = checkPort(smtp_port);
                    }
                }
                while (!isValidPort);
            }

            //Get Username
            try
            {
                userName = Properties.Settings.Default.Email_Username;
            }
            catch (Exception)
            {
                Console.Write("Enter Email account username: ");
                userName = Console.ReadLine();
            }

            //Get Password
            try
            {
                string plainText;
                password = Properties.Settings.Default.Email_Password;
                plainText = Crypto.DecryptStringAES(password);
                plainText = RandomString();
            }
            catch (Exception ex)
            {
                if (ex is FormatException)
                    Console.WriteLine("Invalid password cryptogram in .config");

                Console.Write("Enter Email account password: ");
                password = Crypto.EncryptStringAES(getPasswordMasked());
            }

            //Check for SSL
            try
            {
                enableSSL = Properties.Settings.Default.Email_Enable_SSL;
            }
            catch (Exception)
            {
                bool validResponse = false;
                do
                {
                    Console.Write("Do you want to enable SSL encryption for Email notifications? (Y/N): ");
                    char response = Console.ReadKey(true).KeyChar;
                    
                    switch (response)
                    {
                        case 'y':
                            enableSSL = true;
                            validResponse = true;
                            break;
                        case 'Y':
                            enableSSL = true;
                            validResponse = true;
                            break;
                        case 'n':
                            enableSSL = false;
                            validResponse = true;
                            break;
                        case 'N':
                            enableSSL = false;
                            validResponse = true;
                            break;
                        default:
                            Console.WriteLine(response + ": Not a valid response.");
                            validResponse = false;
                            break;
                    }
                }
                while (!validResponse);
            }

            try
            {
                from_address = new MailAddress(sFromEmail);
                to_address = new MailAddress(sToEmail, sToName);

                smtpClient = new SmtpClient(smtp_server, smtp_port);
                smtpClient.UseDefaultCredentials = false;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.EnableSsl = enableSSL;

                message = new MailMessage(from_address, to_address);
                usable = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to initialize the Email handler:\r\n" + ex.ToString());
                usable = false;
            }
        }

        public void sendAlert(string merchantName, bool available)
        {
            message.Subject = merchantName + ": " + (available ? "SNES Classic in stock!!" : "SNES Classic out of stock...");
            message.Body = message.Subject;
            smtpClient.Credentials = new System.Net.NetworkCredential(Properties.Settings.Default.Email_Username,
                                                                Crypto.DecryptStringAES(password));
            smtpClient.Send(message);
        }

        private static string getEmail(string prompt)
        {
            bool validEmail = false;
            string emailAddress;

            do
            {
                Console.Write(prompt + ": ");
                emailAddress = Console.ReadLine();
                if (Regex.IsMatch(emailAddress, rfc_2822_pattern, RegexOptions.IgnoreCase))
                    validEmail = true;
                else
                    Console.WriteLine(emailAddress + ": not a valid Email address.");
            }
            while (!validEmail);

            return emailAddress;
        }

        private static bool isValidFQDN(string pattern)
        {
            Uri uri = new Uri(pattern);
            return uri.IsAbsoluteUri;
        }

        private static bool checkPort(int portNumber)
        {
            if (portNumber < 0 | portNumber > 65535)
            {
                Console.WriteLine(portNumber + ": not a valid port number.");
                return false;
            }
            else
                return true;
        }

        public static string getPasswordMasked()
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

        public static string RandomString()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 256)
              .Select(s => s[(new Random()).Next(s.Length)]).ToArray());
        }
    }
}
