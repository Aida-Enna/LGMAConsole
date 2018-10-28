using FubarDev.FtpServer;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LGMA
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            Task.Run(async () =>
            {
                // Do any async anything you need here without worry
                // Setup dependency injection
                var services = new ServiceCollection();

                //// use %TEMP%/TestFtpServer as root folder
                //services.Configure<DotNetFileSystemOptions>(opt => opt
                //    .RootPath = @"G:\My Drive\NSP");
                if(File.Exists("client_id.json") == false)
                {
                    Console.WriteLine("Please complete the first step on the Github page and get the client_id.json file! Press any key to exit.");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("Please enter your Google Drive email:");
                string Username = Console.ReadLine();
                // Configuration

                var clientSecretsFile = "client_id.json";

                // Loading the credentials
                UserCredential credential;
                using (var secretsSource = new System.IO.FileStream(clientSecretsFile, FileMode.Open))
                {
                    var secrets = GoogleClientSecrets.Load(secretsSource);
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets.Secrets,
                        new[] { DriveService.Scope.DriveFile, DriveService.Scope.Drive },
                        Username,
                        CancellationToken.None);
                }

                // Add FTP server services
                // DotNetFileSystemProvider = Use the .NET file system functionality
                // AnonymousMembershipProvider = allow only anonymous logins
                services.AddFtpServer(builder => builder
                    //.UseDotNetFileSystem() // Use the .NET file system functionality
                    .UseGoogleDrive(credential)
                    .EnableAnonymousAuthentication()); // allow anonymous logins
                string PossibleIP = "";
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (item.OperationalStatus == OperationalStatus.Up)
                    {
                        foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                if (ip.Address.ToString() != "127.0.0.1") PossibleIP = ip.Address.ToString();
                            }
                        }
                    }
                }

                // Configure the FTP server
                Console.WriteLine("Enter the IP address of the current machine (Likely IP: " + PossibleIP + "):");
                string IP = Console.ReadLine();
                Console.WriteLine("Enter the port you want to start the server on:");
                string Port = Console.ReadLine();
                services.Configure<FtpServerOptions>(opt => opt.ServerAddress = IP);
                services.Configure<FtpServerOptions>(opt => opt.Port = Convert.ToInt32(Port));

                // Build the service provider
                using (var serviceProvider = services.BuildServiceProvider())
                {
                    // Initialize the FTP server
                    var ftpServer = serviceProvider.GetRequiredService<IFtpServer>();

                    // Start the FTP server
                    ftpServer.Start();
                    Console.WriteLine("Add a new entry to your switch/dz/locations.conf as shown below:");
                    Console.WriteLine("{" + Environment.NewLine + "        \"title\":\"LGMA\"," + Environment.NewLine + "        \"url\": \"" + "ftp://" + IP + ":" + Port + "/\"" + Environment.NewLine + "}");
                    Console.WriteLine("---SERVER ONLINE---");
                    Console.WriteLine("Press ENTER/RETURN to close the server.");
                    Console.ReadLine();

                    // Stop the FTP server
                    ftpServer.Stop();
                }
            }).GetAwaiter().GetResult();
        }
    }
}