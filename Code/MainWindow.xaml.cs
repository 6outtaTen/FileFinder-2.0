using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Controls;
using System.IO;
using System.Windows.Forms;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ookii.Dialogs.Wpf;

namespace FileFinder
{
    // All the following code (besides 1 class) is written by: 6outtaTen, github: https://github.com/6outtaTen
    public partial class MainWindow : Window
    {
        public int mode;
        public string path = "";
        public static string scanFolder = $"{Directory.GetCurrentDirectory()}\\Scans";
        public string startsWith = System.IO.Path.Combine(scanFolder, "Starts_With_Scan_Results");
        public string endsWith = System.IO.Path.Combine(scanFolder, "Ends_With_Scan_Results");
        public string contains = System.IO.Path.Combine(scanFolder, "Contains_Scan_Results");
        public string specific = System.IO.Path.Combine(scanFolder, "Specific_Scan_Results");

        public MainWindow()
        { 
            InitializeComponent();
            CenterWindowOnScreen();

            // Create neccessary folders
  
            Directory.CreateDirectory(startsWith);
            Directory.CreateDirectory(endsWith);
            Directory.CreateDirectory(contains);
            Directory.CreateDirectory(specific);
        }

        // The following class is NOT written by me - all the credit goes out to this GitHub repo: https://github.com/astrohart/FileSystemEnumerable/

        public class FileSystemEnumerable : IEnumerable<FileSystemInfo>
        {
            //private ILog _logger = LogManager.GetLogger(typeof(FileSystemEnumerable));

            private readonly DirectoryInfo _root;
            private readonly IList<string> _patterns;
            private readonly SearchOption _option;

            public static IEnumerable<FileSystemInfo> Search(DirectoryInfo root, string pattern = "*",
                SearchOption option = SearchOption.AllDirectories)
            {
                if (!root.Exists)
                    throw new DirectoryNotFoundException($"The folder '{root.FullName}' could not be located.");

                /* If the search pattern string is blank, then default to the wildcard (*) pattern. */
                if (string.IsNullOrWhiteSpace(pattern))
                    pattern = "*";

                return new FileSystemEnumerable(root, pattern, option);
            }

            public static IEnumerable<FileSystemInfo> Search(string root, string pattern = "*",
                SearchOption option = SearchOption.AllDirectories)
            {
                if (!Directory.Exists(root))
                    throw new DirectoryNotFoundException($"The folder '{root}' could not be located.");

                /* If the search pattern string is blank, then default to the wildcard (*) pattern. */
                if (string.IsNullOrWhiteSpace(pattern))
                    pattern = "*";

                var rootDirectoryInfo = new DirectoryInfo(root);
                return new FileSystemEnumerable(rootDirectoryInfo, pattern, option);
            }

            public FileSystemEnumerable(DirectoryInfo root, string pattern, SearchOption option)
            {
                _root = root;
                _patterns = new List<string> { pattern };
                _option = option;
            }

            public FileSystemEnumerable(DirectoryInfo root, IList<string> patterns, SearchOption option)
            {
                _root = root;
                _patterns = patterns;
                _option = option;
            }

            public IEnumerator<FileSystemInfo> GetEnumerator()
            {
                if (_root == null || !_root.Exists) yield break;

                IEnumerable<FileSystemInfo> matches = new List<FileSystemInfo>();
                try
                {
                    //_logger.DebugFormat("Attempting to enumerate '{0}'", _root.FullName);
                    matches = _patterns.Aggregate(matches, (current, pattern) => current.Concat(_root.EnumerateDirectories(pattern, SearchOption.TopDirectoryOnly))
                        .Concat(_root.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly)));
                }
                catch (UnauthorizedAccessException)
                {
                    //_logger.WarnFormat("Unable to access '{0}'. Skipping...", _root.FullName);
                    yield break;
                }
                catch (PathTooLongException)
                {
                    //_logger.Warn(string.Format(@"Could not process path '{0}\{1}'.", _root.Parent.FullName, _root.Name), ptle);
                    yield break;
                }
                catch (IOException)
                {
                    // "The symbolic link cannot be followed because its type is disabled."
                    // "The specified network name is no longer available."
                    //_logger.Warn(string.Format(@"Could not process path (check SymlinkEvaluation rules)'{0}\{1}'.", _root.Parent.FullName, _root.Name), e);
                    yield break;
                }


                //_logger.DebugFormat("Returning all objects that match the pattern(s) '{0}'", string.Join(",", _patterns));
                foreach (var file in matches)
                {
                    yield return file;
                }

                if (_option != SearchOption.AllDirectories)
                    yield break;

