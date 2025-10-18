using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security.Principal;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Linq;

class UniversalLinkSetup
{
    private static string rootDir, gameFolder, launcherExe, launcherArgs, linkedGameFolder, preExe, preArgs;
    private static bool installMode, showConsole;
    private static Dictionary<string, string> links = new Dictionary<string, string>(), programLinks = new Dictionary<string, string>();
    private static Dictionary<string, string[]> services = new Dictionary<string, string[]>();
    private static Dictionary<string, RegistryEntry> registryEntries = new Dictionary<string, RegistryEntry>();
    private static Dictionary<string, ArgumentSet> argumentSets = new Dictionary<string, ArgumentSet>();
    private static HashSet<string> backupLinks = new HashSet<string>();
    private static System.Threading.Timer backupTimer;
    private static System.Threading.ManualResetEvent exitEvent = new System.Threading.ManualResetEvent(false);

    public class AdminCredentials { public string Username, Password, Domain; }
    public class RegistryEntry { public string Key, Value, Type, Data; }
    public class ArgumentSet { public Dictionary<string, string> Links; public Dictionary<string, RegistryEntry> Registry; public string PreExe; }

    [DllImport("kernel32.dll")] static extern bool AllocConsole();
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("shell32.dll")] static extern bool IsUserAnAdmin();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern uint GetPrivateProfileString(string section, string key, string def, System.Text.StringBuilder ret, uint size, string file);
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)] static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);
    [DllImport("kernel32.dll")] extern static bool CloseHandle(IntPtr handle);

    static void Log(string msg) { if (showConsole) Console.WriteLine(msg); }

    [STAThread]
    static int Main(string[] args)
    {
        var consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero) ShowWindow(consoleWindow, 0);
        
        showConsole = args.Any(a => a.ToLower() == "-console" || a.ToLower() == "-debug");
        if (showConsole)
        {
            if (consoleWindow == IntPtr.Zero) { AllocConsole(); consoleWindow = GetConsoleWindow(); }
            if (consoleWindow != IntPtr.Zero) ShowWindow(consoleWindow, 1);
            Console.Title = "Universal Link Setup - Debug";
        }
        
        installMode = args.Any(a => a.ToLower() == "-install");
        rootDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
        
        Log("=== Универсальный инструмент символических ссылок ===\nПользователь: phamandai274\nДата: 2025-10-17 05:17:12");
        if (installMode) Log("Режим: Установка");
        
        try 
        { 
            LoadConfig(); 
            ProcessArguments(args);
            
            if (!string.IsNullOrEmpty(launcherArgs)) Log("Аргументы: " + launcherArgs);
            
            if (IsLauncherRunning())
            {
                Log("\n[SKIP] Лаунчер запущен. Пропуск настройки...");
                DisplayLauncherInfo();
                ApplyRegistryChanges();
                if (!installMode && !string.IsNullOrEmpty(launcherExe)) { RunPreProgram(); RunLauncher(); }
                Log("\n=== Быстрый запуск завершен ==="); 
            }
            else
            {
                Log("\n[INFO] Полная настройка...");
                if (!IsUserAnAdmin()) { HandleAdminRights(args); return 0; }
                DisplayConfig(); 
                Setup(); 
                Log("\n=== Настройка завершена ==="); 
            }
            
            StartBackupCheck();
            WaitForCompletion();
            return 0; 
        }
        catch (Exception ex) { 
            if (!showConsole) MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else { Log("ОШИБКА: " + ex.Message); Console.ReadKey(); } 
            return 1; 
        }
        finally { StopBackupCheck(); }
    }

    static void HandleAdminRights(string[] args)
    {
        Log("[!] Требуются права администратора");
        var adminCreds = GetAdminCredentialsFromRegistry();
        if (adminCreds != null) { Log("[✓] Найдены данные администратора"); RunWithCredentials(adminCreds.Username, adminCreds.Password, adminCreds.Domain, args); return; }
        
        try { RestartAsAdmin(args); }
        catch 
        { 
            if (!showConsole) 
            { 
                if (MessageBox.Show("Требуются права администратора", "Администратор", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                    try { RestartAsAdmin(args); } catch { }
            } 
            else RunAsAnotherUser(args);
        }
    }

    static void WaitForCompletion()
    {
        if (backupLinks.Count > 0 && showConsole)
        {
            Log("\nНажмите любую клавишу для выхода...");
            new System.Threading.Thread(() => { try { Console.ReadKey(); exitEvent.Set(); } catch { } }) { IsBackground = true }.Start();
            exitEvent.WaitOne();
        }
        else if (showConsole) { Log("Нажмите любую клавишу..."); Console.ReadKey(); }
    }

    static void StartBackupCheck()
    {
        if (backupLinks.Count == 0) return;
        Log("\n[BACKUP] Проверка через 3 минуты (" + backupLinks.Count + " папок)");
        backupTimer = new System.Threading.Timer(CheckAndBackupData, null, TimeSpan.FromMinutes(3), System.Threading.Timeout.InfiniteTimeSpan);
    }

    static void StopBackupCheck()
    {
        if (backupTimer != null) { backupTimer.Dispose(); backupTimer = null; }
        exitEvent.Set();
    }

    static void CheckAndBackupData(object state)
    {
        try
        {
            Log("\n[BACKUP] Проверка обновлений...");
            bool anyBackedUp = false;

            if (!string.IsNullOrEmpty(gameFolder) && string.IsNullOrEmpty(linkedGameFolder))
                foreach (var link in links.Where(l => backupLinks.Contains(l.Key)))
                {
                    string linkPath = Path.Combine(gameFolder, link.Key);
                    if (Directory.Exists(linkPath) && !IsSymbolicLink(linkPath) && Directory.Exists(link.Value))
                    {
                        BackupUpdatedData(linkPath, link.Value);
                        anyBackedUp = true;
                    }
                }

            foreach (var link in programLinks)
            {
                string folderType = GetFolderTypeFromPath(link.Key);
                if (!backupLinks.Contains(folderType)) continue;
                if (Directory.Exists(link.Key) && !IsSymbolicLink(link.Key) && Directory.Exists(link.Value))
                {
                    BackupUpdatedData(link.Key, link.Value);
                    anyBackedUp = true;
                }
            }

            if (!string.IsNullOrEmpty(linkedGameFolder) && backupLinks.Contains("GameFolder") && 
                Directory.Exists(gameFolder) && !IsSymbolicLink(gameFolder) && Directory.Exists(linkedGameFolder))
            {
                BackupUpdatedData(gameFolder, linkedGameFolder);
                anyBackedUp = true;
            }

            Log(anyBackedUp ? "[BACKUP] Обновления скопированы" : "[BACKUP] Обновлений нет");
            exitEvent.Set();
        }
        catch (Exception ex) { Log("[!] Ошибка: " + ex.Message); exitEvent.Set(); }
    }

    static string GetFolderTypeFromPath(string path)
    {
        if (path.Contains(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))) return "AppDataLocal";
        if (path.Contains(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))) return "AppDataRoaming";
        if (path.Contains(Environment.GetEnvironmentVariable("ProgramData"))) return "ProgramData";
        if (path.Contains(Environment.GetEnvironmentVariable("ProgramFiles"))) return "ProgramFiles";
        if (path.Contains(Environment.GetEnvironmentVariable("ProgramFiles(x86)"))) return "ProgramFilesx86";
        return Path.GetFileName(path);
    }

    static bool IsSymbolicLink(string path) { try { return new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint); } catch { return false; } }

    static void BackupUpdatedData(string src, string dst)
    {
        try { Log("[COPY] " + src + " -> " + dst); CopyDirectory(src, dst, true); }
        catch (Exception ex) { Log("[!] Ошибка: " + ex.Message); }
    }

    static void CopyDirectory(string srcDir, string dstDir, bool overwrite)
    {
        var src = new DirectoryInfo(srcDir);
        var dst = new DirectoryInfo(dstDir);
        if (!dst.Exists) dst.Create();

        foreach (var file in src.GetFiles())
        {
            string dstFile = Path.Combine(dst.FullName, file.Name);
            if (overwrite || !File.Exists(dstFile))
                try { file.CopyTo(dstFile, overwrite); }
                catch (Exception ex) { Log("[!] " + file.Name + ": " + ex.Message); }
        }

        foreach (var d in src.GetDirectories())
            CopyDirectory(d.FullName, Path.Combine(dst.FullName, d.Name), overwrite);
    }

    static void ProcessArguments(string[] args)
    {
        var systemArgs = new[] { "-install", "-console", "-debug" };
        var launcherArgsList = new List<string>();
        var customArgs = new List<string>();
        
        foreach (string arg in args)
        {
            if (systemArgs.Contains(arg.ToLower())) continue;
            if (arg.StartsWith("-") && argumentSets.ContainsKey(arg.Substring(1))) customArgs.Add(arg.Substring(1));
            else launcherArgsList.Add(arg);
        }
        
        launcherArgs = string.Join(" ", launcherArgsList);
        if (customArgs.Count > 0) ApplyCustomArguments(customArgs);
    }

    static void ApplyCustomArguments(List<string> customArgs)
    {
        Log("\n--- Кастомные аргументы ---");
        
        foreach (string argName in customArgs)
        {
            if (!argumentSets.ContainsKey(argName)) continue;
            var argSet = argumentSets[argName];
            
            if (argSet.Links != null) 
                foreach (var link in argSet.Links) 
                { 
                    links[link.Key] = link.Value; 
                    Log("[LINK+] " + link.Key + " -> " + link.Value); 
                }
            
            if (argSet.Registry != null) 
                foreach (var reg in argSet.Registry) 
                { 
                    registryEntries[reg.Key] = reg.Value; 
                    Log("[REG+] " + reg.Value.Key + "\\" + reg.Value.Value); 
                }
            
            if (!string.IsNullOrEmpty(argSet.PreExe))
            {
                ParsePreExe(argSet.PreExe);
                Log("[PREEXE+] " + preExe);
            }
        }
    }

    static void ApplyRegistryChanges()
    {
        if (registryEntries.Count == 0) return;
        Log("\n--- Реестр ---");
        foreach (var entry in registryEntries)
        {
            try { ApplyRegistryEntry(entry.Value); Log("[REG] " + entry.Key); }
            catch (Exception ex) { Log("[!] " + entry.Key + ": " + ex.Message); }
        }
    }

    static void ApplyRegistryEntry(RegistryEntry entry)
    {
        var rootKey = GetRootKey(entry.Key);
        if (rootKey == null) throw new Exception("Неподдерживаемый корень: " + entry.Key);

        string keyPath = GetKeyPath(entry.Key);
        string data = ExpandVariables(entry.Data);

        using (var key = rootKey.CreateSubKey(keyPath))
        {
            if (key == null) throw new Exception("Не удалось создать ключ");

            switch (entry.Type.ToUpper())
            {
                case "REG_SZ": key.SetValue(entry.Value, data, RegistryValueKind.String); break;
                case "REG_DWORD": key.SetValue(entry.Value, uint.Parse(data), RegistryValueKind.DWord); break;
                case "REG_QWORD": key.SetValue(entry.Value, ulong.Parse(data), RegistryValueKind.QWord); break;
                case "REG_BINARY": key.SetValue(entry.Value, StringToBinary(data), RegistryValueKind.Binary); break;
                case "REG_MULTI_SZ": key.SetValue(entry.Value, data.Split('|'), RegistryValueKind.MultiString); break;
                case "REG_EXPAND_SZ": key.SetValue(entry.Value, data, RegistryValueKind.ExpandString); break;
                default: throw new Exception("Неподдерживаемый тип: " + entry.Type);
            }
        }
    }

    static RegistryKey GetRootKey(string fullKey)
    {
        if (fullKey.StartsWith("HKEY_CURRENT_USER") || fullKey.StartsWith("HKCU")) return Registry.CurrentUser;
        if (fullKey.StartsWith("HKEY_LOCAL_MACHINE") || fullKey.StartsWith("HKLM")) return Registry.LocalMachine;
        if (fullKey.StartsWith("HKEY_CLASSES_ROOT") || fullKey.StartsWith("HKCR")) return Registry.ClassesRoot;
        if (fullKey.StartsWith("HKEY_USERS") || fullKey.StartsWith("HKU")) return Registry.Users;
        if (fullKey.StartsWith("HKEY_CURRENT_CONFIG") || fullKey.StartsWith("HKCC")) return Registry.CurrentConfig;
        return null;
    }

    static string GetKeyPath(string fullKey) { int i = fullKey.IndexOf('\\'); return i > 0 ? fullKey.Substring(i + 1) : ""; }
    static byte[] StringToBinary(string hex) { hex = hex.Replace(" ", "").Replace("-", ""); var result = new byte[hex.Length / 2]; for (int i = 0; i < result.Length; i++) result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16); return result; }

    static bool IsLauncherRunning()
    {
        if (string.IsNullOrEmpty(launcherExe) || !File.Exists(launcherExe)) return false;
        try
        {
            string name = Path.GetFileNameWithoutExtension(launcherExe);
            return Process.GetProcessesByName(name).Length > 0;
        }
        catch { return false; }
    }

    static void DisplayLauncherInfo()
    {
        Log("\n--- Информация ---");
        if (!string.IsNullOrEmpty(preExe)) Log("Пред-программа: " + preExe);
        if (!string.IsNullOrEmpty(launcherExe)) Log("Лаунчер: " + launcherExe);
        if (registryEntries.Count > 0) Log("Реестр: " + registryEntries.Count);
        if (backupLinks.Count > 0) Log("Бэкап: " + backupLinks.Count + " папок");
    }

    static AdminCredentials GetAdminCredentialsFromRegistry()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\UniversalLinkSetup\AdminCredentials"))
            {
                if (key == null) return null;
                string username = key.GetValue("Username") as string;
                string encryptedPassword = key.GetValue("Password") as string;
                string domain = key.GetValue("Domain") as string ?? Environment.MachineName;
                
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(encryptedPassword)) return null;
                
                string password = DecryptPassword(encryptedPassword);
                if (ValidateCredentials(username, password, domain))
                    return new AdminCredentials { Username = username, Password = password, Domain = domain };
                
                Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\UniversalLinkSetup\AdminCredentials", false);
            }
        }
        catch { }
        return null;
    }

    static bool ValidateCredentials(string username, string password, string domain) 
    { 
        IntPtr token = IntPtr.Zero; 
        try { return LogonUser(username, domain, password, 2, 0, out token); } 
        finally { if (token != IntPtr.Zero) CloseHandle(token); } 
    }
    
    static string EncryptPassword(string password) 
    { 
        byte[] data = System.Text.Encoding.UTF8.GetBytes(password);
        byte[] key = System.Text.Encoding.UTF8.GetBytes(Environment.MachineName + "phamandai274");
        for (int i = 0; i < data.Length; i++) data[i] ^= key[i % key.Length];
        return Convert.ToBase64String(data);
    }
    
    static string DecryptPassword(string encryptedPassword) 
    { 
        try 
        { 
            byte[] data = Convert.FromBase64String(encryptedPassword);
            byte[] key = System.Text.Encoding.UTF8.GetBytes(Environment.MachineName + "phamandai274");
            for (int i = 0; i < data.Length; i++) data[i] ^= key[i % key.Length];
            return System.Text.Encoding.UTF8.GetString(data);
        } 
        catch { return ""; } 
    }
    
    static void RunWithCredentials(string username, string password, string domain, string[] args) 
    { 
        try 
        { 
            var startInfo = new ProcessStartInfo 
            { 
                UseShellExecute = false,
                FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
                WorkingDirectory = Environment.CurrentDirectory,
                Domain = domain,
                UserName = username,
                Password = GetSecureString(password),
                LoadUserProfile = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            if (args.Length > 0) startInfo.Arguments = string.Join(" ", args);
            Process.Start(startInfo);
        } 
        catch (Exception ex) { Log("[!] Ошибка: " + ex.Message); if (showConsole) Console.ReadKey(); } 
    }
    
    static void RestartAsAdmin(string[] args) 
    { 
        var startInfo = new ProcessStartInfo 
        { 
            UseShellExecute = true,
            FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden
        };
        if (args.Length > 0) startInfo.Arguments = string.Join(" ", args);
        Process.Start(startInfo);
    }

    static void RunAsAnotherUser(string[] args)
    {
        Console.Write("\nАдминистратор: ");
        string username = Console.ReadLine();
        Console.Write("Пароль: ");
        string password = ReadPassword();
        Console.Write("Домен (Enter для локальной машины): ");
        string domain = Console.ReadLine();
        if (string.IsNullOrEmpty(domain)) domain = Environment.MachineName;
        
        if (ValidateCredentials(username, password, domain))
        {
            try 
            { 
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\UniversalLinkSetup\AdminCredentials")) 
                { 
                    key.SetValue("Username", username);
                    key.SetValue("Password", EncryptPassword(password));
                    key.SetValue("Domain", domain);
                    key.SetValue("SavedDate", "2025-10-17 05:17:12");
                } 
                Log("[✓] Данные сохранены");
            }
            catch (Exception ex) { Log("[!] Ошибка: " + ex.Message); }
            RunWithCredentials(username, password, domain, args);
        }
        else { Log("\n[!] Неверные данные"); Console.ReadKey(); }
    }

    static string ReadPassword() 
    { 
        string password = "";
        ConsoleKeyInfo key;
        do 
        {
            key = Console.ReadKey(true);
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter) 
            { 
                password += key.KeyChar;
                Console.Write("*");
            } 
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0) 
            { 
                password = password.Substring(0, password.Length - 1);
                Console.Write("\b \b");
            } 
        } while (key.Key != ConsoleKey.Enter);
        Console.WriteLine();
        return password;
    }
    
    static System.Security.SecureString GetSecureString(string password) 
    { 
        var secureString = new System.Security.SecureString();
        foreach (char c in password) secureString.AppendChar(c);
        secureString.MakeReadOnly();
        return secureString;
    }

    static string ExpandVariables(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        var vars = new Dictionary<string, string> 
        {
            {"AppDataLocal", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)},
            {"AppDataRoaming", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)},
            {"ProgramData", Environment.GetEnvironmentVariable("ProgramData")},
            {"ProgramFiles", Environment.GetEnvironmentVariable("ProgramFiles")},
            {"ProgramFilesx86", Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? Environment.GetEnvironmentVariable("ProgramFiles")},
            {"UserProfile", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)},
            {"Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop)},
            {"Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)},
            {"Windows", Environment.GetEnvironmentVariable("WINDIR")},
            {"System32", Environment.GetFolderPath(Environment.SpecialFolder.System)},
            {"Temp", Path.GetTempPath().TrimEnd('\\')},
            {"CommonAppData", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}
        };
        foreach (var v in vars)
            if (!string.IsNullOrEmpty(v.Value))
                path = path.Replace("%" + v.Key + "%", v.Value).Replace("{" + v.Key + "}", v.Value).Replace("$" + v.Key, v.Value);
        return path;
    }

    static void ParsePreExe(string raw)
    {
        if (string.IsNullOrEmpty(raw)) { preExe = preArgs = ""; return; }
        string expanded = ExpandVariables(raw);
        
        if (expanded.StartsWith("\"")) 
        { 
            int end = expanded.IndexOf("\"", 1);
            if (end > 0) 
            { 
                preExe = expanded.Substring(1, end - 1);
                preArgs = expanded.Length > end + 1 ? expanded.Substring(end + 1).Trim() : "";
            } 
            else preExe = expanded.Trim('"');
        }
        else 
        { 
            int exeIndex = expanded.ToLower().LastIndexOf(".exe");
            if (exeIndex > 0) 
            { 
                int spaceAfter = expanded.IndexOf(" ", exeIndex + 4);
                if (spaceAfter > 0) 
                { 
                    preExe = expanded.Substring(0, spaceAfter).Trim();
                    preArgs = expanded.Substring(spaceAfter + 1).Trim();
                } 
                else preExe = expanded.Trim();
            } 
            else preExe = expanded.Trim();
        }
    }

    static void LoadConfig()
    {
        string configFile = Path.Combine(rootDir, "config.ini");
        if (!File.Exists(configFile)) return;
        
        gameFolder = ReadIni(configFile, "Settings", "GameFolder", "");
        launcherExe = ExpandVariables(ReadIni(configFile, "Settings", "LauncherExe", ""));
        ParsePreExe(ReadIni(configFile, "Settings", "PreExe", ""));
        linkedGameFolder = ReadIni(configFile, "Settings", "LinkedGameFolder", "");
        
        string backupFolders = ReadIni(configFile, "Settings", "BackupFolders", "");
        if (!string.IsNullOrEmpty(backupFolders))
            foreach (string folder in backupFolders.Split(','))
            {
                string trimmed = folder.Trim();
                if (!string.IsNullOrEmpty(trimmed)) backupLinks.Add(trimmed);
            }
        
        LoadSection(configFile, "GameLinks", (k, v) => links[k] = v);
        LoadSection(configFile, "CustomLinks", (k, v) => programLinks[v] = Path.Combine(rootDir, "Data", k));
        LoadSection(configFile, "Services", (k, v) => services[k] = v.Split('|'));
        LoadRegistrySection(configFile);
        LoadArgumentSets(configFile);
        
        LoadMultipleProgramLinks(configFile, "AppDataLocal", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        LoadMultipleProgramLinks(configFile, "AppDataRoaming", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        LoadMultipleProgramLinks(configFile, "ProgramData", Environment.GetEnvironmentVariable("ProgramData"));
        LoadMultipleProgramLinks(configFile, "ProgramFiles", Environment.GetEnvironmentVariable("ProgramFiles"));
        LoadMultipleProgramLinks(configFile, "ProgramFilesx86", Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? Environment.GetEnvironmentVariable("ProgramFiles"));
    }

    static void LoadMultipleProgramLinks(string configFile, string folderType, string systemBase)
    {
        if (string.IsNullOrEmpty(systemBase)) return;
        
        var allKeys = GetIniKeys(configFile, "ProgramLinks");
        var matchingKeys = allKeys.Where(k => k.StartsWith(folderType, StringComparison.OrdinalIgnoreCase)).ToArray();
        
        foreach (var key in matchingKeys)
        {
            string path = ReadIni(configFile, "ProgramLinks", key, "");
            if (string.IsNullOrEmpty(path)) continue;
            
            if (key.Equals(folderType, StringComparison.OrdinalIgnoreCase) || int.TryParse(key.Substring(folderType.Length), out int num))
            {
                var systemFull = Path.Combine(systemBase, path);
                programLinks[systemFull] = Path.Combine(rootDir, "Data", folderType, path);
            }
        }
    }

    static void LoadArgumentSets(string configFile)
    {
        try
        {
            string[] lines = File.ReadAllLines(configFile);
            string currentSection = "";
            
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                }
                else if (currentSection.StartsWith("Arg_") && trimmed.Contains("=") && !trimmed.StartsWith(";"))
                {
                    string argName = currentSection.Substring(4);
                    if (!argumentSets.ContainsKey(argName))
                        argumentSets[argName] = new ArgumentSet { Links = new Dictionary<string, string>(), Registry = new Dictionary<string, RegistryEntry>(), PreExe = null };
                    
                    string[] parts = trimmed.Split(new char[] { '=' }, 2);
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    
                    if (key.Equals("PreExe", StringComparison.OrdinalIgnoreCase))
                        argumentSets[argName].PreExe = value;
                    else if (key.StartsWith("Link_"))
                        argumentSets[argName].Links[key.Substring(5)] = value;
                    else if (key.StartsWith("Reg_"))
                    {
                        string[] regParts = value.Split('|');
                        if (regParts.Length >= 4)
                            argumentSets[argName].Registry[key.Substring(4)] = new RegistryEntry 
                            { 
                                Key = regParts[0].Trim(),
                                Value = regParts[1].Trim(),
                                Type = regParts[2].Trim(),
                                Data = regParts[3].Trim()
                            };
                    }
                }
            }
        }
        catch { }
    }

    static void LoadRegistrySection(string configFile)
    {
        foreach (string key in GetIniKeys(configFile, "Registry"))
        {
            string value = ReadIni(configFile, "Registry", key, "");
            if (string.IsNullOrEmpty(value)) continue;
            
            string[] parts = value.Split('|');
            if (parts.Length >= 4)
                registryEntries[key] = new RegistryEntry 
                { 
                    Key = parts[0].Trim(),
                    Value = parts[1].Trim(),
                    Type = parts[2].Trim(),
                    Data = parts[3].Trim()
                };
        }
    }

    static void LoadSection(string configFile, string section, Action<string, string> action) 
    { 
        foreach (string key in GetIniKeys(configFile, section)) 
        { 
            string value = ReadIni(configFile, section, key, "");
            if (!string.IsNullOrEmpty(value)) action(key, value);
        } 
    }
    
    static string[] GetIniKeys(string configFile, string section) 
    { 
        var keys = new List<string>();
        try 
        { 
            bool inSection = false;
            foreach (string line in File.ReadAllLines(configFile)) 
            { 
                string t = line.Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                    inSection = t.Equals("[" + section + "]", StringComparison.OrdinalIgnoreCase);
                else if (inSection && t.Contains("=") && !t.StartsWith(";"))
                    keys.Add(t.Split('=')[0].Trim());
            } 
        } 
        catch { }
        return keys.ToArray();
    }
    
    static string ReadIni(string file, string section, string key, string def) 
    { 
        var sb = new System.Text.StringBuilder(512);
        GetPrivateProfileString(section, key, def, sb, 512, file);
        return sb.ToString();
    }

    static void DisplayConfig()
    {
        if (!string.IsNullOrEmpty(gameFolder))
        {
            Log("\n--- Настройки ---");
            Log("Папка: " + gameFolder);
            Log("Пользователь: " + WindowsIdentity.GetCurrent().Name);
        }
        
        if (!string.IsNullOrEmpty(linkedGameFolder)) Log("Ссылка: " + gameFolder + " -> " + linkedGameFolder);
        if (!string.IsNullOrEmpty(preExe)) Log("Пред-программа: " + preExe);
        if (!string.IsNullOrEmpty(launcherExe)) Log("Лаунчер: " + launcherExe + (installMode ? " [ПРОПУСК]" : ""));
        if (backupLinks.Count > 0) Log("Бэкап: " + string.Join(", ", backupLinks));
        if (argumentSets.Count > 0) { Log("\n--- Аргументы ---"); foreach (var a in argumentSets) Log("-" + a.Key); }
        if (registryEntries.Count > 0) Log("\n--- Реестр: " + registryEntries.Count + " записей ---");
        if (links.Count > 0) { Log("\n--- Игровые ссылки ---"); foreach (var l in links) Log((Directory.Exists(l.Value) ? "[OK] " : "[НЕТ] ") + l.Key); }
        if (programLinks.Count > 0) { Log("\n--- Программные ссылки ---"); foreach (var l in programLinks) Log((Directory.Exists(l.Value) ? "[OK] " : "[НЕТ] ") + Path.GetFileName(l.Key)); }
        if (services.Count > 0) { Log("\n--- Службы ---"); foreach (var s in services) Log(s.Key); }
    }

    static void Setup()
    {
        if (!string.IsNullOrEmpty(gameFolder))
        {
            Log("\n--- Папки ---");
            if (!string.IsNullOrEmpty(linkedGameFolder))
            {
                EnsureDir(linkedGameFolder);
                SafeRemove(gameFolder);
                CreateLink(gameFolder, linkedGameFolder);
            }
            else EnsureDir(gameFolder);
        }
        
        EnsureDir(Path.Combine(rootDir, "Data"));
        EnsureDir(Path.Combine(rootDir, "Data\\Regs"));
        EnsureDir(Path.Combine(rootDir, "Data\\Regs\\settings"));
        EnsureDir(Path.Combine(rootDir, "Data\\Regs\\games"));
        
        var folderTypes = new[] { "AppDataLocal", "AppDataRoaming", "ProgramData", "ProgramFiles", "ProgramFilesx86" };
        foreach (string ft in folderTypes)
            if (programLinks.Any(link => link.Value.Contains("Data\\" + ft)))
                EnsureDir(Path.Combine(rootDir, "Data", ft));
        
        foreach (var link in programLinks) EnsureDir(link.Value);
        
        if (links.Count > 0 || programLinks.Count > 0)
        {
            Log("\n--- Удаление ---");
            if (string.IsNullOrEmpty(linkedGameFolder))
                foreach (var link in links) SafeRemove(Path.Combine(gameFolder, link.Key));
            foreach (var link in programLinks) SafeRemove(link.Key);
            
            Log("\n--- Ссылки ---");
            foreach (var link in programLinks) CreateLink(link.Key, link.Value);
            if (string.IsNullOrEmpty(linkedGameFolder))
                foreach (var link in links) CreateLink(Path.Combine(gameFolder, link.Key), link.Value);
        }
        
        if (services.Count > 0) { Log("\n--- Службы ---"); foreach (var s in services) ManageService(s.Key, s.Value); }
        
        ImportRegs();
        ApplyRegistryChanges();
        
        if (!installMode && !string.IsNullOrEmpty(launcherExe)) { RunPreProgram(); RunLauncher(); }
    }

    static void RunPreProgram() 
    { 
        if (string.IsNullOrEmpty(preExe) || !File.Exists(preExe)) return;
        try 
        { 
            var startInfo = new ProcessStartInfo { FileName = preExe, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(preExe) };
            if (!string.IsNullOrEmpty(preArgs)) startInfo.Arguments = preArgs;
            Process.Start(startInfo);
            System.Threading.Thread.Sleep(1000);
        } 
        catch (Exception ex) { Log("[!] " + ex.Message); } 
    }
    
    static void RunLauncher() 
    { 
        if (!File.Exists(launcherExe)) return;
        try 
        { 
            var startInfo = new ProcessStartInfo { FileName = launcherExe, UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(launcherExe) };
            if (!string.IsNullOrEmpty(launcherArgs)) startInfo.Arguments = launcherArgs;
            Process.Start(startInfo);
        } 
        catch (Exception ex) { Log("[!] " + ex.Message); } 
    }
    
    static bool ServiceExists(string name) { try { using (var sc = new System.ServiceProcess.ServiceController(name)) { var s = sc.Status; return true; } } catch { return false; } }
    
    static void ManageService(string name, string[] config) 
    { 
        if (!File.Exists(config[0])) return;
        if (ServiceExists(name)) { RunCmd("sc.exe", "stop \"" + name + "\""); RunCmd("sc.exe", "delete \"" + name + "\""); System.Threading.Thread.Sleep(1000); }
        RunCmd("sc.exe", string.Format("create \"{0}\" binPath= \"{1}\" start= {2}", name, config[0], config.Length > 1 ? config[1] : "auto"));
        if (config.Length > 2 ? config[2] == "true" : true) RunCmd("sc.exe", "start \"" + name + "\"");
    }
    
    static void RunCmd(string cmd, string args) { try { Process.Start(new ProcessStartInfo(cmd, args) { UseShellExecute = false, CreateNoWindow = true }).WaitForExit(); } catch { } }
    static void EnsureDir(string path) { if (!Directory.Exists(path)) { Directory.CreateDirectory(path); Log("[+] " + Path.GetFileName(path)); } }
    static void SafeRemove(string path) { if (Directory.Exists(path)) { try { Directory.Delete(path, true); Log("[-] " + Path.GetFileName(path)); } catch { } } }
    static void CreateLink(string link, string target) { if (!Directory.Exists(target)) return; RunCmd("cmd.exe", "/c mklink /D \"" + link + "\" \"" + target + "\""); Log("[L] " + Path.GetFileName(link)); }

    static void ImportRegs()
    {
        Log("\n--- Реестр ---");
        string regDir = Path.Combine(rootDir, "Data", "Regs");
        var allRegFiles = new List<string>();
        
        try
        {
            if (Directory.Exists(regDir)) allRegFiles.AddRange(Directory.GetFiles(regDir, "*.reg"));
            string settingsDir = Path.Combine(regDir, "settings");
            if (Directory.Exists(settingsDir)) allRegFiles.AddRange(Directory.GetFiles(settingsDir, "*.reg"));
            if (!installMode)
            {
                string gamesDir = Path.Combine(regDir, "games");
                if (Directory.Exists(gamesDir)) allRegFiles.AddRange(Directory.GetFiles(gamesDir, "*.reg"));
            }
            
            if (allRegFiles.Count == 0) return;
            
            foreach (string regFile in allRegFiles)
            {
                try 
                { 
                    Process.Start(new ProcessStartInfo 
                    { 
                        FileName = "regedit.exe",
                        Arguments = "/s \"" + regFile + "\"",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Verb = "runas"
                    }).WaitForExit(5000);
                    Log("[R] " + Path.GetFileName(regFile));
                    System.Threading.Thread.Sleep(100);
                } 
                catch { }
            }
        }
        catch { }
    }
}