                //_logger.DebugFormat("Enumerating all child directories.");
                foreach (var dir in _root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    //_logger.DebugFormat("Enumerating '{0}'", dir.FullName);
                    var fileSystemInfos = new FileSystemEnumerable(dir, _patterns, _option);
                    foreach (var match in fileSystemInfos)
                    {
                        yield return match;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public void CenterWindowOnScreen()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        // Logic for radio buttons - each one of them sets the "mode" value to an integer which is then used to determine which search mode has been selected
        private void Starts_Mode_Checked(object sender, RoutedEventArgs e)
        {
            mode = 1;
        }

        private void Ends_Mode_Checked(object sender, RoutedEventArgs e)
        {
            mode = 2;
        }

        private void Contains_Mode_Checked(object sender, RoutedEventArgs e)
        {
            mode = 3;
        }

        private void Specific_Mode_Checked(object sender, RoutedEventArgs e)
        {
            mode = 4;
        }
        private void Search_Btn_Clicked(object sender, RoutedEventArgs e)
        {
            string param = search_param_input.Text;
            string foundFiles = "";

            // Make sure that neither search path nor search parameters are blank

            if (path == "")
                MessageBox.Show("No search path was selected.");
            else if (param == "")
                MessageBox.Show("No search parameters were given.");
            else if(param == "" && path == "")
                MessageBox.Show("No search parameters and search path were given.");
            else
            {
                int c = 0;
                switch (mode)
                {
                    case 1:
                        /*
                        I want to explain some of the variable naming in the following code:
                        sFolder - short for startsWithFolder
                        eFolder - short for endsWithFolder
                        cFolder - short for containsFolder
                        spFolder - short for specificFolder

                        and so on and so forth, the variables are used to create files and I needed to name them somehow and since I am a lazy fuck, that's the result ;)
                        */
                         
                        // Create a log file

                        string sFolder = $"{Directory.GetCurrentDirectory()}\\Scans\\Starts_With_Scan_Results\\";
                        string sFileName = $"Starts_With_Scan_Result_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.txt";
                        string sFilePath = System.IO.Path.Combine(sFolder, sFileName);
                        foundFiles = $"This file is saved at {sFilePath}.\nThe search parameters are: \n{param}\nThe path searched is:\n{path}\nThe date and time of the search is {DateTime.Now}\n\n\n";


                        // Initialize Starts_With search

                        //Console.WriteLine("Search started.");
                        foreach (var fileSystemInfo in FileSystemEnumerable.Search(path, $"{param}*", SearchOption.AllDirectories))
                        {
                            //Console.WriteLine(fileSystemInfo.FullName);
                            foundFiles += $"{fileSystemInfo.FullName}\n";
                            c++;
                        }

                        // Write data to log file

                        File.WriteAllText(sFilePath, foundFiles);
                        MessageBox.Show($"Found {c} matches!\nThe result file will open after you close this window.");

                        Process.Start(sFilePath);

                        break;
                    case 2:
                        // Create a log file

                        string eFolder = $"{Directory.GetCurrentDirectory()}\\Scans\\Ends_With_Scan_Results\\";
                        string eFileName = $"Ends_With_Scan_Result_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.txt";
                        string eFilePath = System.IO.Path.Combine(eFolder, eFileName);
                        foundFiles = $"This file is saved at {eFilePath}.\nThe search parameters are: \n{param}\nThe path searched is:\n{path}\nThe date and time of the search is {DateTime.Now}\n\n\n";


                        // Initialize Ends_With search

                        //Console.WriteLine("Search started.");
                        foreach (var fileSystemInfo in FileSystemEnumerable.Search(path, $"*{param}", SearchOption.AllDirectories))
                        {
                            //Console.WriteLine(fileSystemInfo.FullName);
                            foundFiles += $"{fileSystemInfo.FullName}\n";
                            c++;
                        }

                        // Write data to log file

                        File.WriteAllText(eFilePath, foundFiles);
                        MessageBox.Show($"Found {c} matches!\nThe result file will open after you close this window.");

                        Process.Start(eFilePath);

                        break;
                    case 3:
                        // Create a log file

                        string cFolder = $"{Directory.GetCurrentDirectory()}\\Scans\\Contains_Scan_Results\\";
                        string cFileName = $"Contains_Scan_Result_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.txt";
                        string cFilePath = System.IO.Path.Combine(cFolder, cFileName);
                        foundFiles = $"This file is saved at {cFilePath}.\nThe search parameters are: \n{param}\nThe path searched is:\n{path}\nThe date and time of the search is {DateTime.Now}\n\n\n";

                        // Initialize Contains search
                        //Console.WriteLine("Search started.");
                        foreach (var fileSystemInfo in FileSystemEnumerable.Search(path, $"*{param}*", SearchOption.AllDirectories))
                        {
                            //Console.WriteLine(fileSystemInfo.FullName);
                            foundFiles += $"{fileSystemInfo.FullName}\n";
                            c++;
                        }

                        // Write data to log file

                        File.WriteAllText(cFilePath, foundFiles);
                        MessageBox.Show($"Found {c} matches!\nThe result file will open after you close this window.");

                        Process.Start(cFilePath);


                        break;
                    case 4:
                        // Create a log file

                        string spFolder = $"{Directory.GetCurrentDirectory()}\\Scans\\Specific_Scan_Results\\";
                        string spFileName = $"Specific_Scan_Result_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.txt";
                        string spFilePath = System.IO.Path.Combine(spFolder, spFileName);
                        foundFiles = $"This file is saved at {spFilePath}.\nThe search parameters are: \n{param}\nThe path searched is:\n{path}\nThe date and time of the search is {DateTime.Now}\n\n\n";

                        // Initialize Specific search

                        //Console.WriteLine("Search started.");
                        foreach (var fileSystemInfo in FileSystemEnumerable.Search(path, param, SearchOption.AllDirectories))
                        {
                            //Console.WriteLine(fileSystemInfo.FullName);
                            foundFiles += $"{fileSystemInfo.FullName}\n";
                            c++;
                        }

                        // Write data to log file

                        File.WriteAllText(spFilePath, foundFiles);
                        MessageBox.Show($"Found {c} matches!\nThe result file will open after you close this window.");

                        Process.Start(spFilePath);

                        break;
                    default:
                        // Run this if no search mode was selected

                        MessageBox.Show("No mode has been selected");
                        break;
                }
            }
        }


        private void Select_Btn_Clicked(object sender, RoutedEventArgs e)
        {
            var ookiiDialog = new VistaFolderBrowserDialog();

            if ((bool)ookiiDialog.ShowDialog())
            {
                path = ookiiDialog.SelectedPath;
            }
        }

        private void Data_Was_Entered(object sender, TextChangedEventArgs e)
        {
            // 
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new About());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new Settings());
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            // 
        }
    }
}
